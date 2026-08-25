using System.Collections.Generic;
using System.Linq;
using Mirror;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.GrassGeneration
{
    public class GrassGenerator : NetworkBehaviour
    {
        [Header("Prefabs")] 
        [SerializeField] private Grass[] _grassPrefabs;

        [Header("Settings")] 
        [SerializeField] private GrassGeneratorSettings _generatorSettings;
        
        [Space(10)]
        
        [SerializeField] private float _updateRate = 0.1f;

        [SerializeField] private AnimationCurve _marginRelativeToSpawnRadius;
        
        private readonly Dictionary<GrassData.OutputChunk, List<Grass>> _spawnedGrasses = new();
        
        private List<GrassData.OutputChunk> _outputChunks = new();

        private bool _isDisabled;
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            InvokeRepeating(nameof(UpdateGrass), _updateRate, _updateRate);

            _generatorSettings.SettingsUpdatedAction += OnGeneratorSettingsUpdated;
        }

        public override void OnStopLocalPlayer()
        {
            CancelInvoke(nameof(UpdateGrass));
            
            _generatorSettings.SettingsUpdatedAction -= OnGeneratorSettingsUpdated;

            CleanUp();
            
            base.OnStopLocalPlayer();
        }

        private void CleanUp()
        {
            foreach (var c in _spawnedGrasses)
            {
                foreach (var g in c.Value)
                {
                    Loader.Instance.PoolService.ReleaseObject(g);
                }
            }
            
            _spawnedGrasses.Clear();
        }
        
        private void UpdateGrass()
        {
            if (!_isDisabled)
            {
                GrassData.Instance.GetNearestNonAlloc(transform.position, _generatorSettings.SpawnRadius,
                    ref _outputChunks);

                // Удаляем неактуальное.

                var grassesToRelease = _spawnedGrasses.Where(
                    g => Vector2.Distance(
                        new Vector2(g.Key.X, g.Key.Z),
                        new Vector2(transform.position.x, transform.position.z)) > _generatorSettings.SpawnRadius);

                foreach (var p in grassesToRelease.ToList())
                {
                    foreach (var g in p.Value)
                    {
                        Loader.Instance.PoolService.ReleaseObject(g);
                    }

                    _spawnedGrasses.Remove(p.Key);
                }

                // Спавним новые.

                foreach (var o in _outputChunks)
                {
                    if (_spawnedGrasses.ContainsKey(o))
                    {
                        continue;
                    }

                    _spawnedGrasses.Add(o, new List<Grass>());

                    var grassContainers = GrassData.Instance.GetChunk(o.X, o.Z);

                    foreach (var c in grassContainers)
                    {
                        var grassPrefab = _grassPrefabs[(int)c.GrassType];

                        if (Loader.Instance.PoolService.TrySpawnObject(
                                grassPrefab,
                                c.GetPosition(),
                                c.GetRotation(),
                                out var grass))
                        {
                            grass.transform.localScale = c.GetScale();

                            _spawnedGrasses[o].Add(grass);

                            // Настраиваем LODs.
                            
                            CalculateGrassLODs(grass);
                        }
                    }
                }
            }

            _isDisabled = _generatorSettings.SpawnRadius == 0; 
        }

        private void RecalculateGrassesLODs()
        {
            foreach (var s in _spawnedGrasses)
            {
                foreach (var g in s.Value)
                {
                    CalculateGrassLODs(g);
                }
            }
        }
        
        private void CalculateGrassLODs(Grass grass)
        {
            var size = grass.LODGroup.size;
            
            grass.LODGroup.RecalculateBounds();
            
            var height = LODUtils.DistanceToRelativeHeight(
                Loader.Instance.MainCamera.Camera,
                _generatorSettings.SpawnRadius,
                grass.LODGroup.size);
            
            var lods = grass.LODGroup.GetLODs();

            if (lods.Length > 0)
            {
                lods[^1].screenRelativeTransitionHeight = 
                    height + (1 - height) * _marginRelativeToSpawnRadius.Evaluate(_generatorSettings.SpawnRadius);
            }

            grass.LODGroup.SetLODs(lods);
            grass.LODGroup.size = size;
        }

        private void OnGeneratorSettingsUpdated()
        {
            RecalculateGrassesLODs();
        }
    }
}
