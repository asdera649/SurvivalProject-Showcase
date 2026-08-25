using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.UIControls
{
    public class ClassicButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // public event UnityAction<ClassicButton> PointerDowned;
        // public event UnityAction<ClassicButton> PointerUpped;
        
        public UnityAction<ClassicButton> PointerDowned;
        public UnityAction<ClassicButton> PointerUpped;

        [Header("References")] 
        [SerializeField] private GameObject _background;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _outline;
        
        [SerializeField] private RadialMenu.RadialMenu _radialMenu;
        public RadialMenu.RadialMenu RadialMenu => _radialMenu;
        
        [Header("Settings")]
        [SerializeField] private float _dragThreshold = 30;
        
        public Sprite Icon
        {
            set => _icon.sprite = value;
        }
        
        private bool _isPointerDown;
        private bool _isDrag;
        
        private Vector2 _dragStartPosition;
        
        public void Initialize(Sprite icon)
        {
            Icon = icon;
        }

        protected virtual void Update()
        {
            _background.SetActive(!_radialMenu.IsOpen);
            _outline.SetActive(!_radialMenu.IsEmpty);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isPointerDown)
            {
                return;
            }
            
            _dragStartPosition = eventData.position;
            
            PointerDowned?.Invoke(this);
            
            transform.localScale *= 0.9f;

            _isPointerDown = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isPointerDown)
            {
                return;
            }
            
            var distance = Vector2.Distance(_dragStartPosition, eventData.position);

            if (distance > _dragThreshold)
            {
                if (_isDrag == false)
                {
                    RadialMenu.Open();
                    transform.SetAsLastSibling();
                }

                RadialMenu.InputPosition = eventData.position - (Vector2)transform.position;
            
                _isDrag = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPointerDown)
            {
                return;
            }

            if (!_isDrag)
            {
                PointerUpped?.Invoke(this);
            }
            
            transform.localScale /= 0.9f;
            
            if (_isDrag)
            {
                RadialMenu.Execute();
                RadialMenu.Close();
            }

            _isPointerDown = false;
            _isDrag = false;
        }
    }
}
