using UnityEngine;

namespace SP.Runtime.Core.Systems.Input
{
    public class ReloadInputAction : InputAction
    {
        public ReloadInputAction(string name, Sprite icon, ClickType clickType) : base(name, icon, clickType)
        {
            
        }

        public int TotalAmmo { get; set; }
    }
}