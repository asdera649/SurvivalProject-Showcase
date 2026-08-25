using System;
using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Entities.LootContainers;
using SP.Runtime.Core.Entities.Mined;
using UnityEngine;

namespace SP.Runtime.Core.Services.ResourcesService
{
    [CreateAssetMenu(menuName = "ResourcesService/ResourcesCollection")]
    public class ResourcesCollection : ScriptableObject
    {
        #region Structs
        
        public enum ResourceType
        {
            Resource,
            PickupResource,
            Container
        }
        
        [Serializable]
        public class Resource
        {
            [SerializeField] private BaseEntity _resourcePrefab;
            public BaseEntity ResourcePrefab => _resourcePrefab;

            [SerializeField] private PickupResource _pickupResourcePrefab;
            public PickupResource PickupResourcePrefab => _pickupResourcePrefab;
            
            [SerializeField] private ContainerEntity _containerPrefab;
            public ContainerEntity ContainerPrefab => _containerPrefab;

            [SerializeField] private ResourceType _resourceType;
            public ResourceType ResourceType => _resourceType;

            [SerializeField] private float _restoreTime;
            public float RestoreTime => _restoreTime;
            
            [SerializeField] private float _castRadius;
            public float CastRadius => _castRadius;

            public GameObject GetPrefab()
            {
                GameObject output = null;

                switch (_resourceType)
                {
                    case ResourceType.Resource:
                    {
                        output = _resourcePrefab.gameObject;
                        break;
                    }
                    case ResourceType.PickupResource:
                    {
                        output = _pickupResourcePrefab.gameObject;
                        break;
                    }
                    case ResourceType.Container:
                    {
                        output = _containerPrefab.gameObject;
                        break;
                    }
                }

                return output;
            }
        }
        
        #endregion

        [Header("Settings")] 
        [SerializeField] private List<Resource> _resources;
        public IReadOnlyList<Resource> Resources => _resources;

        #region Utilities
        
        public bool TryGetResource(BaseEntity entity, out Resource output)
        {
            output = null;
            
            foreach (var r in _resources)
            {
                if (r.ResourceType == ResourceType.Resource)
                {
                    if (r.ResourcePrefab.TryGetComponent(out NetworkIdentity identity) &&
                        identity.assetId == entity.netIdentity.assetId)
                    {
                        output = r;
                        break;
                    }
                }
            }

            return output != null;
        }

        public bool TryGetPickupResource(PickupResource pickupResource, out Resource output)
        {
            output = null;
            
            foreach (var r in _resources)
            {
                if (r.ResourceType == ResourceType.PickupResource)
                {
                    if (r.PickupResourcePrefab.TryGetComponent(out NetworkIdentity identity) &&
                        identity.assetId == pickupResource.netIdentity.assetId)
                    {
                        output = r;
                        break;
                    }
                }
            }

            return output != null;
        }
        
        #endregion
    }
}