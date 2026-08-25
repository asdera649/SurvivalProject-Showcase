using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Input
{
    public class AdditionalInputAction
    {
        public AdditionalInputAction(string name, string description, Sprite icon)
        {
            Name = name;
            Description = description;
            Icon = icon;
        }
            
        public event UnityAction<AdditionalInputAction> ExecuteAction;
            
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Sprite Icon { get; private set; }

        public void Cleanup()
        {
            ExecuteAction = null;
        }
            
        public void Execute()
        {
            ExecuteAction?.Invoke(this);
        }
    }
}