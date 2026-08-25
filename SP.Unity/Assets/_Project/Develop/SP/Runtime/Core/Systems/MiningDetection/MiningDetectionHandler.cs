using UnityEngine;

namespace SP.Runtime.Core.Systems.MiningDetection
{
    public class MiningDetectionHandler : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private ParticleSystem _valueEffect;

        private bool _isDisabled;
        
        private float _valueEffectRateBackup;
        
        private void Awake()
        {
            _valueEffectRateBackup = _valueEffect.emission.rateOverTime.constant;
        }

        private void OnEnable()
        {
            _isDisabled = false;

            DisableValueEffect();
        }
        
        private void OnDisable()
        {
            _isDisabled = true;
            
            DisableValueEffect();
        }

        public void EnableValueEffect(float percent)
        {
            if (_isDisabled)
            {
                return;
            }
            
            var e = _valueEffect.emission;

            e.rateOverTime = _valueEffectRateBackup * percent;

            if (!_valueEffect.isPlaying)
            {
                _valueEffect.Play();
            }
        }
        
        public void DisableValueEffect()
        {
            _valueEffect.Stop();
        }
    }
}