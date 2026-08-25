using System.Collections.Generic;
using Mirror;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Entities.Buildings.CupboardEntity
{
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public class CupboardArea : MonoBehaviour
    {
        public event UnityAction<BaseEntity, BaseEntity.DamageSenderInfo> TargetBuildingDestroyed;

        [Header("References")]
        [SerializeField] private CupboardEntity _cupboardEntity;
        public CupboardEntity CupboardEntity => _cupboardEntity;

        private readonly List<BuildingEntity> _targetBuildings = new ();
        public IReadOnlyList<BuildingEntity> TargetBuildings => _targetBuildings;

        [Header("Settings")] 
        [SerializeField] private float _radius = 20;
        public float Radius => _radius;
        
        private SphereCollider _sphereCollider;
        private SphereCollider SphereCollider
        {
            get
            {
                if (_sphereCollider == null)
                {
                    _sphereCollider = GetComponent<SphereCollider>();
                }

                return _sphereCollider;
            }
        }
        
        private Rigidbody _rigidbody;
        private Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody>();
                }

                return _rigidbody;
            }
        }

        private void OnValidate()
        {
            if (gameObject.layer != LayerUtils.GetIgnoreRaycastLayer())
            {
                gameObject.layer = LayerUtils.GetIgnoreRaycastLayer();
            }
            
            SphereCollider.isTrigger = true;
            SphereCollider.center = Vector3.up;
            SphereCollider.radius = _radius;

            Rigidbody.mass = 1;
            Rigidbody.drag = 0;
            Rigidbody.angularDrag = 0.05f;
            Rigidbody.useGravity = false;
            Rigidbody.isKinematic = true;
            Rigidbody.interpolation = RigidbodyInterpolation.None;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        private void OnDestroy()
        {
            ClearBuildings();
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!ComponentUtils.TryGetComponentInParent<BuildingEntity>(other.transform, out var buildingEntity))
            {
                return;
            }
            
            UpdateTargetBuildings(buildingEntity);
        }
        
        private void UpdateTargetBuildings(BuildingEntity buildingEntity)
        {
            var cupboards = CupboardEntity.GetCupboards(buildingEntity.transform.position);

            foreach (var c in cupboards)
            {
                if (c != _cupboardEntity)
                {
                    continue;
                }

                AddBuilding(buildingEntity);
                break;
            }
        }
        
        private void AddBuilding(BuildingEntity buildingEntity)
        {
            if (_targetBuildings.Contains(buildingEntity))
            {
                return;
            }
            
            buildingEntity.Dead += OnBuildingDead;
            buildingEntity.Destroyed += OnBuildingDestroyed;
                
            _targetBuildings.Add(buildingEntity);
        }
        
        private void RemoveBuilding(BuildingEntity buildingEntity)
        {
            if (!_targetBuildings.Contains(buildingEntity))
            {
                return;
            }
            
            buildingEntity.Dead -= OnBuildingDead;
            buildingEntity.Destroyed -= OnBuildingDestroyed;
                
            _targetBuildings.Remove(buildingEntity);
        }
        
        private void ClearBuildings()
        {
            for (var i = _targetBuildings.Count - 1; i >= 0; i--)
            {
                RemoveBuilding(_targetBuildings[i]);
            }
        }

        #region Callbacks
        
        [ServerCallback]
        private void OnBuildingDead(BaseEntity entity, BaseEntity.DamageSenderInfo damageSenderInfo)
        {
            TargetBuildingDestroyed?.Invoke(entity, damageSenderInfo);

            RemoveBuilding((BuildingEntity)entity);
        }

        private void OnBuildingDestroyed(BaseEntity entity)
        {
            RemoveBuilding((BuildingEntity)entity);
        }
        
        #endregion
    }
}
