using UnityEngine;

namespace SP.Runtime.Core.UI.Hud
{
    [CreateAssetMenu(menuName = "SettingsService/HudSettings")]
    public class HudSettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private bool _helpWithAiming;
        public bool HelpWithAiming
        {
            get => _helpWithAiming;
            set => _helpWithAiming = value;
        }
    }
}