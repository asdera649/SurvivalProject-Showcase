using FMODUnity;
using SP.Runtime.Core.Items.Settings;
using SP.Runtime.Core.Items.Settings.Impact;
using SP.Runtime.Core.Services;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class Impact : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private ParticleSystem _particle;
        [SerializeField] private StudioEventEmitter _impactEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private float _delay = 1;

        private readonly string _surfaceParameter = "Surface";
        
        private void OnEnable()
        {
            Invoke(nameof(Destroy), _delay);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Destroy));
        }
        
        public void Play(SoundImpactSettings.Surface surface)
        {
            _particle.Play();
            _impactEventEmitter.Play();
            _impactEventEmitter.SetParameter(_surfaceParameter, EnumUtils.GetValueIndex(surface));
        }
        
        private void Destroy()
        {
            Loader.Instance.PoolService.ReleaseObject(this);
        }
    }
}