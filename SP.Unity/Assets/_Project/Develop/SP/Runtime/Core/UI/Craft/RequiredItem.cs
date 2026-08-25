using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Craft
{
    public class RequiredItem : MonoBehaviour
    {
        [SerializeField] private GameObject _dummy;
        [SerializeField] private GameObject _group;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _requiredQuantityText;
        [SerializeField] private TMP_Text _totalText;
        
        private Color _backupColor;

        #region Logics
        
        private void Awake()
        {
            _backupColor = _totalText.color;
        }

        public void Initialize(Systems.Craft.RequiredItem requiredItem, int total)
        {
            _dummy.SetActive(requiredItem == null);
            _group.SetActive(requiredItem != null);

            if (requiredItem == null) return;
            
            _icon.sprite = requiredItem.Item.Icon;
            _totalText.color = (requiredItem.Quantity > total) ? Color.red : _backupColor;
            _totalText.text = "x" + total.ToString();
            _requiredQuantityText.text = "-" + requiredItem.Quantity.ToString();
        }
        
        #endregion
    }
}
