using SP.Runtime.Core.Systems.Craft;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Builder.PlacementConditions
{
    public class CostPlacementCondition : BasePlacementCondition
    {
        public CostPlacementCondition(Inventory.Inventory ownerInventory, RequiredItem requiredItem)
        {
            _ownerInventory = ownerInventory;
            RequiredItem = requiredItem;
        }

        private readonly Inventory.Inventory _ownerInventory;
        
        public RequiredItem RequiredItem { private get; set; }

        public override bool CanPlace(Vector3 placePosition)
        {
            if (!_ownerInventory.Contains(RequiredItem.Item, RequiredItem.Quantity))
            {
                return false;
            }

            return true;
        }
    }
}