using System.Collections;
using Mirror;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class TimedExplosiveObject : ExplosiveObject
    {
        [Header("Settings")]
        [SerializeField] private float _delay;

        private Coroutine _delayProcess;

        public override void OnStartServer()
        {
            base.OnStartServer();

            StartDelayProcess();
        }

        public override void OnStopServer()
        {
            StopDelayProcess();
            
            base.OnStopServer();
        }

        [ServerCallback]
        private void StartDelayProcess()
        {
            _delayProcess ??= StartCoroutine(Delay(_delay));
        }

        [ServerCallback]
        private void StopDelayProcess()
        {
            if (_delayProcess != null)
            {
                StopCoroutine(_delayProcess);
                _delayProcess = null;
            }
        }

        [ServerCallback]
        private IEnumerator Delay(float delay)
        {
            yield return new WaitForSeconds(delay);

            OnDelayCompleted();
        }

        [ServerCallback]
        protected virtual void OnDelayCompleted()
        {
            Explode();
        }
    }
}
