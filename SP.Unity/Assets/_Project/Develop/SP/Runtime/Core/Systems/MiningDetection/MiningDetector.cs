using System.Collections.Generic;
using System.Linq;
using Mirror;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.MiningDetection
{
    public class MiningDetector : NetworkBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private MiningDetectorSettings _miningDetectorSettings;
        [SerializeField] private float _detectionRadius = 45;
        [SerializeField] private float _updateRate = 0.5f;

        private readonly List<MiningDetectionHandler> _handlersCache = new();

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            InvokeRepeating(nameof(UpdateDetection), _updateRate, _updateRate);
        }

        public override void OnStopLocalPlayer()
        {
            CancelInvoke(nameof(UpdateDetection));

            Clear();

            base.OnStopLocalPlayer();
        }

        private void UpdateDetection()
        {
            if (!_miningDetectorSettings.IsEnabled)
            {
                Clear();
                return;
            }
            
            var handlers = PhysicUtils.GetTypesBySphere<MiningDetectionHandler>(
                transform.position,
                _detectionRadius,
                -1,
                QueryTriggerInteraction.Ignore);

            foreach (var h in handlers)
            {
                EnableSelection(h);
            }
            
            for (var i = _handlersCache.Count - 1; i >= 0; i--)
            {
                if (_handlersCache[i] == null)
                {
                    _handlersCache.RemoveAt(i);
                    continue;
                }

                if (handlers.All(h => h != _handlersCache[i]))
                {
                    DisableSelection(_handlersCache[i]);
                }
            }
        }

        private void EnableSelection(MiningDetectionHandler handler)
        {
            if (!_handlersCache.Contains(handler))
            {
                _handlersCache.Add(handler);
            }

            var percent = Mathf.Clamp01(
                1 - Vector3.Distance(transform.position, handler.transform.position) / _detectionRadius); 
            
            handler.EnableValueEffect(percent);
        }

        private void DisableSelection(MiningDetectionHandler handler)
        {
            if (_handlersCache.Contains(handler))
            {
                _handlersCache.Remove(handler);
            }
            
            handler.DisableValueEffect();
        }

        private void Clear()
        {
            for (var i = _handlersCache.Count - 1; i >= 0; i--)
            {
                DisableSelection(_handlersCache[i]);
            }
            
            _handlersCache.Clear();
        }
    }
}