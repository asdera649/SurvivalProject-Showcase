using System;
using System.Collections.Generic;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Craft
{
    public class CraftMenu : MonoBehaviour
    {
        #region Structs
        
        [Serializable]
        public class Section
        {
            [SerializeField] private BaseItem.ItemSections _itemSection;
            public BaseItem.ItemSections ItemSection => _itemSection;

            [SerializeField] private string _nameLocalize;
            public string NameLocalize => _nameLocalize;

            [SerializeField] private Sprite _sectionIcon;
            public Sprite SectionIcon => _sectionIcon;
        }
        
        #endregion
        
        public event UnityAction<CraftingRecipe, int> CraftAction;

        [Header("Prefabs")] 
        [SerializeField] private CraftingItem _craftingItemPrefab;
        [SerializeField] private RequiredItem _requiredItemPrefab;
        [SerializeField] private Craft.Section _sectionPrefab;
        [SerializeField] private GameObject _dummyCraftableItemPrefab;

        [Header("Reference")] 
        [SerializeField] private GameObject _group;
        
        [SerializeField] private Transform _sectionsBlock;
        [SerializeField] private Transform _craftableItemsContainer;
        [SerializeField] private GameObject _infoBlock;
        [SerializeField] private Transform _requiredItemsBlock;
        
        [SerializeField] private LocalizeStringEvent _nameLocalize;
        [SerializeField] private LocalizeStringHelper _requiredWorkbenchTextLocalize;
        [SerializeField] private TMP_Text _requiredWorkbenchText;
        [SerializeField] private LocalizeStringEvent _descriptionLocalize;
        [SerializeField] private SuccessBlock _successBlock;
        [SerializeField] private Slider _quantitySlider;
        [SerializeField] private TMP_Text _totalQuantityText;
        [SerializeField] private CraftButton _craftButton;
        [SerializeField] private Button _exitButton;
        public Button ExitButton => _exitButton; // В будущем подпишемся на onClick из вне

        [Header("Settings")]
        [SerializeField] private Color _activeCraftableItemColor;
        [SerializeField] private List<Section> _usedSections = new();
        
        private bool _isCraftMenuActive;
        public bool IsCraftMenuActive => _isCraftMenuActive;

        private int GetCraftingQuantity => (int)_quantitySlider.value;
        
        private Craft.Section _activeSection;
        private Craft.Section ActiveSection
        {
            get => _activeSection;
            set
            {
                var temp = _activeSection;
                _activeSection = value;
                
                OnActiveSectionUpdate(temp, _activeSection);
            }
        }

        private CraftingItem _activeCraftingItem;
        private CraftingItem ActiveCraftingItem
        {
            get => _activeCraftingItem;
            set
            {
                var temp = _activeCraftingItem;
                _activeCraftingItem = value;

                OnActiveCraftableItemUpdate(temp, _activeCraftingItem);
            }
        }
        
        private readonly List<CraftingItem> _craftingItems = new();
        private readonly List<Craft.Section> _sections = new();
        private readonly List<RequiredItem> _requiredItems = new();

        private Systems.Craft.Craft _craft;
        
        private void Start()
        {
            _craftButton.HoldCompleted += OnCraftButtonHoldCompleted;
            _quantitySlider.onValueChanged.AddListener(OnQuantitySliderUpdate);
        }

        public void Initialize(Systems.Craft.Craft craft)
        {
            _craft = craft;
            
            foreach (var recipe in _craft.CraftCollection.CraftingRecipes)
            {
                var craftingItem = Instantiate(
                        _craftingItemPrefab.gameObject,
                        _craftableItemsContainer).GetComponent<CraftingItem>();
            
                craftingItem.Initialize(recipe);
                craftingItem.Clicked += OnCraftableItemClick;
                _craftingItems.Add(craftingItem);
            }

            foreach (var s in _usedSections)
            {
                var section = Instantiate(
                        _sectionPrefab.gameObject,
                        _sectionsBlock.transform).GetComponent<Craft.Section>();
            
                section.Initialize(s);
                section.ClickAction += OnSectionButtonClick;
                _sections.Add(section);
            }

            for (var i = 0; i < 3; i++)
                Instantiate(_dummyCraftableItemPrefab, _craftableItemsContainer);

            for (var i = 0; i < 4; i++)
            {
                var requiredItem = Instantiate(
                    _requiredItemPrefab.gameObject,
                    _requiredItemsBlock).GetComponent<RequiredItem>();
                
                requiredItem.Initialize(null, 0);
                
                _requiredItems.Add(requiredItem);
            }

            SetFirstSection();
            
            _craft.WorkbenchLevelHandler.WorkbenchLevelUpdateAction += OnWorkbenchLevelUpdate;

            UpdateCraftMenu();
        }
        
        private void OnDestroy()
        {
            _craftButton.HoldCompleted -= OnCraftButtonHoldCompleted;
            _quantitySlider.onValueChanged.RemoveListener(OnQuantitySliderUpdate);

            for (var i = _craftingItems.Count - 1; i >= 0; i--)
            {
                _craftingItems[i].Clicked -= OnCraftableItemClick;
                Destroy(_craftingItems[i].gameObject);
                _craftingItems.RemoveAt(i);
            }

            for (var i = _sections.Count - 1; i >= 0; i--)
            {
                _sections[i].ClickAction -= OnSectionButtonClick;
                Destroy(_sections[i].gameObject);
                _sections.RemoveAt(i);
            }

            for (var i = _requiredItems.Count - 1; i >= 0; i--)
            {
                Destroy(_requiredItems[i].gameObject);
                _requiredItems.RemoveAt(i);
            }

            if (_craft == null)
            {
                return;
            }
            
            _craft.WorkbenchLevelHandler.WorkbenchLevelUpdateAction -= OnWorkbenchLevelUpdate;
        }
        
        public void SetView(bool value)
        {
            _isCraftMenuActive = value;

            if (!value)
                ActiveCraftingItem = null;

            UpdateCraftMenu();
        }
        
        private void SetFirstSection()
        {
            var sectionTransform = _sectionsBlock.transform.GetChild(0);
            var section = sectionTransform != null ? sectionTransform.GetComponent<Craft.Section>() : null;
            if (section == null) return;
            
            ActiveSection = section;
        }
        
        public void UpdateCraftMenu()
        {
            _group.SetActive(_isCraftMenuActive);
            
            if (!_isCraftMenuActive) return;
            
            foreach (var i in _craftingItems)
            {
                i.SetUnavailability(!_craft.CanCraft(i.CraftingRecipe));

                var value = i.CraftingRecipe.RequiredWorkbenchLevel <= _craft.WorkbenchLevelHandler.CurrentWorkbenchLevel;

                if (!value)
                {
                    if (ActiveCraftingItem == i)
                    {
                        ActiveCraftingItem = null;
                    }
                }
                
                i.SetInteractable(value);
            }

            ActiveSection = ActiveSection;

            if (ActiveCraftingItem != null)
                ActiveCraftingItem = ActiveCraftingItem.gameObject.activeSelf ? ActiveCraftingItem : null;
        }

        #region Callbacks
        
        private void OnCraftButtonHoldCompleted()
        {
            CraftAction?.Invoke(ActiveCraftingItem.CraftingRecipe, GetCraftingQuantity);
            
            _successBlock.ShowSuccessText(
                "{Successful} - {" + ActiveCraftingItem.CraftingRecipe.ReceivedItem.Name + "}(x"+ GetCraftingQuantity + ")");
        }
        
        private void OnWorkbenchLevelUpdate()
        {
            UpdateCraftMenu();
        }

        private void OnCraftableItemClick(CraftingItem craftingItem)
        {
            if (ActiveCraftingItem != craftingItem)
                ActiveCraftingItem = craftingItem;
        }

        private void OnSectionButtonClick(Craft.Section section)
        {
            if (ActiveSection != section)
                ActiveSection = section;
        }

        private void OnActiveCraftableItemUpdate(CraftingItem oldValue, CraftingItem newValue, bool antiStackOverflow = false)
        {
            var color = Color.white;
            var entry = new EntryContainer("WorkbenchNotRequired", Array.Empty<LocalVariable>());
            
            if (oldValue != null)
            {
                oldValue.ResetColor();
            }
            
            if (newValue != null)
            {
                newValue.SetColor(_activeCraftableItemColor);

                _nameLocalize.SetEntry(newValue.CraftingRecipe.ReceivedItem.Name);
                _descriptionLocalize.SetEntry(newValue.CraftingRecipe.ReceivedItem.Description);
                
                if (newValue.CraftingRecipe.RequiredWorkbenchLevel > _craft.WorkbenchLevelHandler.CurrentWorkbenchLevel)
                {
                    color = Color.red;
                }
                
                if (newValue.CraftingRecipe.RequiredWorkbenchLevel > 0)
                {
                    entry = new EntryContainer(
                        "RequiredWorkbenchLevel",
                        new LocalVariable[]
                        {
                            new("level", new IntVariable() { Value = newValue.CraftingRecipe.RequiredWorkbenchLevel })
                        });
                }
            
                if (oldValue != newValue)
                {
                    _quantitySlider.maxValue = newValue.CraftingRecipe.ReceivedItem.MaxQuantity;
                    _quantitySlider.value = 1;
                }
            }
            
            _requiredWorkbenchText.color = color;
            _requiredWorkbenchTextLocalize.SetEntry(entry);

            _infoBlock.SetActive(newValue != null);
            _quantitySlider.gameObject.SetActive(newValue != null && newValue.CraftingRecipe.CraftWholesale);
            _totalQuantityText.gameObject.SetActive(newValue != null && newValue.CraftingRecipe.CraftWholesale);
            _craftButton.Button.interactable = newValue != null && _craft.CanCraft(newValue.CraftingRecipe, GetCraftingQuantity);

            if (!antiStackOverflow)
                OnQuantitySliderUpdate(GetCraftingQuantity);
        }

        private void OnActiveSectionUpdate(Craft.Section oldValue, Craft.Section newValue)
        {
            if (oldValue != newValue)
                ActiveCraftingItem = null;

            if (oldValue != null)
                oldValue.ResetColor();

            if (newValue != null)
            {
                newValue.SetColor(_activeCraftableItemColor);

                foreach (var craftableItem in _craftingItems)
                {
                    craftableItem.gameObject.SetActive(
                        craftableItem.CraftingRecipe.ReceivedItem.ItemSection == newValue.ItemSection);
                }
            }
        }

        private void OnQuantitySliderUpdate(float value)
        {
            if (ActiveCraftingItem != null)
            {
                for (var i = 0; i < _requiredItems.Count; i++)
                {
                    if (ActiveCraftingItem.CraftingRecipe.ItemsForCrafting.Count > i)
                    {
                        _requiredItems[i].Initialize(new Systems.Craft.RequiredItem(
                            ActiveCraftingItem.CraftingRecipe.ItemsForCrafting[i].Item,
                            ActiveCraftingItem.CraftingRecipe.ItemsForCrafting[i].Quantity * (int)value),
                            _craft.Inventory.GetTotal(ActiveCraftingItem.CraftingRecipe.ItemsForCrafting[i].Item));
                    }
                    else
                    {
                        _requiredItems[i].Initialize(null, 0);
                    }
                }

                foreach (var i in _requiredItems)
                    i.gameObject.SetActive(true);

                OnActiveCraftableItemUpdate(ActiveCraftingItem, ActiveCraftingItem, true);
            }
            else
            {
                foreach (var i in _requiredItems)
                    i.gameObject.SetActive(false);
            }

            _totalQuantityText.text = "x" + value;
        }
        
        #endregion
    }
}
