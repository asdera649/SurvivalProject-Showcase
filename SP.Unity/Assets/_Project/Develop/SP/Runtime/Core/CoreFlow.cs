using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Mirror;
using SP.Runtime.Bootstrap.Services.BackendInteractionService;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Services.AuthorizationService;
using SP.Runtime.Core.Services.BuildingService;
using SP.Runtime.Core.Services.ClientObjectsInjectionService;
using SP.Runtime.Core.Services.MiniMapSaveService;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Meta.Services.NetworkService;
using SP.Runtime.Utilities;
using UnityEngine;
using VContainer.Unity;

namespace SP.Runtime.Core
{
    [UsedImplicitly]
    public class CoreFlow : IStartable
    {
        public CoreFlow(
            LoadingService.LoadingService loadingService,
            BackendInteractionService backendInteractionService,
            ServerBackupHandler serverBackupHandler,
            ServerInfoSender serverInfoSender,
            ClientObjectsInjectionService clientObjectsInjectionService,
            BuildingService buildingService,
            SaveService saveService,
            AuthorizationService authorizationService,
            MiniMapSaveService miniMapSaveService,
            DeveloperTools developerTools)
        {
            _loadingService = loadingService;
            _backendInteractionService = backendInteractionService;
            _serverBackupHandler = serverBackupHandler;
            _serverInfoSender = serverInfoSender;
            _clientObjectsInjectionService = clientObjectsInjectionService;
            _buildingService = buildingService;
            _saveService = saveService;
            _authorizationService = authorizationService;
            _miniMapSaveService = miniMapSaveService;
            _developerTools = developerTools;
        }
        
        private readonly LoadingService.LoadingService _loadingService;
        private readonly BackendInteractionService _backendInteractionService;
        private readonly ServerBackupHandler _serverBackupHandler;
        private readonly ServerInfoSender _serverInfoSender;
        private readonly ClientObjectsInjectionService _clientObjectsInjectionService;
        private readonly BuildingService _buildingService;
        private readonly SaveService _saveService;
        private readonly AuthorizationService _authorizationService;
        private readonly MiniMapSaveService _miniMapSaveService;
        private readonly DeveloperTools _developerTools;
        
        public async void Start()
        {
            if (MirrorUtils.IsHeadless)
            {
                await _loadingService.BeginLoading(_serverBackupHandler);
            }
            
            await _loadingService.BeginLoading(_clientObjectsInjectionService);
            await _loadingService.BeginLoading(_buildingService);
            await _loadingService.BeginLoading(_saveService);
            await _loadingService.BeginLoading(_authorizationService);
            await _loadingService.BeginLoading(_miniMapSaveService);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            await _loadingService.BeginLoading(_developerTools);
#endif

            if (MirrorUtils.IsHeadless)
            {
                await SetServerReady();
            }

            if (MirrorUtils.IsHeadless)
            {
                await _loadingService.BeginLoading(_serverInfoSender);
            }
            
            // Сервер закончил свою настройку, включаем возможность подключения клиентов.
            DisableLockNewConnections();
        }

        private async UniTask SetServerReady()
        {
            var isLog = true;
            
            start:

            if (isLog)
            {
                Debug.Log("[CoreFlow]: Setting the server to the ready state...");
            }
                
            var result = await _backendInteractionService.SetServerReady(isLog);

            if (!result)
            {
                if (isLog)
                {
                    Debug.LogWarning("[CoreFlow]: Failed to set the server to the ready state. " +
                                  "Let's keep trying, in case of success a message will be displayed...");
                }

                isLog = false;
                
                await UniTask.Delay(5000);
                
                goto start;
            }
            
            Debug.Log("[CoreFlow]: Successfully set the server to the ready state!");
        }

        [ServerCallback]
        private void DisableLockNewConnections()
        {
            NetworkService.singleton.SetLockNewConnections(false);
        }
    }
}
