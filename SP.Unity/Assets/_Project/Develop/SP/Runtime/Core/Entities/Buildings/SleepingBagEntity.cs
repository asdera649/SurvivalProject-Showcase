using FMODUnity;
using Mirror;
using SP.Runtime.Core.Services.AuthorizationService;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Core.Systems.Interaction;
using SP.Runtime.Core.UI;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Entities.Buildings
{
    [RequireComponent(typeof(InteractionHandler))]
    public class SleepingBagEntity : BuildingEntity
    {
        [Header("Prefabs")]
        [SerializeField] private SleepingBagRenameMenu _renameMenuPrefab;
        
        [Header("References")] 
        [SerializeField] private StudioEventEmitter _openEventEmitter;
        [SerializeField] private StudioEventEmitter _closeEventEmitter;

        [Header("Settings")] 
        [SerializeField] private float _respawnPointCooldown = 300;
        [SerializeField] private float _renameMenuDestroyDistance = 3;
    
        [SaveHandler.Saved]
        private string _ownerUniqueId;
        public string OwnerUniqueId => _ownerUniqueId;
        
        [SyncVar, SaveHandler.Saved] 
        private string _sleepingBagName = _sleepingBagDefaultName;
        
        private bool _isInitialized;
        
        private const string _sleepingBagDefaultName = "New sleeping bag.";
        
        private AuthorizationService.RespawnPoint _respawnPoint;

        private static SleepingBagRenameMenu _sleepingBagRenameMenu;
        
        private DistanceDestroyedObjectFactory _distanceDestroyedObjectFactory;

        private InteractionHandler _interactionHandler;
        private InteractionHandler InteractionHandler
        {
            get
            {
                if (_interactionHandler == null)
                {
                    _interactionHandler = GetComponent<InteractionHandler>();
                }

                return _interactionHandler;
            }
        }

        private AuthorizationService _authorizationService;

        [Inject]
        private void Inject(AuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }
        
        protected override void Awake()
        {
            base.Awake();

            _distanceDestroyedObjectFactory = new DistanceDestroyedObjectFactory(_renameMenuDestroyDistance);
        }
        
        public override void OnStopServer()
        {
            if (_isInitialized)
            {
                RemoveSleepingBag();
            }
            
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            InteractionHandler.ExecutionAction += OnExecution;
        }

        public override void OnStopClient()
        {
            InteractionHandler.ExecutionAction -= OnExecution;
            
            DestroyRenameMenu();
            
            base.OnStopClient();
        }

        [ServerCallback]
        public void Initialize(string ownerUniqueId)
        {
            _ownerUniqueId = ownerUniqueId;
            
            AddSleepingBag();

            _isInitialized = true;
        }
        
        [ServerCallback]
        private void AddSleepingBag()
        {
            if (_authorizationService.TryAddRespawnPoint(
                    _ownerUniqueId,
                    _sleepingBagName,
                    transform.position,
                    _respawnPointCooldown,
                    out var respawnPointUniqueId))
            {
                _respawnPoint = respawnPointUniqueId;
            }
        }

        [ServerCallback]
        private void RemoveSleepingBag()
        {
            if (_respawnPoint == null)
            {
                return;
            }
            
            _authorizationService.RemoveRespawnPoint(_respawnPoint);
        }

        [ServerCallback]
        private void RenameSleepingBag(string sleepingBagName)
        {
            if (_respawnPoint == null || string.IsNullOrEmpty(sleepingBagName))
            {
                return;
            }
            
            _sleepingBagName = sleepingBagName;

            _respawnPoint.Setup(_sleepingBagName);
        }
        
        private void OnExecution()
        {
            if (NetworkClient.localPlayer == null ||
                !NetworkClient.localPlayer.TryGetComponent(out Player.Player player))
            {
                return;
            }

            InstantiateRenameMenu(player);
        }

        private void InstantiateRenameMenu(Player.Player player)
        {
            if (_sleepingBagRenameMenu != null)
            {
                return;
            }
            
            _sleepingBagRenameMenu = _distanceDestroyedObjectFactory.Instantiate(
                this,
                player.transform,
                transform,
                _renameMenuPrefab.gameObject).GetComponent<SleepingBagRenameMenu>();
            
            _sleepingBagRenameMenu.RenameConfirmed += OnRenameConfirmed;
            _sleepingBagRenameMenu.Destroyed += OnRenameMenuDestroyed;
            _sleepingBagRenameMenu.CancelButton.onClick.AddListener(OnCancelButtonClicked);
            _sleepingBagRenameMenu.Initialize(_sleepingBagName);

            CmdInvokeOpenOrCloseImpacts(true);
        }

        private void DestroyRenameMenu()
        {
            if (_sleepingBagRenameMenu == null)
            {
                return;
            }
            
            Destroy(_sleepingBagRenameMenu.gameObject);
        }

        private void OnRenameMenuDestroyed(SleepingBagRenameMenu sleepingBagRenameMenu)
        {
            _sleepingBagRenameMenu.RenameConfirmed -= OnRenameConfirmed;
            _sleepingBagRenameMenu.Destroyed -= OnRenameMenuDestroyed;
            _sleepingBagRenameMenu.CancelButton.onClick.RemoveListener(OnCancelButtonClicked);
            
            CmdInvokeOpenOrCloseImpacts(false);
        }
        
        [Command(requiresAuthority = false)]
        private void CmdInvokeOpenOrCloseImpacts(bool isOpen)
        {
            RpcInvokeOpenOrCloseImpacts(isOpen);
        }
        
        [ClientRpc]
        private void RpcInvokeOpenOrCloseImpacts(bool isOpen)
        {
            if (isOpen)
            {
                _openEventEmitter.Play();
            }
            else
            {
                _closeEventEmitter.Play();
            }
        }
        
        [ClientCallback]
        private void OnRenameConfirmed(string sleepingBagName)
        {
            CmdRenameSleepingBag(sleepingBagName);
            DestroyRenameMenu();
        }
        
        private void OnCancelButtonClicked()
        {
            DestroyRenameMenu();
        }
        
        [Command(requiresAuthority = false)]
        private void CmdRenameSleepingBag(string sleepingBagName)
        {
            RenameSleepingBag(sleepingBagName);
        }
    }
}
