using SP.Runtime.Core.Services;
using SP.Runtime.Core.Services.AuthorizationService;
using SP.Runtime.Core.Services.BuildingService;
using SP.Runtime.Core.Services.ClientObjectsInjectionService;
using SP.Runtime.Core.Services.EnvironmentService;
using SP.Runtime.Core.Services.MiniMapSaveService;
using SP.Runtime.Core.Services.PlayerDeathPlacemarkHandler;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Core.Services.TimeService;
using SP.Runtime.Core.Systems;
using SP.Runtime.Core.UI;
using SP.Runtime.Core.UI.MiniMap;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Core
{
    public class CoreScope : LifetimeScope
    {
        [Space(10)]
        
        [Header("Prefabs")]
        [SerializeField] private MiniMap _PCMiniMapPrefab;
        [SerializeField] private MiniMap _mobileMiniMapPrefab;
        [SerializeField] private PauseMenu _pauseMenuPrefab;

        [Header("References")] 
        [SerializeField] private ClientObjectsInjectionService _clientObjectsInjectionService;
        [SerializeField] private BuildingService _buildingService;
        [SerializeField] private SaveService _saveService;
        [SerializeField] private AuthorizationService _authorizationService;
        [SerializeField] private MiniMapSaveService _miniMapSaveService;
        [SerializeField] private PlayerDeathPlacemarkHandler _playerDeathPlacemarkHandler;
        [SerializeField] private TimeService _timeService;
        [SerializeField] private EnvironmentService _environmentService;
        [SerializeField] private DeveloperTools _developerTools;

        [Space(10)] 
        
        [SerializeField] private PostProcessingHandler _postProcessingHandler;
        
        [Header("Settings")]
        
        [Header("Map")] 
        [SerializeField] private Sprite _mapTexture;
        [SerializeField] private float _worldDeltaSize;
        [SerializeField] private Vector2 _fullMapStartPositionOffset;
        [SerializeField] private Vector3 _fullMapRotationOffset;
        [SerializeField] private float _boundX = 512;
        [SerializeField] private float _boundY = 512;
        [SerializeField] private float _defaultZoom = 1;
        [SerializeField] private float _maxZoom = 2;
        [SerializeField] private float _minZoom = 0.75f;
        [SerializeField] private float _miniMapZoom = 1;
        
        public void Start()
        {
            Build();
        }

        public void OnValidate()
        {
            if (autoRun)
            {
                Debug.LogWarning("The “autoRun” should remain disabled!");
            }
            
            autoRun = false;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            #region ServerSideLogic
            
            builder.Register<ServerBackupHandler>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ServerInfoSender>().AsSelf();
            
            #endregion

            #region Main
            
            builder.RegisterComponent(_clientObjectsInjectionService).AsSelf();
            builder.RegisterComponent(_buildingService).AsSelf();
            builder.RegisterComponent(_saveService).AsSelf();
            builder.RegisterComponent(_authorizationService).AsSelf();
            builder.RegisterComponent(_miniMapSaveService).AsSelf();
            builder.RegisterComponent(_playerDeathPlacemarkHandler).AsSelf();
            builder.RegisterComponent(_timeService).AsSelf();
            builder.RegisterComponent(_environmentService).AsSelf();
            builder.RegisterComponent(_developerTools).AsSelf();
            
            #endregion

            #region UI
            
            builder.RegisterComponent(InstantiateMiniMap()).AsSelf();
            
            var pauseMenu = Instantiate(_pauseMenuPrefab);
            builder.RegisterComponent(pauseMenu).AsSelf();
            builder.RegisterComponent(pauseMenu.SettingsBlock).AsSelf();
            
            #endregion
            
            builder.RegisterComponent(_postProcessingHandler);
            
            builder.RegisterEntryPoint<CoreFlow>();
        }
        
        private MiniMap InstantiateMiniMap()
        {
            MiniMap output = null;
            
            var runtimePlatform = Application.platform;
            
            if (runtimePlatform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor)
            {
                output = Instantiate(_PCMiniMapPrefab);
            }
            else if (runtimePlatform == RuntimePlatform.Android)
            {
                output = Instantiate(_mobileMiniMapPrefab);
            }

            if (output == null)
            {
                output = Instantiate(_PCMiniMapPrefab);
            }
            
            output.Initialize(
                _mapTexture,
                _worldDeltaSize,
                _fullMapStartPositionOffset,
                _fullMapRotationOffset,
                _boundX,
                _boundY,
                _defaultZoom,
                _maxZoom,
                _minZoom,
                _miniMapZoom);

            return output;
        }
    }
}
