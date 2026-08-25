using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Canvas = UnityEngine.Canvas;

namespace SP.Runtime.Core.UI.UIControls
{
    [RequireComponent(typeof(Image), typeof(CanvasGroup))]
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        #region Structs

        private enum AxisOptionsEnum
        {
            Both,
            Horizontal,
            Vertical
        }
        
        #endregion

        public event UnityAction<Vector2> PointerDownAction;
        public event UnityAction<Vector2> DragAction;
        public event UnityAction<Vector2> PointerUpAction;

        public bool Interactable;
        
        [Header("References")]
        [SerializeField] private RectTransform _background;
        protected RectTransform Background => _background;

        [SerializeField] private RectTransform _handle;
        protected RectTransform Handle => _handle;
        
        [Header("Settings")]
        [SerializeField] private float _handleRange = 1;
        [SerializeField] private float _deadZone;
        [SerializeField] private AxisOptionsEnum _axisOptions = AxisOptionsEnum.Both;
        [SerializeField] private bool _snapX;
        [SerializeField] private bool _snapY;
        
        protected float Horizontal => _snapX ? SnapFloat(_input.x, AxisOptionsEnum.Horizontal) : _input.x;
        protected float Vertical => _snapY ? SnapFloat(_input.y, AxisOptionsEnum.Vertical) : _input.y;
        
        protected virtual Vector2 Direction => new(Horizontal, Vertical);

        private float HandleRange
        {
            set => _handleRange = Mathf.Abs(value);
        }

        private float DeadZone
        {
            set => _deadZone = Mathf.Abs(value);
        }

        private Vector2 _input;
        
        private Canvas _canvas;
        
        private CanvasGroup _canvasGroup;
        private CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                }
                
                return _canvasGroup;
            }
        }
        
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

        protected virtual void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            HandleRange = _handleRange;
            DeadZone = _deadZone;
            
            _canvas = GetComponentInParent<Canvas>();

            if (_canvas == null)
            {
                Debug.LogError("The Joystick is not placed inside a canvas");
            }

            var center = new Vector2(0.5f, 0.5f);
            
            _background.pivot = center;
            _handle.anchorMin = center;
            _handle.anchorMax = center;
            _handle.pivot = center;
            _handle.anchoredPosition = Vector2.zero;
        }

        protected virtual void Update()
        {
            CanvasGroup.blocksRaycasts = Interactable;
            CanvasGroup.alpha = Interactable ? 1 : 0.2f;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            PointerDownAction?.Invoke(Direction);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var position = RectTransformUtility.WorldToScreenPoint(null, _background.position);
            var radius = _background.sizeDelta / 2;
            
            _input = (eventData.position - position) / (radius * _canvas.scaleFactor);
            
            FormatInput();
            HandleInput(_input.magnitude, _input.normalized, radius, null);
            
            _handle.anchoredPosition = _input * radius * _handleRange;

            DragAction?.Invoke(Direction);
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            _input = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;

            PointerUpAction?.Invoke(Direction);
        }

        protected virtual void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, UnityEngine.Camera cam)
        {
            if (magnitude > _deadZone)
            {
                if (magnitude > 1)
                {
                    _input = normalised;
                }
            }
            else
            {
                _input = Vector2.zero;
            }
        }

        private void FormatInput()
        {
            if (_axisOptions == AxisOptionsEnum.Horizontal)
            {
                _input = new Vector2(_input.x, 0);
            }
            else if (_axisOptions == AxisOptionsEnum.Vertical)
            {
                _input = new Vector2(0, _input.y);
            }
        }

        private float SnapFloat(float value, AxisOptionsEnum snapAxis)
        {
            if (value == 0)
            {
                return value;
            }

            if (_axisOptions == AxisOptionsEnum.Both)
            {
                var angle = Vector2.Angle(_input, Vector2.up);
                
                if (snapAxis == AxisOptionsEnum.Horizontal)
                {
                    if (angle < 22.5f || angle > 157.5f)
                    {
                        return 0;
                    }
                    else
                    {
                        return value > 0 ? 1 : -1;
                    }
                }
                else if (snapAxis == AxisOptionsEnum.Vertical)
                {
                    if (angle is > 67.5f and < 112.5f)
                    {
                        return 0;
                    }
                    else
                    {
                        return value > 0 ? 1 : -1;
                    }
                }
                
                return value;
            }
            else
            {
                if (value > 0)
                {
                    return 1;
                }

                if (value < 0)
                {
                    return -1;
                }
            }
            
            return 0;
        }

        protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
        {
            var rectTransform = Image.rectTransform;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPosition,
                    null,
                    out var localPoint))
            {
                var pivotOffset = rectTransform.pivot * rectTransform.sizeDelta;
                return localPoint - _background.anchorMax * rectTransform.sizeDelta + pivotOffset;
            }
            
            return Vector2.zero;
        }
    }
}