using System.Collections.Generic;
using SP.Runtime.Core.UI.Hud;
using SP.Runtime.Core.UI.UIControls;
using SP.Runtime.Core.UI.UIControls.RadialMenu;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Input
{
    public class MobileInput : BaseInput
    {
        public MobileInput(MobileHud hud)
        {
            _hud = hud;
            _hud.MovementJoystick.DragAction += OnMovementJoystickDrag;
            _hud.MovementJoystick.PointerUpAction += OnMovementJoystickPointerUp;
            _hud.AttackButton.PointerDowned += OnAttackButtonPointerDown;
            _hud.AttackButton.PointerUpped += OnAttackButtonPointerUp;
        }

        private readonly Dictionary<InputAction, ClassicButton> _instancedInputActions = new();
        private readonly Dictionary<AdditionalInputAction, RadialMenuButton> _instancedAdditionalInputActions = new();
        
        private bool _isAttackButtonHold;

        private readonly MobileHud _hud;

        public override void Update()
        {
            base.Update();

            if (CurrentAttackType == AttackType.Holding && _isAttackButtonHold)
            {
                InvokeAttackAction();
            }
        }
        
        public override void Cleanup()
        {
            _hud.MovementJoystick.DragAction -= OnMovementJoystickDrag;
            _hud.MovementJoystick.PointerUpAction -= OnMovementJoystickPointerUp;
            _hud.AttackButton.PointerDowned -= OnAttackButtonPointerDown;
            _hud.AttackButton.PointerUpped -= OnAttackButtonPointerUp;
            
            base.Cleanup();
        }

        protected override void OnInputActionAdded(InputAction inputAction)
        {
            base.OnInputActionAdded(inputAction);

            ClassicButton button = null;
            
            if (inputAction.GetType() == typeof(InputAction))
            {
                button = _hud.AddAdditionalButton<ClassicButton>(inputAction.Icon);
            }
            else if (inputAction.GetType() == typeof(ReloadInputAction))
            {
                var reloadInputAction = (ReloadInputAction)inputAction;
                
                var reloadButton = _hud.AddAdditionalButton<ReloadButton>(inputAction.Icon);
                reloadButton.Initialize(reloadInputAction, reloadInputAction.Icon);

                button = reloadButton;
            }
            else if (inputAction.GetType() == typeof(BuildingChangeInputAction))
            {
                button = _hud.AddAdditionalButton<BuildingChangeButton>(inputAction.Icon);
            }

            if (button == null)
            {
                Debug.LogError("Unknown input action type!");
                return;
            }

            if (inputAction.TypeOfClick == InputAction.ClickType.Down)
            {
                button.PointerDowned += OnAdditionalButtonExecute;
            }
            else if (inputAction.TypeOfClick == InputAction.ClickType.Up)
            {
                button.PointerUpped += OnAdditionalButtonExecute;
            }
            
            _instancedInputActions.Add(inputAction, button);
        }
        
        protected override void OnInputActionRemoved(InputAction inputAction)
        {
            if (_instancedInputActions.TryGetValue(inputAction, out var classicButton))
            {
                _hud.RemoveAdditionalButton(classicButton);
                _instancedInputActions.Remove(inputAction);
            }
            
            base.OnInputActionRemoved(inputAction);
        }
        
        protected override void OnAdditionalInputActionAdded(InputAction inputAction, AdditionalInputAction additionalInputAction)
        {
            base.OnAdditionalInputActionAdded(inputAction, additionalInputAction);

            if (_instancedInputActions.TryGetValue(inputAction, out var classicButton))
            {
                var radialMenuButton = classicButton.RadialMenu.InstantiateButton(
                    additionalInputAction.Name,
                    additionalInputAction.Description,
                    additionalInputAction.Icon);

                radialMenuButton.ExecuteAction += OnRadialMenuButtonExecute;
                
                _instancedAdditionalInputActions.Add(additionalInputAction, radialMenuButton);
            }
        }
        
        protected override void OnAdditionalInputActionRemoved(InputAction inputAction, AdditionalInputAction additionalInputAction)
        {
            if (_instancedInputActions.TryGetValue(inputAction, out var classicButton))
            {
                if (_instancedAdditionalInputActions.TryGetValue(additionalInputAction, out var radialMenuButton))
                {
                    radialMenuButton.ExecuteAction -= OnRadialMenuButtonExecute;
                    classicButton.RadialMenu.DestroyButton(radialMenuButton);
                    _instancedAdditionalInputActions.Remove(additionalInputAction);
                }
            }

            base.OnAdditionalInputActionRemoved(inputAction, additionalInputAction);
        }

        protected override void OnInputActionDataUpdate(InputAction inputAction)
        {
            if (_instancedInputActions.TryGetValue(inputAction, out var classicButton))
            {
                classicButton.Icon = inputAction.Icon;
            }
        }

        private void OnMovementJoystickDrag(Vector2 direction)
        {
            InvokeMoveAction(direction);
        }
        
        private void OnMovementJoystickPointerUp(Vector2 direction)
        {
            InvokeMoveAction(Vector2.zero);
        }
        
        private void OnAttackButtonPointerDown(ClassicButton classicButton)
        {
            if (CurrentAttackType == AttackType.SingleClick)
            {
                InvokeAttackAction();
            }

            _isAttackButtonHold = true;
        }
        
        private void OnAttackButtonPointerUp(ClassicButton classicButton)
        {
            _isAttackButtonHold = false;
        }

        private void OnAdditionalButtonExecute(ClassicButton classicButton)
        {
            foreach (var a in _instancedInputActions)
            {
                if (a.Value == classicButton)
                {
                    a.Key.Execute();
                    
                    return;
                }
            }
        }

        private void OnRadialMenuButtonExecute(RadialMenuButton radialMenuButton)
        {
            foreach (var a in _instancedAdditionalInputActions)
            {
                if (a.Value == radialMenuButton)
                {
                    a.Key.Execute();
                    
                    return;
                }
            }
        }
    }
}