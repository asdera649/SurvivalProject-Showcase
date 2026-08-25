using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Mask = SP.Runtime.Core.UI.Utilities.Mask;

namespace SP.Runtime.Core.UI.MiniMap
{
    [RequireComponent(typeof(Image), typeof(Mask))]
    public class Placemark : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event UnityAction<Placemark> ClickAction;
    
        public string Name { get; private set; }
        public string Description { get; private set; }
        public bool DontDelete { get; private set; }
    
        private bool _isDrag;
        
        private Image _image;
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
        
        private Mask _mask;
        public Mask Mask
        {
            get
            {
                if (_mask == null)
                {
                    _mask = GetComponent<Mask>();
                }
                
                return _mask;
            }
        }

        public void Initialize(string placemarkName, Sprite icon, Color? color, string placemarkDescription, bool dontDelete)
        {
            Name = placemarkName;
            Description = placemarkDescription;
            
            Image.sprite = icon;
            
            if (color != null)
            {
                Image.color = color.Value;
            }
            
            DontDelete = dontDelete;
        }

        public void OnPointerDown(PointerEventData eventData)
        {

        }

        public void OnDrag(PointerEventData eventData)
        {
            _isDrag = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDrag)
            {
                ClickAction?.Invoke(this);
            }

            _isDrag = false;
        }
    }
}
