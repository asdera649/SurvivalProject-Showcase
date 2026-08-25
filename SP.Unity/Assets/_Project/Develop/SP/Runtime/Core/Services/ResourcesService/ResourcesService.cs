using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Entities.Mined;
using UnityEngine;

namespace SP.Runtime.Core.Services.ResourcesService
{
    public class ResourcesService : NetworkBehaviour
    {
        #region Structs
        
        private class DestroyedResource
        {
            public DestroyedResource(
                ResourcesCollection.Resource resource,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Resource = resource;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                
                Setup(resource.RestoreTime);
            }
            
            public ResourcesCollection.Resource Resource { get; }
            
            private float _cooldown;
            public float Cooldown
            {
                get => _cooldown;
                private set => _cooldown = Mathf.Clamp(value, 0, float.MaxValue);
            }
            
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }

            private float _lastTime;

            public void Setup(float cooldown)
            {
                Cooldown = cooldown;
                
                _lastTime = Time.time;
            }
            
            public void UpdateCooldown()
            {
                Cooldown -= Time.time - _lastTime;

                _lastTime = Time.time;
            }
        }

        #endregion

        [Header("References")] 
        [SerializeField] private ResourcesData _resourcesData;
        
        [Header("Settings")]
        [SerializeField] private ResourcesCollection _resourcesCollection;
        [SerializeField] private float _casteHeight = 20;
        [SerializeField] private LayerMask _layerMask;
        
        private readonly List<DestroyedResource> _destroyedResources = new();

        private readonly List<BaseEntity> _trackedEntities = new();
        private readonly List<PickupResource> _trackedPickupResources = new();
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            Invoke(nameof(Initialize), 1);
            
            InvokeRepeating(nameof(UpdateTrackedResources), 1, 1);
        }
        
        public override void OnStopServer()
        {
            ClearTrackedResources();
            
            CancelInvoke(nameof(Initialize));
            
            CancelInvoke(nameof(UpdateTrackedResources));
            
            base.OnStopServer();
        }
        
        [ServerCallback]
        private void Initialize()
        {
            foreach (var r in _resourcesData.ResourceContainers)
            {
                InstantiateResource(r.Resource, r.Position, r.Rotation, r.Scale);
            }
        }
        
        [ServerCallback]
        private void UpdateTrackedResources()
        {
            for (var i = _destroyedResources.Count - 1; i >= 0; i--)
            {
                _destroyedResources[i].UpdateCooldown();

                if (_destroyedResources[i].Cooldown > 0)
                {
                    continue;
                }

                switch (_destroyedResources[i].Resource.ResourceType)
                {
                    case ResourcesCollection.ResourceType.Resource:
                    {
                        if (CanRestore(_destroyedResources[i]))
                        {
                            Restore(_destroyedResources[i]);
                            _destroyedResources.Remove(_destroyedResources[i]);
                        }
                        else
                        {
                            _destroyedResources[i].Setup(_destroyedResources[i].Resource.RestoreTime);
                        }
                        
                        break;
                    }
                    case ResourcesCollection.ResourceType.PickupResource:
                    {
                        Restore(_destroyedResources[i]);
                        _destroyedResources.Remove(_destroyedResources[i]);
                        
                        break;
                    }
                }
            }
        }

        private bool CanRestore(DestroyedResource resource)
        {
            return !Physics.SphereCast(
                resource.Position + new Vector3(0, _casteHeight, 0),
                resource.Resource.CastRadius,
                Vector3.down,
                out _,
                _casteHeight,
                _layerMask,
                QueryTriggerInteraction.Ignore);
        }

        [ServerCallback]
        private void Restore(DestroyedResource resource)
        {
            if (resource.Resource.ResourceType == ResourcesCollection.ResourceType.Container)
            {
                Debug.LogError("The containers are not recoverable!");
                return;
            }
            
            InstantiateResource(resource.Resource, resource.Position, resource.Rotation, resource.Scale);
        }
        
        [ServerCallback]
        private void ClearTrackedResources()
        {
            for (var i = _trackedEntities.Count - 1; i >= 0; i--)
            {
                RemoveTrackedEntity(_trackedEntities[i]);
            }
            
            for (var i = _trackedPickupResources.Count - 1; i >= 0; i--)
            {
                RemoveTrackedPickupResource(_trackedPickupResources[i]);
            }
        }

        [ServerCallback]
        private void InstantiateResource(
            ResourcesCollection.Resource resource,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            var instance = Instantiate(resource.GetPrefab(), position, rotation);

            instance.transform.localScale = scale;

            switch (resource.ResourceType)
            {
                case ResourcesCollection.ResourceType.Resource:
                {
                    if (instance.TryGetComponent(out BaseEntity entity))
                    {
                        AddTrackedEntity(entity);
                    }
                        
                    break;
                }
                case ResourcesCollection.ResourceType.PickupResource:
                {
                    if (instance.TryGetComponent(out PickupResource pickupResource))
                    {
                        AddTrackedPickupResource(pickupResource);
                    }
                        
                    break;
                }
            }
            
            NetworkServer.Spawn(instance);
        }

        [ServerCallback]
        private void AddTrackedEntity(BaseEntity entity)
        {
            entity.Dead += OnResourcesDestroy;
            _trackedEntities.Add(entity);
        }
        
        [ServerCallback]
        private void RemoveTrackedEntity(BaseEntity entity)
        {
            entity.Dead -= OnResourcesDestroy;
            _trackedEntities.Remove(entity);
        }
        
        [ServerCallback]
        private void AddTrackedPickupResource(PickupResource pickupResource)
        {
            pickupResource.DestroyAction += OnPickupResourceDestroy;
            _trackedPickupResources.Add(pickupResource);
        }

        [ServerCallback]
        private void RemoveTrackedPickupResource(PickupResource pickupResource)
        {
            pickupResource.DestroyAction -= OnPickupResourceDestroy;
            _trackedPickupResources.Remove(pickupResource);
        }

        #region Callbacks

        [ServerCallback]
        private void OnResourcesDestroy(BaseEntity entity, BaseEntity.DamageSenderInfo sender)
        {
            if (_resourcesCollection.TryGetResource(entity, out var resource))
            {
                _destroyedResources.Add(new DestroyedResource(
                    resource,
                    entity.transform.position,
                    entity.transform.rotation, 
                    entity.transform.localScale));
            }
        }

        [ServerCallback]
        private void OnPickupResourceDestroy(PickupResource pickupResource)
        {
            if (_resourcesCollection.TryGetPickupResource(pickupResource, out var resource))
            {
                _destroyedResources.Add(new DestroyedResource(
                    resource,
                    pickupResource.transform.position,
                    pickupResource.transform.rotation,
                    pickupResource.transform.localScale));
            }
        }
        
        #endregion
    }
}