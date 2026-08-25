using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Input
{
    public class InputAction
    {
        #region Structs

        public enum ClickType
        {
            Down,
            Up
        }

        #endregion
        
        public InputAction(string name, Sprite icon, ClickType clickType)
        {
            Name = name;
            Icon = icon;
            TypeOfClick = clickType;
        }

        public event UnityAction<InputAction, AdditionalInputAction> AdditionalInputActionAdded;
        public event UnityAction<InputAction, AdditionalInputAction> AdditionalInputActionRemoved;
        public event UnityAction<InputAction> InputActionDataUpdated;
        public event UnityAction<InputAction> Executed;
        
        public string Name { get; private set; }

        private Sprite _icon;
        public Sprite Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                InputActionDataUpdated?.Invoke(this);
            }
        }

        public ClickType TypeOfClick { get; private set; }

        private readonly List<AdditionalInputAction> _additionalInputActions = new ();

        public AdditionalInputAction AddAdditionalInputAction(string name, string description, Sprite icon)
        {
            var output = new AdditionalInputAction(name, description, icon);
            
            _additionalInputActions.Add(output);
            
            AdditionalInputActionAdded?.Invoke(this, output);
            
            return output;
        }

        public void RemoveAdditionalInputAction(AdditionalInputAction additionalInputAction)
        {
            if (!_additionalInputActions.Contains(additionalInputAction))
            {
                return;
            }

            additionalInputAction.Cleanup();

            _additionalInputActions.Remove(additionalInputAction);
            
            AdditionalInputActionRemoved?.Invoke(this, additionalInputAction);
        }

        public void Cleanup()
        {
            foreach (var a in _additionalInputActions.ToArray())
            {
                RemoveAdditionalInputAction(a);
            }
            
            AdditionalInputActionAdded = null;
            AdditionalInputActionRemoved = null;
            InputActionDataUpdated = null;
            Executed = null;
        }
        
        public void Execute()
        {
            Executed?.Invoke(this);
        }
    }
}