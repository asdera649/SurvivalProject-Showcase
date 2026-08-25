using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items;

namespace SP.Runtime.Core.Objects
{
    public class CombatObject : PhysicalObject
    {
        protected BaseEntity.DamageSenderInfo Sender { get; private set; }
        protected DamageInfo DamageInfo { get; private set; }

        [SyncVar] private BaseEntity _sender;

        [ServerCallback]
        public void Initialize(BaseEntity.DamageSenderInfo sender, DamageInfo damageInfo)
        {
            Sender = sender;
            DamageInfo = damageInfo;

            _sender = sender.Entity;
        }

        protected bool CheckOwner(BaseEntity entity)
        {
            return _sender != null && _sender == entity;
        }
    }
}
