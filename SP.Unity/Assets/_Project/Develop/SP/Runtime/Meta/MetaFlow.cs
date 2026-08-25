using JetBrains.Annotations;
using SP.Runtime.Meta.Services.NetworkService;
using VContainer.Unity;

namespace SP.Runtime.Meta
{
    [UsedImplicitly]
    public class MetaFlow : IStartable
    {
        public void Start()
        {
            NetworkService.singleton.ServerStarted += OnServerStarted;
        }
        
        private void OnServerStarted()
        {
            // Отключаем возможность подключения клиентов, до момента пока сервер не закончит свою настройку.
            // Далее включаем ее в CoreFlow.
            NetworkService.singleton.SetLockNewConnections(true);
            
            NetworkService.singleton.ServerStarted -= OnServerStarted;
        }
    }
}
