using FMODUnity;
using UnityEngine;

namespace SP.Runtime.Core.Objects.Models
{
    public class GunModel : BaseModel
    {
        [Header("References")]
        [SerializeField] private StudioEventEmitter _reloadEventEmitter;
        
        [SerializeField] private Transform _aimTransform;
        public Transform AimTransform => _aimTransform;

        [SerializeField] private Transform _secondHandTransform;
        public Transform SecondHandTransform => _secondHandTransform;

        [SerializeField] private Transform _muzzleTransform;
        public Transform MuzzleTransform => _muzzleTransform;

        public void Reload()
        {
            if (_reloadEventEmitter.IsPlaying())
            {
                return;
            }
            
            _reloadEventEmitter.Play();
        }
        
        public void StopReload()
        {
            if (!_reloadEventEmitter.IsPlaying())
            {
                return;
            }
            
            _reloadEventEmitter.Stop();
        }
    }
}
