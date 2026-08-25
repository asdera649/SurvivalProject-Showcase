using UnityEngine;
using UnityEngine.EventSystems;

namespace SP.Runtime.Core.UI.UIControls
{
    public class ClassicJoystick : Joystick
    {
        #region Structs
        
        public enum JoystickType
        {
            Fixed,
            Floating,
            Dynamic
        }
        
        #endregion

        [Header("Settings")]
        [SerializeField] private float _moveThreshold = 1;
        [SerializeField] private float _maxMoveDistance = 300;
        [SerializeField] private JoystickType _joystickType = JoystickType.Fixed;
        [Space(10)]
        [SerializeField] private bool _animateClicking = true;
        [SerializeField] private float _animationSmoothness = 30;

        protected override Vector2 Direction => _joystickType == JoystickType.Dynamic ? 
            _direction : new Vector2(Horizontal, Vertical);
        
        private Vector2 _fixedPosition = Vector2.zero;
        private Vector3 _currentLocalScale = Vector3.one;

        private Vector2 _direction = Vector2.zero;

        private bool _isPointerDown;
        private int _pointerId;

        protected override void Start()
        {
            base.Start();

            Initialize();
        }

        private void Initialize()
        {
            _fixedPosition = Background.anchoredPosition;

            SetMode(_joystickType);
        }

        protected override void Update()
        {
            base.Update();
            
            UpdateScale();
        }

        private void UpdateScale()
        {
            Handle.transform.localScale = Vector3.Lerp(
                Handle.transform.localScale,
                _currentLocalScale,
                _animationSmoothness * Time.deltaTime);
        }
        
        private void SetMode(JoystickType joystickType)
        {
            _joystickType = joystickType;

            if (joystickType == JoystickType.Fixed)
            {
                Background.anchoredPosition = _fixedPosition;
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (_isPointerDown)
            {
                return;
            }

            if (_joystickType != JoystickType.Fixed)
            {
                Background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            }

            if (_animateClicking)
            {
                _currentLocalScale *= 0.9f;
            }

            _pointerId = eventData.pointerId;
            _isPointerDown = true;

            base.OnPointerDown(eventData);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (_pointerId != eventData.pointerId)
            {
                return;
            }

            _direction = Vector2.zero;

            if (_joystickType != JoystickType.Fixed)
            {
                Background.anchoredPosition = Vector2.zero;
            }

            if (_animateClicking)
            {
                _currentLocalScale /= 0.9f;
            }

            _isPointerDown = false;
            _pointerId = 0;

            base.OnPointerUp(eventData);
        }

        protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, UnityEngine.Camera cam)
        {
            if (_joystickType == JoystickType.Dynamic && magnitude > _moveThreshold)
            {
                var difference = normalised * (magnitude - _moveThreshold) * radius;
                var newPos = Background.anchoredPosition += difference;
                
                Background.anchoredPosition = Vector2.ClampMagnitude(newPos, _maxMoveDistance);
            }

            _direction = (Background.anchoredPosition + Handle.anchoredPosition) / 
                         (_maxMoveDistance + (radius.x + radius.y) / 2);

            base.HandleInput(magnitude, normalised, radius, cam);
        }
    }
}