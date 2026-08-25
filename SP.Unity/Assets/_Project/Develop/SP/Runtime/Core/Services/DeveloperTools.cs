using Cysharp.Threading.Tasks;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.UI.DeveloperTools;
using SP.Runtime.LoadingService;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Services
{
    public class DeveloperTools : NetworkBehaviour, ILoadUnit
    {
        [Header("Prefab")] 
        [SerializeField] private DeveloperToolsMenu _developerToolsMenuPrefab;
        
        [Header("Settings")]
        [SerializeField] private BaseItem[] _receivedItems;

        private DeveloperToolsMenu _developerToolsMenu;
        
        private TimeService.TimeService _timeService;
        private SaveService.SaveService _saveService;

        [Inject]
        private void Inject(TimeService.TimeService timeService, SaveService.SaveService saveService)
        {
            _timeService = timeService;
            _saveService = saveService;
        }
        
        public UniTask Load()
        {
            InitializeClient();
            
            return UniTask.CompletedTask;
        }
        
        [Client]
        private void InitializeClient()
        {
            _developerToolsMenu = Instantiate(_developerToolsMenuPrefab);

            _developerToolsMenu.TimeIsSet += OnTimeIsSet;
            _developerToolsMenu.Saved += OnSaved;
            _developerToolsMenu.ItemClicked += OnItemClicked;
            
            _developerToolsMenu.Initialize(_receivedItems);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            
            _developerToolsMenu.TimeIsSet -= OnTimeIsSet;
            _developerToolsMenu.Saved -= OnSaved;
            _developerToolsMenu.ItemClicked -= OnItemClicked;
        }

        private void OnTimeIsSet(float time)
        {
            CmdSerTime(time);
        }
        
        private void OnSaved()
        {
            CmdSave();
        }
        
        private void OnItemClicked(int itemIndex)
        {
            CmdGetItem(itemIndex);
        }

        [Command(requiresAuthority = false)]
        private void CmdSerTime(float time)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _timeService.SetTime(time);
#endif
        }
        
        [Command(requiresAuthority = false)]
        private void CmdSave()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            _saveService.Save();
#endif
        }
        
        [Command(requiresAuthority = false)]
        private void CmdGetItem(int itemIndex)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var item = BaseItem.Instantiate(_receivedItems[itemIndex]);
            
            item.Quantity = item.MaxQuantity;
            
            if (NetworkClient.localPlayer != null)
            {
                if (NetworkClient.localPlayer.TryGetComponent<Systems.Inventory.Inventory>(out var inventory))
                {
                    inventory.Add(item);
                }
            }
#endif
        }
    }
}