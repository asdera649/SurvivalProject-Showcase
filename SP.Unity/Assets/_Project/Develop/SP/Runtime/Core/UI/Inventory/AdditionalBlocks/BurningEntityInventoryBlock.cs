using SP.Runtime.Core.Entities;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory.AdditionalBlocks
{
    public class BurningEntityInventoryBlock : BaseAdditionalBlock
    {
        [Header("Refrences")]
        [SerializeField] private Button _button;
        [SerializeField] private LocalizeStringEvent _buttonTextLocalize;
        
        private Color _backupColor;
    
        private BaseBurningEntity _burningEntity;

        #region Logics
        
        private void Awake()
        {
            _backupColor = _button.image.color;
        }

        private void Start()
        {
            _button.onClick.AddListener(OnClick);
        }

        public override void Initialize<T>(T burningEntity, string nameEntry, Systems.Inventory.Inventory inventory, int[] inventorySize)
        {
            base.Initialize(burningEntity, nameEntry, inventory, inventorySize);
        
            _burningEntity = burningEntity as BaseBurningEntity;
        
            OnEnableUpdate();
        }

        protected override void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
            
            base.OnDestroy();
        }

        private void Update()
        {
            OnEnableUpdate();
        }
        
        #endregion

        #region Callbacks
        
        private void OnClick()
        {
            _burningEntity.Enable();
        }

        private void OnEnableUpdate()
        {
            if (_burningEntity.IsEnable)
            {
                _button.image.color = new Color(
                    Color.red.r,
                    Color.red.g,
                    Color.red.b,
                    _button.image.color.a);
                
                _buttonTextLocalize.SetEntry("Off");
            }
            else
            {
                _button.image.color = _backupColor;
                _buttonTextLocalize.SetEntry("On");
            }
        }
        
        #endregion
    }
}
