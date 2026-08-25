using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Entities.Buildings;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.UI.Craft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory.AdditionalBlocks
{
    public class RepairBenchInventoryBlock : BaseAdditionalBlock
    {
        [Header("Prefabs")]
        [SerializeField] private RequiredItem _requiredItemPrefab;

        [Header("Refrences")]
        [SerializeField] private Transform _requiredItemsBlock;
        [SerializeField] private TMP_Text _requiredItemsText;
        [SerializeField] private Button _repairButton;
        
        private Systems.Inventory.Inventory _localInventory;

        private RepairBenchEntity _repairBenchEntity;

        private void Start()
        {
            _repairButton.onClick.AddListener(Repair);
        }

        public override void Initialize<T>(T repairBenchEntity, string nameEntry, Systems.Inventory.Inventory inventory, int[] inventorySize)
        {
            base.Initialize(repairBenchEntity, nameEntry, inventory, inventorySize);
            
            _repairBenchEntity = repairBenchEntity as RepairBenchEntity;
            
            _localInventory = NetworkClient.localPlayer != null ?
                NetworkClient.localPlayer.GetComponent<Systems.Inventory.Inventory>() :
                null;
        
            if (_localInventory != null)
                _localInventory.InventoryUpdated += OnLocalInventoryUpdate;
        
            base.Inventory.InventoryUpdated += OnInventoryUpdate;
        
            UpdateRepairMenu();
        }

        protected override void OnDestroy()
        {
            _repairButton.onClick.RemoveListener(Repair);
            
            if (_localInventory != null)
                _localInventory.InventoryUpdated -= OnLocalInventoryUpdate;

            Inventory.InventoryUpdated -= OnInventoryUpdate;

            base.OnDestroy();
        }

        private void Repair()
        {
            _repairBenchEntity.Repair();
        }

        private void UpdateRepairMenu()
        {
            IReadOnlyList<Systems.Craft.RequiredItem> recipe = null;

            if (_repairBenchEntity.TryGetFirstItem(out var item) &&
                !item.IsStockStrengthFull)
            {
                recipe = _repairBenchEntity.GetRepairRecipe(item);
            }
            
            foreach (Transform child in _requiredItemsBlock.transform)
                Destroy(child.gameObject);

            if (recipe != null)
            {
                _requiredItemsText.enabled = false;

                if (_localInventory != null)
                    _repairButton.interactable = _repairBenchEntity.CanRepair(item, _localInventory);

                foreach (var itemForRepair in recipe)
                {
                    var total = 0;

                    if (_localInventory != null)
                        total = _localInventory.GetTotal(itemForRepair.Item);

                    var requiredItem = Instantiate(_requiredItemPrefab.gameObject, _requiredItemsBlock.transform).GetComponent<RequiredItem>();
                    requiredItem.Initialize(
                        new Systems.Craft.RequiredItem(itemForRepair.Item, itemForRepair.Quantity), total);
                }
            }
            else
            {
                _requiredItemsText.enabled = true;
                _repairButton.interactable = false;
            }
        }

        private void OnInventoryUpdate(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            UpdateRepairMenu();
        }

        private void OnLocalInventoryUpdate(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            UpdateRepairMenu();
        }
    }
}
