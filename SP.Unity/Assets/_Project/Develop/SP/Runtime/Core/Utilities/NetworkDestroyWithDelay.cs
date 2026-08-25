using Mirror;
using UnityEngine;

namespace SP.Runtime.Core.Utilities
{
    public class NetworkDestroyWithDelay : NetworkBehaviour
    {
        [SerializeField] private float _delay = 1;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            Invoke(nameof(Destroy), _delay);
        }

        public override void OnStopServer()
        {
            CancelInvoke(nameof(Destroy));
            
            base.OnStopServer();
        }

        [ServerCallback]
        private void Destroy()
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
