using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.MiniMap
{
    [RequireComponent(typeof(RectTransform), typeof(Animation))]
    public class AddingPlacemarkBlock : MonoBehaviour
    {
        public event UnityAction<string, Sprite, Color> PlaceAction;

        [Header("References")]
        [SerializeField] private Button _iconChangeButton;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_InputField _nameInputField;
        [SerializeField] private Button _placeButton;

        [Header("Settings")]
        [SerializeField] private Color[] _iconColorOptions;

        private int _colorIndex = 1;
        
        private RectTransform _rectTransform;
        public RectTransform RectTransform 
        { 
            get 
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                
                return _rectTransform;
            } 
        }
        
        private Animation _animation;
        public Animation Animation 
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

        private void OnEnable()
        {
            _placeButton.onClick.AddListener(OnPlaceButtonClick);
            _iconChangeButton.onClick.AddListener(OnIconChangeButtonClick);
        }

        private void OnDisable()
        {
            _nameInputField.text = null;
            
            _placeButton.onClick.RemoveListener(OnPlaceButtonClick);
            _iconChangeButton.onClick.RemoveListener(OnIconChangeButtonClick);
        }

        private Color GetNextColor()
        {
            var output = Color.white;

            if (_colorIndex <= _iconColorOptions.Length - 1)
            {
                output = _iconColorOptions[_colorIndex];
            }

            _colorIndex++;

            if (_colorIndex > _iconColorOptions.Length - 1)
            {
                _colorIndex = 0;
            }

            return output;
        }
        
        private void OnPlaceButtonClick()
        {
            PlaceAction?.Invoke(_nameInputField.text, _iconImage.sprite, _iconImage.color);
        }

        private void OnIconChangeButtonClick()
        {
            _iconImage.color = GetNextColor();
        }
    }
}
