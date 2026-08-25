using SP.Runtime.Core.Systems.Input;
using TMPro;
using UnityEngine;

namespace SP.Runtime.Core.UI.UIControls
{
    public class ReloadButton : ClassicButton
    {
        [Header("References")] 
        [SerializeField] private TMP_Text _totalAmmoText;

        [Header("Settings")] 
        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _endColor;

        private ReloadInputAction _reloadInputAction;
        
        public void Initialize(ReloadInputAction reloadInputAction, Sprite icon)
        {
            base.Initialize(icon);
            
            Icon = icon;

            _reloadInputAction = reloadInputAction;

            UpdateTotalAmmoText();
        }

        protected override void Update()
        {
            base.Update();

            UpdateTotalAmmoText();
        }

        private void UpdateTotalAmmoText()
        {
            _totalAmmoText.text = "x" + _reloadInputAction.TotalAmmo.ToString();

            _totalAmmoText.color = _reloadInputAction.TotalAmmo > 0 ? _normalColor : _endColor;
        }
    }
}