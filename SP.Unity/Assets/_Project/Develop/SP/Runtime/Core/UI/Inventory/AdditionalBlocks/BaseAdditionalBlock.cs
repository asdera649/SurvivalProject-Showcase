using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Items;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory.AdditionalBlocks
{
    public class BaseAdditionalBlock : MonoBehaviour
    {
        public event UnityAction TakeAllButtonClicked;
        public event UnityAction StoreAllButtonClicked;
        public event UnityAction DropAllButtonClicked;
        public event UnityAction<IReadOnlyList<Slot>> InventoryUpdated;
        public event UnityAction<BaseAdditionalBlock> Destroyed;

        [Header("Prefabs")]
        [SerializeField] private Slot _slotPrefab;
        [SerializeField] private Image _dummySlotPrefab;

        [Header("Refrences")] 
        [SerializeField] private LocalizeStringEvent _nameLocalize;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private GridLayoutGroup _slotContainerLayoutGroup;
        [SerializeField] private Button _takeAllButton;
        [FormerlySerializedAs("_addAllButton")] [SerializeField] private Button _storeAllButton;
        [SerializeField] private Button _dropAllButton;

        [Header("Layout Group Settings")] 
        [Tooltip("Пороговое значение слотов, по превышению которых, GridLayoutGroup переводится в режим " +
                 "фиксированного количества строк.")]
        [SerializeField] private int _thresholdSlotsCount = 20;
        
        [Tooltip("Количество строк которое установится для GridLayoutGroup," +
                 " после перевода в режим фиксированного количества строк.")]
        [SerializeField] private int _targetRowsCount = 4;

        private IReadOnlyList<Slot> _spawnedSlots;
        public IReadOnlyList<Slot> SpawnedSlots => _spawnedSlots;

        private readonly List<Image> _dummySlots = new();
        
        private int[] _inventorySize;
        
        private Systems.Inventory.Inventory _inventory;
        protected Systems.Inventory.Inventory Inventory => _inventory;

        private int _constraintCountBackup = -1;
        private GridLayoutGroup.Constraint _constraintBackup;
        
        private void BackupConstraint()
        {
            if (_constraintCountBackup != -1)
            {
                return;
            }
            
            _constraintCountBackup = _slotContainerLayoutGroup.constraintCount;
            _constraintBackup = _slotContainerLayoutGroup.constraint;
        }
        
        public virtual void Initialize<T>(T entity, string nameEntry, Systems.Inventory.Inventory inventory, int[] inventorySize)
        {
            BackupConstraint();
            
            _nameLocalize.SetEntry(nameEntry);
            
            _inventory = inventory;
            _inventorySize = inventorySize;
            
            OnInventoryUpdated(SyncList<BaseItem>.Operation.OP_ADD, 0, null, null);
            
            _inventory.InventoryUpdated += OnInventoryUpdated;
            
            _takeAllButton.onClick.AddListener(OnTakeAllButtonClicked);
            _storeAllButton.onClick.AddListener(OnAddAllButtonClicked);
            _dropAllButton.onClick.AddListener(OnDropAllButtonClicked);
        }

        protected virtual void OnDestroy()
        {
            _inventory.InventoryUpdated -= OnInventoryUpdated;
            
            _takeAllButton.onClick.RemoveAllListeners();
            _storeAllButton.onClick.RemoveAllListeners();
            _dropAllButton.onClick.RemoveAllListeners();
            
            Destroyed?.Invoke(this);
        }
        
        #region Callbacks

        private void OnTakeAllButtonClicked()
        {
            TakeAllButtonClicked?.Invoke();
        }

        private void OnAddAllButtonClicked()
        {
            StoreAllButtonClicked?.Invoke();
        }
        
        private void OnDropAllButtonClicked()
        {
            DropAllButtonClicked?.Invoke();
        }
        
        private void OnInventoryUpdated(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            foreach (var s in _dummySlots)
            {
                Destroy(s.gameObject);
            }
            
            _dummySlots.Clear();
            
            var parents = new[] { _slotContainer.transform };

            _spawnedSlots = Slot.SpawnSlots(
                _slotPrefab,
                _inventory,
                parents,
                _inventory.GetInventory,
                _inventorySize);

            if (_thresholdSlotsCount > 0 && _targetRowsCount > 0)
            {
                _slotContainerLayoutGroup.constraintCount = _spawnedSlots.Count > _thresholdSlotsCount 
                    ? _targetRowsCount
                    : _constraintCountBackup;
                
                _slotContainerLayoutGroup.constraint = _spawnedSlots.Count > _thresholdSlotsCount
                    ? GridLayoutGroup.Constraint.FixedRowCount
                    : _constraintBackup;
            }
            
            var dummySlotsCount = 0;

            // Не самый оптимальный способ, но пока оставлю...
            if (_slotContainerLayoutGroup.constraint is 
                GridLayoutGroup.Constraint.FixedColumnCount or
                GridLayoutGroup.Constraint.FixedRowCount)
            {
                var tempSpawnedSlotsCount = _spawnedSlots.Count;
                
                while (tempSpawnedSlotsCount % _slotContainerLayoutGroup.constraintCount > 0)
                {
                    tempSpawnedSlotsCount++;
                    dummySlotsCount++;
                }
            }

            for (var i = 0; i < dummySlotsCount; i++)
            {
                _dummySlots.Add(Instantiate(_dummySlotPrefab, _slotContainer));
            }

            InventoryUpdated?.Invoke(_spawnedSlots);
        }
        
        #endregion
    }
}
