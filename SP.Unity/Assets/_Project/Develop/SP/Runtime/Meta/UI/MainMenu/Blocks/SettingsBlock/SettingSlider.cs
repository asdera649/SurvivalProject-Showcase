using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.SettingsBlock
{
    [RequireComponent(typeof(Slider))]
    public class SettingSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TMP_Text _text;
        
        [Space(10)]
        
        [SerializeField] private UnityEvent _onSliding;

        private bool _isPointerDown;
        
        private Slider _slider;
        private Slider Slider
        {
            get
            {
                if (_slider == null)
                {
                    _slider = GetComponent<Slider>();
                }

                return _slider;
            }
        }

        private void OnEnable()
        {
            Slider.onValueChanged.AddListener(OnValueUpdate);
            
            OnValueUpdate(Slider.value);
        }

        private void OnDisable()
        {
            Slider.onValueChanged.RemoveListener(OnValueUpdate);   
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPointerDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPointerDown = false;
        }
        
        private void OnValueUpdate(float value)
        {
            _text.text = Slider.wholeNumbers ? value.ToString(CultureInfo.CurrentCulture) : $"{value:0.00}";

            if (_isPointerDown)
            {
                _onSliding?.Invoke();
            }
        }
    }
}
