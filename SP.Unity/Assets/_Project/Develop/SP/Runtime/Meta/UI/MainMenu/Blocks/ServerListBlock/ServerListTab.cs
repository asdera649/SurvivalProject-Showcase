using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock
{
    [RequireComponent(typeof(Button))]
    public class ServerListTab : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _text;

        [Header("Settings")] 
        [SerializeField] private Color _activeTabColor;
        [SerializeField] private Color _inactiveTabColor;
        
        [SerializeField] private Color _activeTabIconColor;
        [SerializeField] private Color _inactiveTabIconColor;
        
        [SerializeField] private Color _activeTabTextColor;
        [SerializeField] private Color _inactiveTabTextColor;

        private Button _button;
        public Button Button
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
        
        public void SetActive(bool value)
        {
            Button.image.color = value ? _activeTabColor : _inactiveTabColor;
            _icon.color = value ? _activeTabIconColor : _inactiveTabIconColor;
            _text.color = value ? _activeTabTextColor : _inactiveTabTextColor;
        }
    }
}