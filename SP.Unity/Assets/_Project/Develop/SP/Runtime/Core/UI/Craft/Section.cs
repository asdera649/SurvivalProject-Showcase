using SP.Runtime.Core.Items;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Craft
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class Section : MonoBehaviour
    {
        public event UnityAction<Section> ClickAction;

        [SerializeField] private Image _icon;
        [SerializeField] private LocalizeStringEvent _nameLocalize;

        private BaseItem.ItemSections _itemSections;
        public BaseItem.ItemSections ItemSection => _itemSections;
        
        private Color _backupColor;

        private Image _image;
        private Image image
        {
            get
            {
                if (_image == null)
                    _image = GetComponent<Image>();
                
                return _image;
            }
        }
        
        private Button _button;
        private Button button
        {
            get
            {
                if (_button == null)
                    _button = GetComponent<Button>();
                
                return _button;
            }
        }

        #region Logics

        private void Awake()
        {
            _backupColor = image.color;
        }

        private void Start()
        {
            button.onClick.AddListener(OnClick);
        }

        public void Initialize(CraftMenu.Section section)
        {
            _itemSections = section.ItemSection;
            
            _nameLocalize.SetEntry(section.NameLocalize);
            _icon.sprite = section.SectionIcon;
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClick);
        }

        #endregion
        
        public void SetColor(Color color)
        {
            image.color = color;
        }

        public void ResetColor()
        {
            image.color = _backupColor;
        }

        #region Callbacks

        private void OnClick()
        {
            ClickAction?.Invoke(this);
        }
        
        #endregion
    }
}
