using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Objects;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/ThrowableCombatItem")]
    public class ThrowableCombatItem : ThrowingItem
    {
        protected override void OnThrow(PhysicalObject throwingObject)
        {
            base.OnThrow(throwingObject);

            if (!isServer)
            {
                return;
            }

            if (throwingObject is CombatObject combatObject)
            {
                combatObject.Initialize(new BaseEntity.DamageSenderInfo(Entity), DamageInfo);
            }
        }
    }
}
