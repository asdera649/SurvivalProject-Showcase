using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Buildings
{
    public class RepairBenchEntity : LootableEntity<RepairBenchInventoryBlock>
    {
        [Header("Settings")]
        [SerializeField] private CraftCollection _craftCollection;
        
        [ClientCallback]
        public void Repair()
        {
            CmdRepair();
        }

        [Command(requiresAuthority = false)]
        private void CmdRepair(NetworkConnectionToClient sender = null)
        {
            if (sender == null ||
                !sender.identity.TryGetComponent(out Systems.Inventory.Inventory targetInventory))
            {
                return;
            }
            
            DoRepair(targetInventory);
        }
        
        public bool CanRepair(BaseItem item, Systems.Inventory.Inventory targetInventory)
        {
            if (item.IsStockStrengthFull)
            {
                return false;
            }
            
            var recipe = GetRepairRecipe(item);

            foreach (var r in recipe)
            {
                if (!targetInventory.Contains(r.Item, r.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        [ServerCallback]
        private void DoRepair(Systems.Inventory.Inventory targetInventory)
        {
            if (TryGetFirstItem(out var targetItem))
            {
                if (CanRepair(targetItem, targetInventory))
                {
                    var recipe = GetRepairRecipe(targetItem);

                    foreach (var r in recipe)
                    {
                        targetInventory.Remove(r.Item, r.Quantity);
                    }

                    targetItem.SetFullStrength();
                }
            }
        }

        #region Utilities

        public bool TryGetFirstItem(out BaseItem output)
        {
            output = null;
            
            var items = Inventory.GetInventory;

            if (items.Count > 0)
            {
                output = items[0];
            }

            return output != null;
        }

        public IReadOnlyList<RequiredItem> GetRepairRecipe(BaseItem item)
        {
            List<RequiredItem> outputs = new();
            
            if (item.ConsiderStrength)
            {
                if (_craftCollection.TryGetCraftRecipe(item, out var output))
                {
                    foreach (var i in output.ItemsForCrafting)
                    {
                        if (i.Quantity > 1)
                        {
                            var value = (int)Mathf.Lerp(1, (float)i.Quantity / 2, 1 - item.StockStrength);
                            
                            outputs.Add(new RequiredItem(i.Item, Mathf.Clamp(value, 1, i.Quantity)));
                        }
                    }
                }
            }

            return outputs;
        }

        #endregion
    }
}
