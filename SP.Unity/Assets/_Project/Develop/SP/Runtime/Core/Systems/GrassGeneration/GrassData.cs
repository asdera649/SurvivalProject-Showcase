using System;
using System.Collections.Generic;
using System.IO;
using SP.Runtime.Utilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SP.Runtime.Core.Systems.GrassGeneration
{
    public class GrassData : Singleton<GrassData>
    {
        #region Structs

        public enum GrassType : uint
        {
            Grass01 = 0,
            Grass02,
            Flowers,
            FlowersBlue
        }
        
        public readonly struct GrassEntry
        {
            public readonly GrassType GrassType;
            private readonly float _x, _y, _z;
            private readonly float _xAngle, _yAngle, _zAngle;
            private readonly float _xScale, _yScale, _zScale;

            public GrassEntry(GrassType grassType, Vector3 position, Vector3 eulerAngles, Vector3 scale)
            {
                GrassType  = grassType;
                _x = position.x;
                _y = position.y;    
                _z = position.z;
                _xAngle = eulerAngles.x;
                _yAngle = eulerAngles.y;
                _zAngle = eulerAngles.z;
                _xScale = scale.x;
                _yScale = scale.y;
                _zScale = scale.z;
            }

            public Vector3 GetPosition() => new(_x, _y, _z);
            public Quaternion GetRotation() => Quaternion.Euler(_xAngle, _yAngle, _zAngle);
            public Vector3 GetScale() => new(_xScale, _yScale, _zScale);
        }

        public struct OutputChunk
        {
            public OutputChunk(float x, float z)
            {
                X = x;
                Z = z;
            }
            
            public float X { get; }
            public float Z { get; }
        }

        #endregion

        [Header("Settings")]
        [SerializeField] private int _chunkSize = 5;
        [SerializeField] private int _worldRestriction = 500;
        [SerializeField] private bool _deleteGrassAfterBake = true;

        // Единственное, что сериализуется в сцену
        [SerializeField, HideInInspector] private byte[] _packedData;

        [SerializeField, HideInInspector] private int _dataSize;
        public int DataSize => _dataSize;
        
        [SerializeField, HideInInspector] private int _grassCount;
        public int GrassCount => _grassCount;
        
        // Runtime-данные (не сериализуются)
        
        private GrassEntry[] _entries;
        
        private int[] _chunkStart;
        private int[] _chunkCount;
        private int _stepsPerAxis;

        #region Cached (for GetNearestNonAlloc)

        private Vector3 _cachedPosition;
        private int _cachedXChunkStartIndex, _cachedZChunkStartIndex;
        private int _cachedLeftLimit, _cachedRightLimit, _cachedEstimated;
        private int _cachedCurrentXChunkIndex, _cachedCurrentZChunkIndex;
        private bool _cachedLeftDirection;
        private Vector2 _cachedFirstDistancePosition, _cachedSecondDistancePosition;

        #endregion

        private void Awake()
        {
            BuildFromBinary();
        }

        // Десериализация в плоский массив
        private void BuildFromBinary()
        {
            _stepsPerAxis = Mathf.RoundToInt(2f * _worldRestriction / _chunkSize) + 1;
            var totalChunks = _stepsPerAxis * _stepsPerAxis;

            _chunkStart = new int[totalChunks];
            _chunkCount = new int[totalChunks];

            if (_packedData == null || _packedData.Length == 0)
            {
                _entries = Array.Empty<GrassEntry>();
                return;
            }

            using var ms = new MemoryStream(_packedData);
            using var reader = new BinaryReader(ms);

            var count = reader.ReadInt32();
            
            var tempCount = new int[totalChunks];
            
            var tempEntries = new GrassEntry[count];
            var tempFlatIndex  = new int[count];

            for (var i = 0; i < count; i++)
            {
                var grassType = (GrassType)reader.ReadByte();
                var px = reader.ReadSingle(); 
                var py = reader.ReadSingle();
                var pz = reader.ReadSingle();
                var ax = reader.ReadSingle();
                var ay = reader.ReadSingle();
                var az = reader.ReadSingle();
                var sx = reader.ReadSingle();
                var sy = reader.ReadSingle();
                var sz = reader.ReadSingle();

                var xi = Mathf.Clamp(
                    Mathf.FloorToInt((
                        px + _worldRestriction + _chunkSize * 0.5f) / _chunkSize),
                    0,
                    _stepsPerAxis - 1);
                
                var zi = Mathf.Clamp(
                    Mathf.FloorToInt((
                        pz + _worldRestriction + _chunkSize * 0.5f) / _chunkSize),
                    0,
                    _stepsPerAxis - 1);

                var flat = xi * _stepsPerAxis + zi;
                
                tempEntries[i] = new GrassEntry(
                    grassType,
                    new Vector3(px, py, pz),
                    new Vector3(ax, ay, az),
                    new Vector3(sx, sy, sz));
                
                tempFlatIndex[i] = flat;
                tempCount[flat]++;
            }
            
            var offset = 0;
            
            for (var i = 0; i < totalChunks; i++)
            {
                _chunkStart[i] = offset;
                offset += tempCount[i];
            }

            // Заполняем _entries в нужном порядке (по чанкам)
            _entries = new GrassEntry[count];
            
            var writePos = new int[totalChunks];
            Array.Copy(_chunkStart, writePos, totalChunks);

            for (var i = 0; i < count; i++)
            {
                var flat = tempFlatIndex[i];
                _entries[writePos[flat]++] = tempEntries[i];
            }

            Array.Copy(tempCount, _chunkCount, totalChunks);

            // _packedData больше не нужен в runtime - освобождаем ~11 МБ
            _packedData = null;
        }

        // ArraySegment - нулевые аллокации, нет копирования данных
        public ArraySegment<GrassEntry> GetChunk(float x, float z)
        {
            var xi = Mathf.Clamp(
                Mathf.FloorToInt((
                    x + _worldRestriction + _chunkSize * 0.5f) / _chunkSize),
                0,
                _stepsPerAxis - 1);
            
            var zi = Mathf.Clamp(
                Mathf.FloorToInt((
                    z + _worldRestriction + _chunkSize * 0.5f) / _chunkSize),
                0,
                _stepsPerAxis - 1);
            
            var flat = xi * _stepsPerAxis + zi;
            
            return new ArraySegment<GrassEntry>(_entries, _chunkStart[flat], _chunkCount[flat]);
        }

        #region Editor

#if UNITY_EDITOR

        public void Clear()
        {
            _packedData = null;
            _entries = null;
            _chunkStart = null;
            _chunkCount = null;

            _dataSize = 0;
            _grassCount = 0;
        }

        public void Fill()
        {
            try
            {
                Clear();

                var grasses = FindObjectsOfType<Grass>();
                var total = grasses.Length;

                using var ms = new MemoryStream(total * 37 + 4);
                using var writer = new BinaryWriter(ms);

                writer.Write(total);

                for (var i = 0; i < total; i++)
                {
                    if (i % 500 == 0)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Grass Bake",
                                $"Запечка травы... {i} / {total}",
                                (float)i / total))
                        {
                            Debug.LogWarning("[GrassData] Запечка отменена пользователем.");
                            return;
                        }
                    }

                    var g = grasses[i];
                    var pos = g.transform.position;
                    var angles = g.transform.eulerAngles;
                    var scale = g.transform.localScale;

                    writer.Write((byte)g.GrassType);
                    writer.Write(pos.x);
                    writer.Write(pos.y);
                    writer.Write(pos.z);
                    writer.Write(angles.x);
                    writer.Write(angles.y);
                    writer.Write(angles.z);
                    writer.Write(scale.x);
                    writer.Write(scale.y);
                    writer.Write(scale.z);
                }

                _packedData = ms.ToArray();

                _grassCount = total;
                _dataSize = _packedData?.Length ?? 0;

                if (_deleteGrassAfterBake)
                {
                    DeleteGrassObjects(grasses);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void DeleteGrassObjects(Grass[] grasses)
        {
            if (grasses == null || grasses.Length == 0)
            {
                return;
            }
            
            var total = grasses.Length;
            
            for (var i = 0; i < total; i++)
            {
                if (grasses[i] != null)
                {
                    DestroyImmediate(grasses[i].gameObject);
                }

                if (i % 100 != 0)
                {
                    continue;
                }
                
                EditorUtility.DisplayProgressBar("Grass Bake", $"Очистка сцены... {i} / {total}", 1f);
                EditorApplication.QueuePlayerLoopUpdate();
                System.Threading.Thread.Sleep(1);
            }
        }

#endif

        #endregion
        
        public void GetNearestNonAlloc(Vector3 position, float radius, ref List<OutputChunk> output)
        {
            output.Clear();
            _cachedPosition = position;

            if (_entries == null || _entries.Length == 0)
            {
                return;
            }

            _cachedLeftLimit = 0;
            _cachedRightLimit = _stepsPerAxis;
            _cachedEstimated = (_cachedLeftLimit + _cachedRightLimit) / 2;

            while (GetChunkPositionByIndex(_cachedEstimated) != _cachedPosition.x &&
                   _cachedRightLimit - _cachedLeftLimit > 1)
            {
                if (GetChunkPositionByIndex(_cachedEstimated) < _cachedPosition.x)
                {
                    _cachedLeftLimit = _cachedEstimated;
                }
                else
                {
                    _cachedRightLimit = _cachedEstimated;
                }
                
                _cachedEstimated = (_cachedLeftLimit + _cachedRightLimit) / 2;
            }
            
            _cachedXChunkStartIndex = _cachedEstimated;

            _cachedLeftLimit = 0;
            _cachedRightLimit = _stepsPerAxis;
            _cachedEstimated = (_cachedLeftLimit + _cachedRightLimit) / 2;

            while (GetChunkPositionByIndex(_cachedEstimated) != _cachedPosition.z &&
                   _cachedRightLimit - _cachedLeftLimit > 1)
            {
                if (GetChunkPositionByIndex(_cachedEstimated) < _cachedPosition.z)
                {
                    _cachedLeftLimit = _cachedEstimated;
                }
                else
                {
                    _cachedRightLimit = _cachedEstimated;
                }
                
                _cachedEstimated = (_cachedLeftLimit + _cachedRightLimit) / 2;
            }
            
            _cachedZChunkStartIndex = _cachedEstimated;

            _cachedCurrentXChunkIndex = _cachedXChunkStartIndex;
            _cachedCurrentZChunkIndex = _cachedZChunkStartIndex;
            _cachedLeftDirection = true;

            while (true)
            {
                if (_cachedLeftDirection)
                {
                    if (0 > _cachedCurrentXChunkIndex ||
                        _cachedCurrentXChunkIndex >= _stepsPerAxis ||
                        Vector2.Distance(
                            new Vector2(GetChunkPositionByIndex(_cachedCurrentXChunkIndex), 0), 
                            new Vector2(_cachedPosition.x, 0)) > radius)
                    {
                        _cachedCurrentXChunkIndex = _cachedXChunkStartIndex + 1;
                        _cachedLeftDirection = false;
                        continue;
                    }
                }
                else
                {
                    if (0 > _cachedCurrentXChunkIndex ||
                        _cachedCurrentXChunkIndex >= _stepsPerAxis ||
                        Vector2.Distance(
                            new Vector2(GetChunkPositionByIndex(_cachedCurrentXChunkIndex), 0),
                            new Vector2(_cachedPosition.x, 0)) > radius)
                    {
                        break;
                    }
                }

                _cachedCurrentZChunkIndex = _cachedZChunkStartIndex;
                
                while (true)
                {
                    if (0 > _cachedCurrentZChunkIndex || _cachedCurrentZChunkIndex >= _stepsPerAxis)
                    {
                        break;
                    }

                    _cachedFirstDistancePosition.Set(GetChunkPositionByIndex(_cachedCurrentXChunkIndex),
                                                     GetChunkPositionByIndex(_cachedCurrentZChunkIndex));
                    _cachedSecondDistancePosition.Set(_cachedPosition.x, _cachedPosition.z);

                    if (Vector2.Distance(_cachedFirstDistancePosition, _cachedSecondDistancePosition) > radius)
                    {
                        break;
                    }

                    output.Add(new OutputChunk(GetChunkPositionByIndex(_cachedCurrentXChunkIndex),
                                               GetChunkPositionByIndex(_cachedCurrentZChunkIndex)));
                    
                    _cachedCurrentZChunkIndex++;
                }

                _cachedCurrentZChunkIndex = _cachedZChunkStartIndex - 1;
                
                while (true)
                {
                    if (0 > _cachedCurrentZChunkIndex || _cachedCurrentZChunkIndex >= _stepsPerAxis)
                    {
                        break;
                    }

                    _cachedFirstDistancePosition.Set(GetChunkPositionByIndex(_cachedCurrentXChunkIndex),
                                                     GetChunkPositionByIndex(_cachedCurrentZChunkIndex));
                    _cachedSecondDistancePosition.Set(_cachedPosition.x, _cachedPosition.z);

                    if (Vector2.Distance(_cachedFirstDistancePosition, _cachedSecondDistancePosition) > radius)
                    {
                        break;
                    }

                    output.Add(new OutputChunk(GetChunkPositionByIndex(_cachedCurrentXChunkIndex),
                                               GetChunkPositionByIndex(_cachedCurrentZChunkIndex)));
                    
                    _cachedCurrentZChunkIndex--;
                }

                if (_cachedLeftDirection)
                {
                    _cachedCurrentXChunkIndex--;
                }
                else
                {
                    _cachedCurrentXChunkIndex++;
                }
            }
        }
        
        private float GetChunkPositionByIndex(int index) => -_worldRestriction + _chunkSize * index;
    }
}