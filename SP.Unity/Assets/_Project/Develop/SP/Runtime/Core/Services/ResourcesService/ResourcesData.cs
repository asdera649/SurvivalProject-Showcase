using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SP.Runtime.Core.Services.ResourcesService
{
    public class ResourcesData : MonoBehaviour
    {
        #region Structs
        
        [Serializable]
        public class ResourceContainer
        {
            public ResourceContainer(
                ResourcesCollection.Resource resource,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                _resource = resource;
                _position = position;
                _rotation = rotation;
                _scale = scale;
            }

            [SerializeField] private ResourcesCollection.Resource _resource;
            public ResourcesCollection.Resource Resource => _resource;
            
            [SerializeField] private Vector3 _position;
            public Vector3 Position => _position;
            
            [SerializeField] private Quaternion _rotation;
            public Quaternion Rotation => _rotation;
            
            [SerializeField] private Vector3 _scale;
            public Vector3 Scale => _scale;
        }
        
        #endregion

        [Header("Settings")] 
        [SerializeField] private ResourcesCollection _resourcesCollection;
        [SerializeField] private Transform _parentForInstances;
        
        [SerializeField, HideInInspector] private List<ResourceContainer> _resourceContainers = new();
        public IReadOnlyList<ResourceContainer> ResourceContainers => _resourceContainers;
        
        public int ResourceContainersCount => _resourceContainers.Count;
        
#if UNITY_EDITOR
        
        public void Clear()
        {
            _resourceContainers.Clear();
        }

        public void Fill()
        {
            Clear();
            
            foreach (var r in _resourcesCollection.Resources)
            {
                var objects = FindObjectsOfType<GameObject>();

                foreach (var o in objects)
                {
                    if (PrefabUtility.GetCorrespondingObjectFromOriginalSource(o) == r.GetPrefab())
                    {
                        _resourceContainers.Add(new ResourceContainer(
                            r,
                            o.transform.position,
                            o.transform.rotation,
                            o.transform.localScale));
                    }
                }
            }
        }

        public void Instantiate()
        {
            var count = 0;
            
            foreach (var r in _resourceContainers)
            {
                var prefab = PrefabUtility.InstantiatePrefab(r.Resource.GetPrefab());

                if (prefab is GameObject obj)
                {
                    obj.transform.SetPositionAndRotation(r.Position, r.Rotation);
                    obj.transform.localScale = r.Scale;

                    if (_parentForInstances != null)
                    {
                        obj.transform.SetParent(_parentForInstances.transform);
                    }
                }

                count++;
            }
            
            Debug.Log($"Resources successfully instantiate in quantity: {count}");
        }
        
#endif
     }
}