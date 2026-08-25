using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class Rocket : ExplosiveObject
    {
        [Header("References")] 
        [SerializeField] private Transform _smokeTrail;

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (_smokeTrail != null)
            {
                _smokeTrail.SetParent(null);
            }
        }
        
        [ServerCallback]
        protected override void OnCollision(RaycastHit hit)
        {
            base.OnCollision(hit);
            
            if (ComponentUtils.TryGetComponentInParent<BaseEntity>(hit.transform, out var entity) &&
                CheckOwner(entity))
            {
                return;
            }

            Explode(hit.point, hit.transform.GetComponentInParent<BuildingEntity>());
        }
    }
}
