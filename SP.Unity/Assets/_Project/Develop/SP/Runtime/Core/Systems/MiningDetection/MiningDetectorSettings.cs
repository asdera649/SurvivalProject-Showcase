using UnityEngine;

namespace SP.Runtime.Core.Systems.MiningDetection
{
    [CreateAssetMenu(menuName = "SettingsService/MiningDetectorSettings")]
    public class MiningDetectorSettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }
    }
}