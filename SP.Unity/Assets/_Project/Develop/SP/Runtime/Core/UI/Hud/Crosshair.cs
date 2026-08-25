using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Hud
{
    public class Crosshair : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private Image _point;

        [Header("Settings")]
        [SerializeField] private Color _crosshairRedColor = Color.red;
        
        private Image _image;
        
        private Color _crosshairColorBackup;
        private Color _crosshairPointColorBackup;

        public Image Image
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

        private void Awake()
        {
            _crosshairColorBackup = Image.color;
            _crosshairPointColorBackup = _point.color;
        }

        public void SetColor(Color color)
        {
            Image.color = color;
            _point.color = color;
            
            _crosshairColorBackup = Image.color;
            _crosshairPointColorBackup = _point.color;
        }

        public void SetActiveRedCrosshair(bool value)
        {
            if (value)
            {
                Image.color = new Color(
                    _crosshairRedColor.r,
                    _crosshairRedColor.g,
                    _crosshairRedColor.b,
                    Image.color.a);
                
                _point.color = new Color(
                    _crosshairRedColor.r,
                    _crosshairRedColor.g,
                    _crosshairRedColor.b,
                    _point.color.a);
            }
            else
            {
                Image.color = _crosshairColorBackup;
                _point.color = _crosshairPointColorBackup;
            }
        }
    }
}
