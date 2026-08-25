using System;
using System.Collections.Generic;
using FMODUnity;
using SP.Runtime.Localization;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.UIControls.RadialMenu
{
    [RequireComponent(typeof(Animation), typeof(CanvasGroup))]
    public class RadialMenu : MonoBehaviour
    {
        [Header("Prefabs")] 
        [SerializeField] private RadialMenuButton _radialMenuButtonPrefab;

        [Header("References")] 
        [SerializeField] private Image _cursor;
        [SerializeField] private LocalizeStringHelper _nameLocalize;
        [SerializeField] private LocalizeStringHelper _descriptionLocalize;
        [SerializeField] private Transform _buttonsContainer;

        [SerializeField] private StudioEventEmitter _openAndCursorEventEmitter;
        [SerializeField] private StudioEventEmitter _closeEventEmitter;

        [Header("Settings")] 
        [SerializeField] private Color _buttonColor;
        [SerializeField] private Color _selectedButtonColor;
        
        [Space(10)]
        
        [SerializeField] private bool _selectOnlyOnHover = true;
        [SerializeField] private float _cursorTransitionSpeed = 9;
        [SerializeField] 
        [Range(0, 1)] private float _buttonMinFill = 0.25f;
        [SerializeField] private float _pieThickness = 85;
        [SerializeField] private bool _snap = true;

        public Vector2 InputPosition { get; set; }

        private bool _isOpen;
        public bool IsOpen => _isOpen;

        public bool IsEmpty => _buttons.Count == 0;

        private RadialMenuButton _selectedButton;
        private RadialMenuButton SelectedButton
        {
            get => _selectedButton;
            set
            {
                var oldValue = _selectedButton;
                _selectedButton = value;

                if (oldValue == _selectedButton)
                {
                    return;
                }
            
                OnSelectedButtonUpdate(oldValue, _selectedButton);
            }
        }
        
        private readonly List<RadialMenuButton> _buttons = new();

        private const float _radius = 117f;
        private const float _radian = 57.29578f;
        
        private float _desiredFill;
        
        private RadialMenuButton _lastSelectedButton;

        private Animation _animation;
        private Animation Animation
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

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            OnSelectedButtonUpdate(null, null);
            
            transform.localScale = Vector3.zero;
            
            CanvasGroup.alpha = 0;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }
        
        private void Update()
        {
            UpdateCursor();
            
            UpdateColor();
        }

        private void UpdateCursor()
        {
            if (_isOpen)
            {
                _cursor.fillAmount = _desiredFill;
                
                var angle = _radian * Mathf.Atan2(InputPosition.x, InputPosition.y);

                if (angle < 0)
                {
                    angle += 360;
                }

                var cursorRotation = -(angle - _cursor.fillAmount * 360 / 2);
                
                var distanceFromCenter = Vector2.Distance(Vector2.zero, InputPosition);

                if (_selectOnlyOnHover && distanceFromCenter > _pieThickness || !_selectOnlyOnHover)
                {
                    float lastRotation = 0;
                    
                    RadialMenuButton nearest = null;

                    foreach (var b in _buttons)
                    {
                        var rotation = Convert.ToSingle(b.name);
                        var extremeAngle = rotation + (rotation - lastRotation);

                        if (Mathf.Abs(angle) > lastRotation && Mathf.Abs(angle) < extremeAngle)
                        {
                            nearest = b;
                        }

                        lastRotation = extremeAngle;
                    }

                    SelectedButton = nearest;

                    if (SelectedButton != null)
                    {
                        if (_snap)
                        {
                            cursorRotation = -(Convert.ToSingle(SelectedButton.name) - _cursor.fillAmount * 360 / 2);
                        }

                        if (_lastSelectedButton == null)
                        {
                            _cursor.transform.localRotation = Quaternion.Euler(0, 0, cursorRotation);
                        }

                        _cursor.transform.localRotation = Quaternion.Slerp(
                            _cursor.transform.localRotation,
                            Quaternion.Euler(0, 0, cursorRotation),
                            _cursorTransitionSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    SelectedButton = null;
                }
            }
            else
            {
                SelectedButton = null;
            }
            
            _lastSelectedButton = SelectedButton;
        }

        private void UpdateColor()
        {
            _cursor.color = _isOpen && SelectedButton != null ? 
                SelectedButton.Interactable ? ColorUtils.ChangeColorAlpha(_cursor.color, 1) : ColorUtils.ChangeColorAlpha(_cursor.color, 0.25f) : 
                ColorUtils.ChangeColorAlpha(_cursor.color, 0);

            foreach (var b in _buttons)
            {
                b.ImageComponent.color = Color.Lerp(
                    b.ImageComponent.color,
                    b != SelectedButton ? 
                        ColorUtils.ChangeColorAlpha(_buttonColor, b.Interactable ? _buttonColor.a : 0.25f) : 
                        ColorUtils.ChangeColorAlpha(_selectedButtonColor, b.Interactable ? _selectedButtonColor.a : 0.25f),
                    _cursorTransitionSpeed * Time.deltaTime);
            }
        }
        
        public void Open()
        {
            if (_isOpen)
            {
                return;
            }
            
            if (_buttons.Count == 0)
            {
                return;
            }
            
            _openAndCursorEventEmitter.Play();

            Animation.Play();

            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;

            _isOpen = true;
        }

        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }
            
            _closeEventEmitter.Play();
            
            Animation.Stop();

            transform.localScale = Vector3.zero;
            
            CanvasGroup.alpha = 0;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            
            _isOpen = false;
        }
        
        public void Execute()
        {
            if (_isOpen && SelectedButton != null && SelectedButton.Interactable)
            {
                SelectedButton.Execute();
            }
        }
        
        public RadialMenuButton InstantiateButton(string nameEntry, string descriptionEntry, Sprite icon)
        {
            var button = Instantiate(_radialMenuButtonPrefab, Vector2.zero, transform.rotation, _buttonsContainer);

            button.NameEntry = nameEntry;
            button.DescriptionEntry = descriptionEntry;
            button.ImageComponent.sprite = icon;
            button.ImageComponent.color = _buttonColor;
            button.DestroyAction += OnButtonDestroy;
            button.DataUpdateAction += OnDataUpdate;

            _buttons.Add(button);

            UpdateButtonsPosition();

            return button;
        }

        public void DestroyButton(RadialMenuButton button)
        {
            if (_buttons.Contains(button))
            {
                button.DestroyAction -= OnButtonDestroy;
                button.DataUpdateAction -= OnDataUpdate;
                _buttons.Remove(button);
                Destroy(button.gameObject);
            }

            UpdateButtonsPosition();

            if (_buttons.Count == 0)
            {
                Close();
            }
        }
        
        private void UpdateButtonsPosition()
        {
            _desiredFill = 1f / _buttons.Count < _buttonMinFill ? 1f / _buttons.Count : _buttonMinFill;

            var fillRadius = _desiredFill * 360;
            var previousRotation = 0f;

            foreach (var b in _buttons)
            {
                var buttonRotation = previousRotation + fillRadius / 2;
                previousRotation = buttonRotation + fillRadius / 2;

                b.transform.localPosition = new Vector2(
                    _radius * Mathf.Cos((buttonRotation - 90) * Mathf.Deg2Rad),
                    -_radius * Mathf.Sin((buttonRotation - 90) * Mathf.Deg2Rad));

                if (buttonRotation > 360)
                {
                    buttonRotation -= 360;
                }

                b.name = buttonRotation.ToString();
            }
        }
        
        private void UpdateLocalizeText()
        {
            if (SelectedButton != null && SelectedButton.Interactable)
            {
                _nameLocalize.SetEntry(SelectedButton.NameEntry);
                _descriptionLocalize.SetEntry(SelectedButton.DescriptionEntry);
            }
            else
            {
                _nameLocalize.ClearEntry();
                _descriptionLocalize.ClearEntry();
            }
        }

        #region Callbacks
        
        private void OnSelectedButtonUpdate(RadialMenuButton oldValue, RadialMenuButton newValue)
        {
            UpdateLocalizeText();

            if (newValue != null)
            {
                _openAndCursorEventEmitter.Play();
            }
        }
        
        private void OnDataUpdate(RadialMenuButton button)
        {
            UpdateLocalizeText();
        }

        private void OnButtonDestroy(RadialMenuButton button)
        {
            if (_buttons.Contains(button))
            {
                _buttons.Remove(button);
            }

            UpdateButtonsPosition();
        }
        
        #endregion
    }
}