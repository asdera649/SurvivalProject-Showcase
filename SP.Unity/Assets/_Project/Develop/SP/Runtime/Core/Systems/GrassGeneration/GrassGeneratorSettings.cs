using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.GrassGeneration
{
    [CreateAssetMenu(menuName = "SettingsService/GrassGeneratorSettings")]
    public class GrassGeneratorSettings : ScriptableObject
    {
        public event UnityAction SettingsUpdatedAction; 
        
        [Header("Settings")]
        [SerializeField] private float _spawnRadius = 30;
        public float SpawnRadius
        {
            get => _spawnRadius;
            set
            {
                _spawnRadius = Mathf.Clamp(value, _minSpawnRadius, _maxSpawnRadius);
                
                SettingsUpdatedAction?.Invoke();
            }
        }

        private const float _minSpawnRadius = 0;
        private const float _maxSpawnRadius = 100;
    }
}