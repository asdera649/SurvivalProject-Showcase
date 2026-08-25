using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Localization;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Craft
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class CraftingItem : MonoBehaviour
    {
        public event UnityAction<CraftingItem> Clicked;

        [Header("Reference")]
        [SerializeField] private Image _icon;
        [SerializeField] private Image _requiredWorkbenchBlock;
        [SerializeField] private LocalizeStringHelper _requiredWorkbenchLocalize;

        [SerializeField] private Material _silhouetteMaterial;

        [Header("Settings")] 
        [SerializeField] private Color _requiredWorkbenchBlockInteractivityColor;

        private CraftingRecipe _craftingRecipe;
        public CraftingRecipe CraftingRecipe => _craftingRecipe;
        
        private const float _activeInteractableTransparency = 0.85f;
        private const float _inactiveInteractableTransparency = 0.25f;
        
        private Color _backupColor;
        private Color _requiredWorkbenchBlockBackupColor;
        
        private Image _image;
        private Image Image
        {
            get
            {
                if (_image == null)
                {
                    _image = GetComponent<Image>();
                }
                
                return _image;
            }
        }
        
        private Button _button;
        private Button Button
        {
            get
            {
                if (_button == null)
                {
                    _button = GetComponent<Button>();
                }
                
                return _button;
            }
        }
        
        private void Awake()
        {
            _backupColor = Image.color;
            _requiredWorkbenchBlockBackupColor = _requiredWorkbenchBlock.color;
        }

        private void OnEnable()
        {
            Button.onClick.AddListener(OnClick);
        }
        
        private void OnDisable()
        {
            Button.onClick.RemoveListener(OnClick);
        }
        
        public void Initialize(CraftingRecipe craftingRecipe)
        {
            _craftingRecipe = craftingRecipe;

            _icon.sprite = CraftingRecipe.ReceivedItem.Icon;
            _requiredWorkbenchBlock.gameObject.SetActive(CraftingRecipe.RequiredWorkbenchLevel > 0);

            if (CraftingRecipe.RequiredWorkbenchLevel > 0)
            {
                _requiredWorkbenchLocalize.SetEntry(new EntryContainer(
                    "lv.",
                    new LocalVariable[]
                    {
                        new("level", new IntVariable() { Value = CraftingRecipe.RequiredWorkbenchLevel })
                    }));
            }
            else
            {
                _requiredWorkbenchLocalize.ClearEntry();
            }
        }
        

        public void SetInteractable(bool value)
        {
            //Button.interactable = value;
            _icon.material = value ? null : _silhouetteMaterial;
            
            _requiredWorkbenchBlock.color = value ? 
                _requiredWorkbenchBlockInteractivityColor : _requiredWorkbenchBlockBackupColor;
        }

        public void SetUnavailability(bool value)
        {
            _icon.color = ColorUtils.ChangeColorAlpha(
                _icon.color,
                value ? _inactiveInteractableTransparency : _activeInteractableTransparency);
        }

        public void SetColor(Color color)
        {
            Image.color = color;
        }

        public void ResetColor()
        {
            Image.color = _backupColor;
        }

        #region Callbacks

        private void OnClick()
        {
            Clicked?.Invoke(this);
        }
        
        #endregion
    }
}
