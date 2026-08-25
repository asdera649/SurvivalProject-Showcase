using Cysharp.Threading.Tasks;
using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Utilities;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Services.SaveService
{
    public class SaveService : BaseSaveService
    {
        [Header("SaveService")]
        
        [Header("Settings")] 
        [SerializeField] private TimeService.TimeService _savedTimeService;
        [SerializeField] private Player _savedPlayer;
        [SerializeField] private BuildingEntity[] _savedBuildings;

        [Space(10)] 
        
        [SerializeField] private bool _loadLastSaveAtStartup;
        [SerializeField] private float _savingRate = 300;

        private BuildingService.BuildingService _buildingService;
        private AuthorizationService.AuthorizationService _authorizationService;
        private PlayerDeathPlacemarkHandler.PlayerDeathPlacemarkHandler _playerDeathPlacemarkHandler;
        private TimeService.TimeService _timeService;
        
        [Inject]
        private void Inject(
            BuildingService.BuildingService buildingService,
            AuthorizationService.AuthorizationService authorizationService,
            PlayerDeathPlacemarkHandler.PlayerDeathPlacemarkHandler playerDeathPlacemarkHandler,
            TimeService.TimeService timeService)
        {
            _buildingService = buildingService;
            _authorizationService = authorizationService;
            _playerDeathPlacemarkHandler = playerDeathPlacemarkHandler;
            _timeService = timeService;
        }
        
        public override UniTask Load()
        {
            base.Load();
            
            Initialize();
            
            return UniTask.CompletedTask;
        }

        [ServerCallback]
        private void Initialize()
        {
            RegisterCustomSpawnHandler(_savedTimeService.SaveHandler.Guid, GetTimeServiceSaveHandler);
            
            RegisterCustomSpawnHandler(_authorizationService.SaveHandler.Guid, GetAuthorizationServiceSaveHandler);
            
            RegisterCustomSpawnHandler(_playerDeathPlacemarkHandler.SaveHandler.Guid, GetPlayerDeathPlacemarkSaveHandler);
            
            RegisterCustomSpawnHandler(_savedPlayer.SaveHandler.Guid, InstantiatePlayer);

            foreach (var b in _savedBuildings)
            {
                RegisterCustomSpawnHandler(
                    b.SaveHandler.Guid,
                    (position, rotation, localScale) => PlacePiece(b, position, rotation, localScale));
            }

            if (MirrorUtils.IsHeadless)
            {
                StartAutosave();
            }

            if (_loadLastSaveAtStartup)
            {
                LoadProgress();
            }
        }
        
        private void OnDestroy()
        {
            StopAutosave();
        }

        [ServerCallback]
        private void StartAutosave()
        {
            Debug.Log("Autosave is running!");
                
            InvokeRepeating(nameof(SaveProgress), _savingRate, _savingRate);
        }
        
        private void StopAutosave()
        {
            CancelInvoke(nameof(SaveProgress));
        }
        
        [ServerCallback]
        private void SaveProgress()
        {
            Save();
            
            Debug.Log("Progress saved!");
        }

        [ServerCallback]
        private void LoadProgress()
        {
            LoadSave();
            
            Debug.Log("Last save loaded!");
        }

        private SaveHandler GetTimeServiceSaveHandler(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            return _timeService.SaveHandler;
        }
        
        private SaveHandler GetAuthorizationServiceSaveHandler(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            return _authorizationService.SaveHandler;
        }

        private SaveHandler GetPlayerDeathPlacemarkSaveHandler(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            return _playerDeathPlacemarkHandler.SaveHandler;
        }

        private SaveHandler InstantiatePlayer(Vector3 position, Quaternion rotation, Vector3 localScale)
        {
            return _authorizationService.InstantiatePlayer(
                "Setup by SaveService",
                "Setup by SaveService",
                position).SaveHandler;
        }

        private SaveHandler PlacePiece(
            BuildingEntity building,
            Vector3 position,
            Quaternion rotation,
            Vector3 localScale)
        {
            return _buildingService.PlacePiece(
                building.PieceBehaviour,
                position,
                rotation.eulerAngles,
                localScale).GetComponent<SaveHandler>();
        }
    }
}