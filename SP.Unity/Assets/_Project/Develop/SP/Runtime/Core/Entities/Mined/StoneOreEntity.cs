using FMODUnity;
using Mirror;
using SP.Runtime.Core.Systems.Destruction;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Mined
{
    public class StoneOreEntity : MinedEntity
    {
        [Header("References")]
        [SerializeField] private StudioEventEmitter _oreDestructionEventEmitter;
        
        [ServerCallback]
        protected override void OnDeath()
        {
            base.OnDeath();

            // Если это хост то просто вызываем Destruct().
            // Все потому, что если у хоста перед уничтожением отправить Rpc,то он, не отправится.
            
            if (isServer && isClient)
            {
                Destruct();
            }
            
            RpcDestruct();
        }
        
        [ClientRpc]
        private void RpcDestruct()
        {
            Destruct();
        }
        
        private void Destruct()
        {
            _oreDestructionEventEmitter.Play();
            
            if (NetworkClient.localPlayer == null || !TryGetComponent(out DestructionHandler dh))
            {
                return;
            }
            
            dh.Destruct();
        }
    }
}
