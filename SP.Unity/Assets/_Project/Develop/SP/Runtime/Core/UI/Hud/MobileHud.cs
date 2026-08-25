using System.Collections.Generic;
using SP.Runtime.Core.UI.UIControls;
using UnityEngine;

namespace SP.Runtime.Core.UI.Hud
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MobileHud : BaseHud
    {
        [Header("Prefabs")] 
        [SerializeField] private ClassicButton _classicButtonPrefab;
        [SerializeField] private ReloadButton _reloadButtonPrefab;
        [SerializeField] private BuildingChangeButton _buildingChangeButtonPrefab;

        [Header("References")] 
        [SerializeField] private Sprite _meleeAttackIcon;
        [SerializeField] private Sprite _gunAttackIcon;
        [SerializeField] private Sprite _provisionUseIcon;
        
        [Space(10)]
        
        [SerializeField] private ClassicJoystick _movementJoystick;
        public ClassicJoystick MovementJoystick => _movementJoystick;

        [SerializeField] private ClassicJoystick _aimJoystick;
        public ClassicJoystick AimJoystick => _aimJoystick;
        
        [SerializeField] private ClassicButton _attackButton;
        public ClassicButton AttackButton => _attackButton;

        [SerializeField] private GameObject _additionalButtonsBlock;

        [Header("Settings")] 
        [SerializeField] private Vector2[] _additionalButtonsPositions;
        
        private readonly List<ClassicButton> _additionalButtons = new();

        private bool _isPointerDowned;
        
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
        
        private void OnEnable()
        {
            _aimJoystick.DragAction += OnAimJoystickDrag;
            _aimJoystick.PointerUpAction += OnAimJoystickUp;
        }
        
        private void OnDisable()
        {
            _aimJoystick.DragAction -= OnAimJoystickDrag;
            _aimJoystick.PointerUpAction -= OnAimJoystickUp;
        }

        private void Update()
        {
            UpdateHelpWithAiming();
        }

        // Временное решение, нужно сделать по уму, через BaseHud
        private void UpdateHelpWithAiming()
        {
            if (IsAimingTarget)
            {
                AttackButton.PointerDowned?.Invoke(AttackButton);
                _isPointerDowned = true;
            }
            else if (_isPointerDowned)
            {
                AttackButton.PointerUpped?.Invoke(AttackButton);
                _isPointerDowned = false;
            }
        }

        public override void SetView(bool value)
        {
            base.SetView(value);

            CanvasGroup.alpha = value ? 1 : 0;
            CanvasGroup.interactable = value;
            CanvasGroup.blocksRaycasts = value;
        }
        
        public T AddAdditionalButton<T>(Sprite icon) where T: ClassicButton
        {
            ClassicButton output = null;

            if (typeof(T) == typeof(ClassicButton))
            {
                output = Instantiate(_classicButtonPrefab, _additionalButtonsBlock.transform);
            }
            else if (typeof(T) == typeof(ReloadButton))
            {
                output = Instantiate(_reloadButtonPrefab, _additionalButtonsBlock.transform);
            }
            else if (typeof(T) == typeof(BuildingChangeButton))
            {
                output = Instantiate(_buildingChangeButtonPrefab, _additionalButtonsBlock.transform);
            }

            if (output == null)
            {
                Debug.LogError("Unknown button type!");
                return null;
            }
            
            output.Initialize(icon);
            
            _additionalButtons.Add(output);
            
            UpdateAdditionalButtons();

            return (T)output;
        }
        
        public void RemoveAdditionalButton(ClassicButton classicButton)
        {
            if (!_additionalButtons.Contains(classicButton))
            {
                return;
            }

            _additionalButtons.Remove(classicButton);
            
            Destroy(classicButton.gameObject);
            
            UpdateAdditionalButtons();
        }
        
        private void UpdateAdditionalButtons()
        {
            for (var i = 0; i < _additionalButtons.Count; i++)
            {
                if (_additionalButtonsPositions.Length > i)
                {
                    var rectTransform = _additionalButtons[i].GetComponent<RectTransform>();
                    
                    rectTransform.anchoredPosition = _additionalButtonsPositions[i];
                    rectTransform.localScale = Vector3.one;
                }
            }
        }

        #region Callbacks
        
        protected override void OnModeUpdate(HudMode oldValue, HudMode newValue)
        {
            base.OnModeUpdate(oldValue, newValue);

            switch (Mode)
            {
                case HudMode.None:
                {
                    _attackButton.gameObject.SetActive(false);
                    break;
                }
                case HudMode.Build:
                {
                    _attackButton.gameObject.SetActive(false);
                    break;
                }
                case HudMode.Melee:
                {
                    _attackButton.Icon = _meleeAttackIcon;
                    _attackButton.gameObject.SetActive(true);
                    break;
                }
                case HudMode.Gun:
                {
                    _attackButton.Icon = _gunAttackIcon;
                    _attackButton.gameObject.SetActive(true);
                    break;
                }
                case HudMode.Provision:
                {
                    _attackButton.Icon = _provisionUseIcon;
                    _attackButton.gameObject.SetActive(true);
                    break;
                }
            }
        }
        
        private void OnAimJoystickDrag(Vector2 direction)
        {
            SetScreenPoint(direction);
        }
        
        private void OnAimJoystickUp(Vector2 direction)
        {
            SetScreenPoint(Vector2.zero);
        }
        
        #endregion
    }
}