using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.DeveloperTools
{
    [RequireComponent(typeof(Button))]
    public class Item : MonoBehaviour
    {
        public event UnityAction<Item> Clicked;

        [SerializeField] private Image _icon;
        
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

        private void OnEnable()
        {
            Button.onClick.AddListener(OnClick);
        }

        public void Initialize(Sprite icon)
        {
            _icon.sprite = icon;
        }

        private void OnDisable()
        {
            Button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            Clicked?.Invoke(this);
        }
    }
}