using UnityEngine;

namespace SP.Runtime.Utilities.FpsDisplay
{
    [CreateAssetMenu(menuName = "SettingsService/FpsDisplaySettings")]
    public class FpsDisplaySettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private bool _showFps;
        public bool ShowFps
        {
            get => _showFps;
            set => _showFps = value;
        }
    }
}