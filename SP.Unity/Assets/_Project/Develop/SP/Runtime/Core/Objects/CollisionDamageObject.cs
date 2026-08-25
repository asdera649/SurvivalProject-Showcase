using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class CollisionDamageObject : CombatObject
    {
        [ServerCallback]
        protected override void OnCollision(RaycastHit hit)
        {
            base.OnCollision(hit);
            
            if (ComponentUtils.TryGetComponentInParent<BaseEntity>(hit.transform, out var entity) &&
                CheckOwner(entity))
            {
                return;
            }

            if (entity != null)
            {
                BaseDamagingItem.SendDamage(
                    entity,
                    DamageInfo,
                    Sender);
            }
            
            NetworkServer.Destroy(gameObject);
        }
    }
}