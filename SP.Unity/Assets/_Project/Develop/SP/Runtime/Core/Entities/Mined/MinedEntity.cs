using Mirror;
using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Mined
{
    public class MinedEntity : BaseEntity
    {
        [Header("Settings")]
        [SerializeField] private BaseItem _receivedItem;
        [SerializeField] private int _receivedQuantity = 1;
        [SerializeField] private DeathMethods[] _deathMethods;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            DamageTook += OnTakeDamage;
        }

        public override void OnStopServer()
        {
            DamageTook -= OnTakeDamage;
            
            base.OnStopServer();
        }
        
        [ServerCallback]
        protected virtual void OnTakeDamage(DamageSenderInfo sender, int damage, DeathMethods deathMethod)
        {
            if (sender.Entity == null ||
                !sender.Entity.TryGetComponent(out Systems.Inventory.Inventory targetInventory))
            {
                return;
            }

            InstantiateItem(targetInventory, damage, deathMethod);
        }

        [ServerCallback]
        private void InstantiateItem(Systems.Inventory.Inventory targetInventory, int damage, DeathMethods deathMethod)
        {
            if (!ContainsDeathMethod(deathMethod))
            {
                return;
            }
            
            var percent = (float)damage / MaxHealth;
            var receivedQuantity = (int)(_receivedQuantity * percent);
            
            var temp = BaseItem.Instantiate(_receivedItem);
            temp.Quantity = receivedQuantity;
            
            targetInventory.Add(temp);
        }

        #region Utilities

        private bool ContainsDeathMethod(DeathMethods deathMethod)
        {
            foreach (var d in _deathMethods)
            {
                if (d == deathMethod)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
