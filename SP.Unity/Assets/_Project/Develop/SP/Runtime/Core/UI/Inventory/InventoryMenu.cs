using System;
using System.Collections.Generic;
using FMODUnity;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory
{
    [RequireComponent(typeof(Animation))]
    public class InventoryMenu : MonoBehaviour
    {
        public event UnityAction<Systems.Inventory.Inventory, int, Systems.Inventory.Inventory, int> ItemMoved;
        public event UnityAction<Systems.Inventory.Inventory, int, Systems.Inventory.Inventory> ItemQuickMoved;
        public event UnityAction<Systems.Inventory.Inventory, int> ItemQuickDropped; 
        public event UnityAction<BaseItem, BaseItem.IReadOnlyAction> ItemActionExecuted;
        public event UnityAction<Slot, Slot> ActiveSlotUpdated;

        [Header("Prefabs")]
        [SerializeField] private Slot _slotPrefab;

        [Header("Reference")] 
        [SerializeField] private GameObject _group;
        [SerializeField] private Transform _layoutGroup;
        [SerializeField] private ItemInfoBlock _itemInfoBlock;
        [SerializeField] private TMP_Text _nickNameText;
        [SerializeField] private Transform _slotContainer;
        [SerializeField] private Transform _shortcutSlotContainer;
        
        [SerializeField] private Button _craftButton;
        public Button CraftButton => _craftButton;
        
        [SerializeField] private Button _exitButton;
        public Button ExitButton => _exitButton;

        [SerializeField] private StudioEventEmitter _openEventEmitter;
        [SerializeField] private StudioEventEmitter _closeEventEmitter;
        [SerializeField] private StudioEventEmitter _slotActionEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private Color _activeSlotToMoveColor;
        [SerializeField] private Color _activeSlotToUseColor;

        private bool _isInventoryMenuActive;
        public bool IsInventoryMenuActive => _isInventoryMenuActive;

        private readonly List<Slot> _spawnedSlots = new();
        
        private Slot _activeSlotToMove;
        private Slot ActiveSlotToMove
        {
            get => _activeSlotToMove;
            set
            {
                var oldValue = _activeSlotToMove;
                _activeSlotToMove = value;

                OnActiveSlotToMoveUpdated(oldValue, _activeSlotToMove);
            }
        }

        private Slot _activeSlot;
        public Slot ActiveSlot
        {
            get => _activeSlot;
            private set
            {
                var oldValue = _activeSlot;
                _activeSlot = value;

                OnActiveSlotUpdated(oldValue, _activeSlot);
            }
        }
        
        private const string _appearanceAnimation = "Appearance";
        private const string _disappearanceAnimation = "Disappearance";

        public float DisappearanceAnimationLength { get; private set; }

        private BaseAdditionalBlock _additionalBlockCache;

        private IReadOnlyList<int> _inventorySize;
        
        private Systems.Inventory.Inventory _inventory;
        
        private Animation _animation;
        private Animation Animation
        {
            get
            {
                if (_animation == null)
                {
                    _animation = GetComponent<Animation>();
                }

                return _animation;
            }
        }

        private void Awake()
        {
            DisappearanceAnimationLength = Animation.GetClip(_disappearanceAnimation).length;
        }

        public void Initialize(Systems.Inventory.Inventory inventory, IReadOnlyList<int> inventorySize)
        {
            _inventory = inventory;
            _inventorySize = inventorySize;
            
            OnInventoryUpdated(SyncList<BaseItem>.Operation.OP_ADD, -1, null, null);
            
            _inventory.InventoryUpdated += OnInventoryUpdated;
            _itemInfoBlock.ItemActionExecuted += OnItemActionExecuted;
            
            UpdateInventoryMenu();
        }

        private void OnDestroy()
        {
            _inventory.InventoryUpdated -= OnInventoryUpdated;
            _itemInfoBlock.ItemActionExecuted -= OnItemActionExecuted;
        }

        public void SetOwnerNickName(string nickName)
        {
            _nickNameText.text = nickName;
        }
        
        public void SetView(bool value, BaseAdditionalBlock additionalBlock = null, bool dontAnimate = false, bool muteSound = false)
        {
            _isInventoryMenuActive = value;

            if (value)
            {
                if (additionalBlock != null)
                {
                    if (_additionalBlockCache != null)
                    {
                        Destroy(_additionalBlockCache.gameObject);
                    }

                    _additionalBlockCache = additionalBlock;
                    _additionalBlockCache.transform.SetParent(_layoutGroup.transform);
                    _additionalBlockCache.transform.localScale = Vector3.one;

                    OnAdditionalBlockUpdated(_additionalBlockCache.SpawnedSlots);
                    _additionalBlockCache.TakeAllButtonClicked += OnTakeAllButtonClicked;
                    _additionalBlockCache.StoreAllButtonClicked += OnStoreAllButtonClicked;
                    _additionalBlockCache.DropAllButtonClicked += OnDropAllButtonClicked;
                    _additionalBlockCache.InventoryUpdated += OnAdditionalBlockUpdated;
                    _additionalBlockCache.Destroyed += OnAdditionalBlockDestroyed;
                }
                else
                {
                    if (!muteSound)
                    {
                        _openEventEmitter.Play();
                    }
                }

                if (!dontAnimate)
                {
                    Animation.Play(_appearanceAnimation);
                }
                else
                {
                    _group.SetActive(IsInventoryMenuActive);
                }
            }
            else
            {
                if (_additionalBlockCache != null)
                {
                    Destroy(_additionalBlockCache.gameObject, DisappearanceAnimationLength);
                }
                else
                {
                    if (!muteSound)
                    {
                        _closeEventEmitter.Play();
                    }
                }

                if (!dontAnimate)
                {
                    Animation.Play(_disappearanceAnimation);
                }
                else
                {
                    _group.SetActive(IsInventoryMenuActive);
                }
            }
            
            UpdateInventoryMenu();
        }

        private void UpdateInventoryMenu()
        {
            if (!IsInventoryMenuActive)
            {
                ActiveSlotToMove = null;

                if (ActiveSlot != null)
                {
                    ActiveSlot.SetColor(_activeSlotToUseColor);
                }
            }
            else
            {
                ActiveSlotToMove = null;

                if (ActiveSlot != null)
                {
                    ActiveSlot.ResetColor();
                }
            }
        }

        private void PlaySlotSound()
        {
            _slotActionEventEmitter.Play();
        }

        #region Callbacks
        
        private void OnSlotClicked(Slot slot)
        {
            if (IsInventoryMenuActive)
            {
                if (ActiveSlotToMove == null)
                {
                    ActiveSlotToMove = slot;
                    
                    if (ActiveSlotToMove.Item != null)
                    {
                        PlaySlotSound();
                    }
                    
                    return;
                }
        
                if (ActiveSlotToMove == slot)
                {
                    ActiveSlotToMove = null;
                    return;
                }
        
                if (ActiveSlotToMove.Item != null)
                {
                    ItemMoved?.Invoke(ActiveSlotToMove.Owner, ActiveSlotToMove.Index, slot.Owner, slot.Index);
                    
                    PlaySlotSound();
                    
                    ActiveSlotToMove = null;
                    
                    return;
                }
        
                ActiveSlotToMove = slot;

                if (ActiveSlotToMove.Item != null)
                {
                    PlaySlotSound();
                }
            }
            else
            {
                if (ActiveSlot == null)
                {
                    ActiveSlot = slot;
                    return;
                }
        
                if (ActiveSlot == slot)
                {
                    ActiveSlot = null;
                    return;
                }
        
                ActiveSlot = slot;
            }
        }

        private void OnActiveSlotToMoveUpdated(Slot oldSlot, Slot newSlot)
        {
            if (oldSlot != null)
            {
                oldSlot.ResetColor();
            }

            if (newSlot != null)
            {
                newSlot.SetColor(_activeSlotToMoveColor);
            }
            
            _nickNameText.gameObject.SetActive(newSlot == null || (newSlot != null && newSlot.Item == null));
            _itemInfoBlock.Initialize(newSlot != null ? newSlot.Item : null);
        }

        private void OnActiveSlotUpdated(Slot oldSlot, Slot newSlot)
        {
            if (oldSlot != null)
            {
                oldSlot.ResetColor();
            }

            if (newSlot != null)
            {
                newSlot.SetColor(_activeSlotToUseColor);
            }

            ActiveSlotUpdated?.Invoke(oldSlot, newSlot);
        }

        private void OnProcessExecuted(Slot slot, Slot.ProcessType processType)
        {
            switch (processType)
            {
                case Slot.ProcessType.Move:
                {
                    QuickMove(slot);
                    break;
                }
                case Slot.ProcessType.Drop:
                {
                    QuickDrop(slot);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(processType), processType, null);
                }
            }
        }

        private void QuickMove(Slot slot)
        {
            if (_additionalBlockCache == null)
            {
                return;
            }

            if (_additionalBlockCache.SpawnedSlots.Count <= 0)
            {
                return;
            }
            
            ItemQuickMoved?.Invoke(
                slot.Owner,
                slot.Index,
                slot.Owner == _additionalBlockCache.SpawnedSlots[0].Owner ?
                    _inventory :
                    _additionalBlockCache.SpawnedSlots[0].Owner);
            
            PlaySlotSound();
        }
        
        private void QuickDrop(Slot slot)
        {
            ItemQuickDropped?.Invoke(slot.Owner, slot.Index);
            
            PlaySlotSound();
        }

        private void OnInventoryUpdated(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            _spawnedSlots.Clear();
            
            var parents = new List<Transform>
            {
                _shortcutSlotContainer,
                _slotContainer
            };

            var slots = Slot.SpawnSlots(
                _slotPrefab,
                _inventory,
                parents, 
                _inventory.GetInventory,
                _inventorySize);

            foreach (var slot in slots)
            {
                slot.Clicked -= OnSlotClicked;
                slot.Clicked += OnSlotClicked;
                slot.ProcessExecuted -= OnProcessExecuted;
                slot.ProcessExecuted += OnProcessExecuted;
            }
            
            _spawnedSlots.AddRange(slots);

            ActiveSlotToMove = ActiveSlotToMove;
        }

        private void OnTakeAllButtonClicked()
        {
            if (_additionalBlockCache == null)
            {
                return;
            }

            foreach (var s in _additionalBlockCache.SpawnedSlots)
            {
                s.StartMoveProcess();
            }
        }

        private void OnStoreAllButtonClicked()
        {
            if (_additionalBlockCache == null)
            {
                return;
            }

            foreach (var s in _spawnedSlots)
            {
                s.StartMoveProcess();
            }
        }
        
        private void OnDropAllButtonClicked()
        {
            if (_additionalBlockCache == null)
            {
                return;
            }

            foreach (var s in _additionalBlockCache.SpawnedSlots)
            {
                s.StartDropProcess();
            }
        }
        
        private void OnAdditionalBlockUpdated(IReadOnlyList<Slot> slots)
        {
            foreach (var slot in slots)
            {
                if (slot != null)
                {
                    slot.Clicked -= OnSlotClicked;
                    slot.Clicked += OnSlotClicked;
                    slot.ProcessExecuted -= OnProcessExecuted;
                    slot.ProcessExecuted += OnProcessExecuted;
                }
            }

            ActiveSlotToMove = ActiveSlotToMove;
        }

        private void OnAdditionalBlockDestroyed(BaseAdditionalBlock additionalBlock)
        {
            if (ActiveSlotToMove == null)
            {
                return;
            }

            if (ActiveSlotToMove.transform.IsChildOf(additionalBlock.transform))
            {
                ActiveSlotToMove = null;
            }
        }
        
        private void OnItemActionExecuted(BaseItem item, BaseItem.IReadOnlyAction action)
        {
            ItemActionExecuted?.Invoke(item, action);
        }
        
        #endregion
    }
}
