using FMODUnity;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Buildings
{
    public class CampFireEntity : BaseBurningEntity
    {
        [Header("References")]
        [SerializeField] private ParticleSystem _particle;
        [SerializeField] private StudioEventEmitter _burningEventEmitter;

        #region Callbacks
        
        protected override void OnEnableUpdate(bool oldValue, bool newValue)
        {
            base.OnEnableUpdate(oldValue, newValue);

            if (!isClient)
            {
                return;
            }

            if (newValue)
            {
                _particle.Play();
                
                if (!_burningEventEmitter.IsPlaying())
                {
                    _burningEventEmitter.Play();
                }
            }
            else
            {
                _particle.Stop();

                if (_burningEventEmitter.IsPlaying())
                {
                    _burningEventEmitter.Stop();
                }
            }
        }
        
        #endregion
    }
}
