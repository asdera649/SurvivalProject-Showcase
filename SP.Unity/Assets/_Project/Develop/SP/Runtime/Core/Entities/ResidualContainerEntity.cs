using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.Systems.Water.BuoyancyObject;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using UnityEngine;

namespace SP.Runtime.Core.Entities
{
    [RequireComponent(typeof(BuoyancyObject), typeof(Rigidbody), typeof(NetworkRigidbodyUnreliable))]
    public class ResidualContainerEntity : LootableEntity<BaseAdditionalBlock>
    {
        private const float _oneSecond = 1;

        public override void OnStartServer()
        {
            base.OnStartServer();

            InvokeRepeating(nameof(CheckEmpty), _oneSecond, _oneSecond);
        }
        
        public override void OnStopServer()
        {
            CancelInvoke(nameof(CheckEmpty));
            
            base.OnStopServer();
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            var items = Inventory.GetInventory;

            for (var i = 0; i < items.Count; i++)
            {
                OnInventoryUpdated(SyncList<BaseItem>.Operation.OP_ADD, i, null, items[i]);
            }
            
            Inventory.InventoryUpdated += OnInventoryUpdated;
        }

        public override void OnStopClient()
        {
            Inventory.InventoryUpdated -= OnInventoryUpdated;
            
            base.OnStopClient();
        }
        
        [ServerCallback]
        protected override void OnDeath()
        {
            
        }

        [ServerCallback]
        private void CheckEmpty()
        {
            if (Inventory.IsEmpty())
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        private void OnInventoryUpdated(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            // Проблема: из за той особенности что у ResidualContainerEntity размер inventorySize зависит
            // от того какой LootableEntity его породит (например: если ChestEntity то 5, если CupboardEntity то 15),
            // причем изменяется inventorySize у ResidualContainerEntity только на стороне сервера при его порождений
            // (статичным методом ResidualContainerEntity.SpawnResidualContainer), сторона клиента же, остается без
            // изменения, что вызывает некорректное отображение инвентрая на стороне клиента.
            // Решение: изменять inventorySize на стороне клиента при обновлении инвентаря, на число, равное
            // размеру обновленного инвентаря.
            
            if (isClient)
            {
                inventorySize = new[] { Inventory.GetInventory.Count };
            }
        }

        #region Statics
        
        [ServerCallback]
        public static void SpawnResidualContainer(
            ResidualContainerEntity prefab,
            Vector3 position,
            Quaternion rotation,
            Inventory sourceInventory)
        {
            if (sourceInventory.IsEmpty())
            {
                return;
            }
            
            var container = Instantiate(prefab, position, rotation);

            var inventory = sourceInventory.GetInventory;
            
            container.inventorySize = new[] { inventory.Count };

            NetworkServer.Spawn(container.gameObject);
            
            for (var i = 0; i < inventory.Count; i++)
            {
                if (inventory[i] != null)
                {
                    sourceInventory.Move(i, container.Inventory);
                }
            }
        }
        
        #endregion
    }
}