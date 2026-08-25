using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Drowning
{
    [RequireComponent(typeof(Image))]
    public class DrowningTip : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Color _neutralColor;
        [SerializeField] private Color _warningColor;

        [Space(10)]
        
        [SerializeField] private float _colorUpdateSmoothness = 15; 
        
        private float _drowningPercentage;
        public float DrowningPercentage
        {
            get => _drowningPercentage;
            set
            {
                _drowningPercentage = Mathf.Clamp01(value);
                
                UpdateColor();
            }
        }

        private Color _currentColor;
        
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
        
        private void Update()
        {
            Image.color = Color.Lerp(Image.color, _currentColor, Time.deltaTime * _colorUpdateSmoothness);
        }

        private void UpdateColor()
        {
            _currentColor = Color.Lerp(_neutralColor, _warningColor, DrowningPercentage);
        }
    }
}