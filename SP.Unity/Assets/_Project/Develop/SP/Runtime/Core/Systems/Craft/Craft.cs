using Mirror;
using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Craft
{
    [RequireComponent(typeof(Inventory.Inventory), typeof(WorkbenchLevelHandler))]
    public class Craft : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private CraftCollection _craftCollection;
        public CraftCollection CraftCollection => _craftCollection;
        
        private Inventory.Inventory _inventory;
        public Inventory.Inventory Inventory
        {
            get
            {
                if (_inventory == null)
                {
                    _inventory = GetComponent<Inventory.Inventory>();
                }

                return _inventory;
            }
        }
        
        private WorkbenchLevelHandler _workbenchLevelHandler;
        public WorkbenchLevelHandler WorkbenchLevelHandler
        {
            get
            {
                if (_workbenchLevelHandler == null)
                {
                    _workbenchLevelHandler = GetComponent<WorkbenchLevelHandler>();
                }

                return _workbenchLevelHandler;
            }
        }

        public bool CanCraft(CraftingRecipe craftingRecipe, int quantity = 1)
        {
            quantity = Mathf.Clamp(quantity, 1, int.MaxValue);
            
            foreach (var i in craftingRecipe.ItemsForCrafting)
            {
                if (Inventory.GetTotal(i.Item) < i.Quantity * quantity)
                {
                    return false;
                }
            }

            if (WorkbenchLevelHandler.CurrentWorkbenchLevel < craftingRecipe.RequiredWorkbenchLevel)
            {
                return false;
            }

            return true;
        }
        
        [ServerCallback]
        public void CraftItem(CraftingRecipe craftingRecipe, int quantity = 1)
        {
            quantity = Mathf.Clamp(quantity, 1, int.MaxValue);

            if (!CanCraft(craftingRecipe, quantity))
            {
                return;
            }

            foreach (var i in craftingRecipe.ItemsForCrafting)
            {
                Inventory.Remove(i.Item, i.Quantity * quantity);
            }

            var item = BaseItem.Instantiate(craftingRecipe.ReceivedItem);
            item.Quantity = quantity;
            Inventory.Add(item);
        }
    }
}