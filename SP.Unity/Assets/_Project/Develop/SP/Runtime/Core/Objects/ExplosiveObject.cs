using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Building.Utilities;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class ExplosiveObject : CombatObject
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _explosionPrefab;
        
        [Header("Settings")]
        [SerializeField] private AnimationCurve _damageSpread;
        [SerializeField] private float _explosionRadius = 1;

        [ServerCallback]
        protected void Explode(Vector3 position = default, BuildingEntity building = null)
        {
            var explodedPosition = position == default ? transform.position : position;
            
            var results = PhysicUtils.GetBySphere<BaseEntity>(
                explodedPosition,
                _explosionRadius,
                -1,
                QueryTriggerInteraction.Ignore);
  
            BuildingEntity targetBuilding = null;

            var targetBuildingClosestPoint = explodedPosition;
            
            var distanceToTargetBuilding = float.MaxValue;
            
            foreach (var r in results)
            {
                var closestPoint = explodedPosition;
            
                var distanceToClosestPoint = float.MaxValue;
                
                foreach (var c in r.Colliders)
                {
                    var closestPointOnCollider = PhysicUtils.GetClosestPoint(c, explodedPosition);

                    closestPointOnCollider += (explodedPosition - closestPointOnCollider).normalized *
                                              CollisionOffset;
                    
                    var distance = Vector3.Distance(explodedPosition, closestPointOnCollider);
                    
                    if (distance < distanceToClosestPoint)
                    {
                        closestPoint = closestPointOnCollider;
                        distanceToClosestPoint = distance;
                    }
                }
                
                if (!BuildUtils.Linecast(
                        explodedPosition,
                        closestPoint,
                        out _,
                        -1,
                        QueryTriggerInteraction.Ignore))
                {
                    if (r.Object is BuildingEntity b)
                    {
                        var distance = Vector3.Distance(explodedPosition, closestPoint);
                        
                        if (distance < distanceToTargetBuilding)
                        {
                            targetBuilding = b;
                            targetBuildingClosestPoint = closestPoint;
                            distanceToTargetBuilding = distance;
                        }

                        continue;
                    }
                    
                    SendDamage(r.Object, explodedPosition, closestPoint);
                }
            }

            if (building != null)
            {
                targetBuilding = building;
                targetBuildingClosestPoint = explodedPosition;
            }

            if (targetBuilding != null)
            {
                SendDamage(targetBuilding, explodedPosition, targetBuildingClosestPoint);
            }
            
            InstantiateExplosion(explodedPosition);
            
            NetworkServer.Destroy(gameObject);
        }

        [ServerCallback]
        private void SendDamage(BaseEntity target, Vector3 explodedPosition, Vector3 closestPoint)
        {
            var percent = Mathf.Clamp01(
                Vector3.Distance(explodedPosition, closestPoint) / _explosionRadius);
            
            BaseDamagingItem.SendDamage(
                target,
                DamageInfo,
                Sender,
                _damageSpread.Evaluate(Mathf.Clamp01(1 - percent)));
        }

        [ServerCallback]
        private void InstantiateExplosion(Vector3 position)
        {
            var explosion = Instantiate(_explosionPrefab, position, Quaternion.identity);
            
            NetworkServer.Spawn(explosion);
        }
    }
}
