using Cysharp.Threading.Tasks;
using Mirror;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.LoadingService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Core.Services.ClientObjectsInjectionService
{
    public class ClientObjectsInjectionService : MonoBehaviour, ILoadUnit
    {
        [SerializeField] private Player _player;

        private IObjectResolver _container;
        
        [Inject]
        private void Inject(IObjectResolver container)
        {
            _container = container;
        }
        
        public UniTask Load()
        {
            if (_player.TryGetComponent<NetworkIdentity>(out var identity))
            {
                NetworkClient.UnregisterPrefab(_player.gameObject);
                NetworkClient.RegisterSpawnHandler(identity.assetId, SpawnPlayer, UnSpawnPlayer);   
            }
            
            return UniTask.CompletedTask;
        }

        private GameObject SpawnPlayer(SpawnMessage msg)
        {
            var player = Instantiate(_player.gameObject, msg.position, msg.rotation);
            player.transform.localScale = msg.scale;
            
            _container.InjectGameObject(player);

            return player;
        }
        
        private void UnSpawnPlayer(GameObject spawned)
        {
            Destroy(spawned);
        }
    }
}