using System;
using System.Collections.Generic;
using System.Linq;
using CI.QuickSave;
using CI.QuickSave.Core.Storage;
using Cysharp.Threading.Tasks;
using Mirror;
using SP.Runtime.Core.Services.SaveService.Converters;
using SP.Runtime.LoadingService;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Services.SaveService
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class BaseSaveService : NetworkBehaviour, ILoadUnit
    {
        #region Structs
        
        [Serializable]
        private class SavedElement
        {
            public SavedElement(
                string guid,
                Vector3 position,
                Quaternion rotation,
                Vector3 localScale,
                IReadOnlyList<SavedMember> members)
            {
                Guid = guid;
                Position = position;
                Rotation = rotation;
                LocalScale = localScale;
                Members = members;
            }

            public string Guid { get; }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 LocalScale { get; }
            public IReadOnlyList<SavedMember> Members { get; }
        }
        
        [Serializable]
        public class SavedMember : BaseSavedMember
        {
            public SavedMember(string componentName, string memberName, object value) : base(memberName, value)
            {
                ComponentName = componentName;
            }
            
            public string ComponentName { get; }
        }

        #endregion

        public event UnityAction Saved;
        
        protected delegate SaveHandler CustomSpawnHandler(Vector3 position, Quaternion rotation, Vector3 localScale);
        
        [Header("BaseSaveService")]
        
        [Header("Settings")]
        [SerializeField] private SaveHandler[] _saveHandlers;
        
        public const string FileName = "Saving";
        
        private readonly Dictionary<string, CustomSpawnHandler> _customSpawnHandlers = new ();

        private int _currentKeyIndex;

        private NetworkIdentity _networkIdentity;
        private NetworkIdentity NetworkIdentity
        {
            get
            {
                if (_networkIdentity == null)
                {
                    _networkIdentity = GetComponent<NetworkIdentity>();
                }

                return _networkIdentity;
            }
        }
        
        public virtual UniTask Load()
        {
            Initialize();
            
            return UniTask.CompletedTask;
        }
        
        [ServerCallback]
        private void Initialize()
        {
            QuickSaveGlobalSettings.RegisterConverter(new ItemConverter());
            
            CheckDubbing();
        }

        private void CheckDubbing()
        {
            foreach (var s in _saveHandlers)
            {
                foreach (var h in _saveHandlers)
                {
                    if (s != h && s.Guid == h.Guid)
                    {
                        Debug.LogWarning(
                            "Two save handlers with the same guid: " + 
                            h.gameObject.name + " and " + s.gameObject.name);
                        
                        return;
                    }
                }
            }
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();

            NetworkIdentity.serverOnly = true;
        }

        [ServerCallback]
        protected void RegisterCustomSpawnHandler(string guid, CustomSpawnHandler customSpawnHandler)
        {
            if (customSpawnHandler == null)
            {
                Debug.LogError("Can not register null CustomSpawnHandler!");
                return;
            }
            
            if (_customSpawnHandlers.ContainsKey(guid))
            {
                Debug.LogError("Such a CustomSpawnHandler already exists!");
                return;
            }
            
            _customSpawnHandlers.Add(guid, customSpawnHandler);
        }

        [ServerCallback]
        public void Save()
        {
            // Удаляем предыдущий файл сохранения
            if (FileAccess.Exists(FileName, false))
            {
                FileAccess.Delete(FileName, false);
            }

            var saveWriter = QuickSaveWriter.Create(FileName);
        
            // Сортируем SaveHandler's в порядке приоритета
            var handlers = FindObjectsOfType<SaveHandler>().OrderByDescending(h => h.Priority);
        
            foreach (var h in handlers)
            {
                saveWriter.Write(
                    CreateKey(),
                    new SavedElement(
                        h.Guid,
                        h.transform.position,
                        h.transform.rotation,
                        h.transform.localScale,
                        h.GetSavedMembers()));
            }

            saveWriter.Commit();
            
            Saved?.Invoke();
        }

        [ServerCallback]
        public void LoadSave()
        {
            if (!FileAccess.Exists(FileName, false))
            {
                Debug.LogWarning("Save does not exist!");
                return;
            }

            var saveReader = QuickSaveReader.Create(FileName);
            var keys = saveReader.GetAllKeys();

            foreach (var k in keys)
            {
                var savedElement = saveReader.Read<SavedElement>(k);
                
                if (TryGetSaveHandlerByGuid(savedElement.Guid, out var handler))
                {
                    var position = Vector3.zero;
                    var rotation = Quaternion.identity;
                    var localScale = Vector3.one;
                    
                    if (handler.SaveTransform)
                    {
                        position = savedElement.Position;
                        rotation = savedElement.Rotation;
                        localScale = savedElement.LocalScale;
                    }
                    
                    var saveHandler = Instantiate(handler, position, rotation, localScale);
                
                    saveHandler.SetSavedMembers(savedElement.Members);
                
                    if (saveHandler.TryGetComponent<NetworkIdentity>(out var identity))
                    {
                        NetworkServer.Spawn(identity.gameObject);
                    }
                }
            }
        }
        
        public void ClearSave()
        {
            if (FileAccess.Exists(FileName, false))
            {
                FileAccess.Delete(FileName, false);
            }
        }

        [ServerCallback]
        private SaveHandler Instantiate(
            SaveHandler saveHandler,
            Vector3 position,
            Quaternion rotation,
            Vector3 localScale)
        { 
            var output = _customSpawnHandlers.TryGetValue(saveHandler.Guid, out var customSpawnHandler) ?
                customSpawnHandler(position, rotation, localScale) :
                UnityEngine.Object.Instantiate(saveHandler, position, rotation);
            
            return output;
        }
        
        private string CreateKey()
        {
            _currentKeyIndex++;
            
            return _currentKeyIndex.ToString();
        }
        
        #region Utilities

        private bool TryGetSaveHandlerByGuid(string guid, out SaveHandler output)
        {
            output = _saveHandlers.FirstOrDefault(h => h.Guid == guid);

            if (output == null)
            {
                Debug.LogWarning("There is no such Save Handler, guid: " + guid);
            }

            return output != null;
        }
        
        #endregion
    }
}
