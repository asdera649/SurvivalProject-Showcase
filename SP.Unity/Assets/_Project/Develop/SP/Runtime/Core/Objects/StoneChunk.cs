using SP.Runtime.Core.Items.Settings;
using SP.Runtime.Core.Items.Settings.Impact;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class StoneChunk : CollisionDamageObject
    {
        [Header("References")] 
        [SerializeField] private Transform _smokeTrail;
        
        [Header("Settings")]
        [SerializeField] private ImpactSettings _impactSettings;

        private Vector3 _lastPosition_v2;

        private void Start()
        {
            _lastPosition_v2 = transform.position;
        }

        private void LateUpdate()
        {
            _lastPosition_v2 = transform.position;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (_smokeTrail != null)
            {
                _smokeTrail.SetParent(null);
            }

            var direction = (transform.position - _lastPosition_v2).normalized;
            
            _impactSettings.SpawnDefaultImpacts(transform.position - direction * 0.2f);
        }
    }
}