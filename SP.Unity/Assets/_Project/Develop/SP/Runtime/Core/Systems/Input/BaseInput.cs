using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Input
{
    public class BaseInput
    {
        #region Structs

        public enum AttackType
        {
            SingleClick,
            Holding
        }

        #endregion
        
        public event UnityAction<Vector2> MoveAction;
        public event UnityAction AttackAction;

        public AttackType CurrentAttackType { get; set; } = AttackType.SingleClick;

        private readonly List<InputAction> _inputActions = new();
        
        public virtual void Update()
        {
            
        }
        
        public virtual void Cleanup()
        {
            foreach (var a in _inputActions.ToArray())
            {
                RemoveInputAction(a);
            }
            
            _inputActions.Clear();
        }
        
        public void ResetSettings()
        {
            CurrentAttackType = AttackType.SingleClick;
        }

        public T AddInputAction<T>(string name, Sprite icon, InputAction.ClickType clickType) where T: InputAction
        {
            InputAction output = null;
            
            if (typeof(T) == typeof(InputAction))
            {
                output = new InputAction(name, icon, clickType);
            }
            else if (typeof(T) == typeof(ReloadInputAction))
            {
                output = new ReloadInputAction(name, icon, clickType);
            }
            else if (typeof(T) == typeof(BuildingChangeInputAction))
            {
                output = new BuildingChangeInputAction(name, icon, clickType);
            }

            if (output == null)
            {
                Debug.LogError("Unknown input action type!");
                return null;
            }

            output.AdditionalInputActionAdded += OnAdditionalInputActionAdded;
            output.AdditionalInputActionRemoved += OnAdditionalInputActionRemoved;
            output.InputActionDataUpdated += OnInputActionDataUpdate;

            _inputActions.Add(output);

            OnInputActionAdded(output);
            
            return (T)output;
        }

        public void RemoveInputAction(InputAction inputAction)
        {
            if (!_inputActions.Contains(inputAction))
            {
                return;
            }

            inputAction.Cleanup();
            
            OnInputActionRemoved(inputAction);
            
            _inputActions.Remove(inputAction);
        }

        protected void InvokeMoveAction(Vector2 direction)
        {
            MoveAction?.Invoke(direction);
        }
        
        protected void InvokeAttackAction()
        {
            AttackAction?.Invoke();
        }
        
        protected virtual void OnInputActionAdded(InputAction inputAction)
        {
            
        }

        protected virtual void OnInputActionRemoved(InputAction inputAction)
        {
            
        }

        protected virtual void OnAdditionalInputActionAdded(InputAction inputAction, AdditionalInputAction additionalInputAction)
        {
            
        }
        
        protected virtual void OnAdditionalInputActionRemoved(InputAction inputAction, AdditionalInputAction additionalInputAction)
        {
            
        }

        protected virtual void OnInputActionDataUpdate(InputAction inputAction)
        {
            
        }
    }
}