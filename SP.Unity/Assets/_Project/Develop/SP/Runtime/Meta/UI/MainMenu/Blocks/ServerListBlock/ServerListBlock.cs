using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Mirror;
using SP.Runtime.Bootstrap.Services.BackendInteractionService;
using SP.Runtime.Localization;
using SP.Runtime.Meta.Services.NetworkService;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;
using VContainer;

namespace SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock
{
    public class ServerListBlock : BaseBlock
    {
        #region Structs

        private enum Tabs
        {
            All,
            Favorites,
            History
        }

        #endregion

        [Header("Prefabs")] 
        [SerializeField] private ServerListElement _serverListElementPrefab;
        
        [Header("References")]
        [SerializeField] private GameObject _serverListContainer;
        
        [SerializeField] private ServerListTab _serversTab;
        [SerializeField] private ServerListTab _favoritesTab;
        [SerializeField] private ServerListTab _historyTab;

        [SerializeField] private Button _refreshButton;

        [SerializeField] private LocalizeStringHelper _serversWipeTipLocalize;

        [SerializeField] private ServerListLoadingBlock _serverListLoadingBlock;
        [SerializeField] private ServerListFailBlock _serverListFailBlock;

        [SerializeField] private MainMenuConnectingStatusBlock _connectingStatusBlock;
        [SerializeField] private MainMenuFailBlock _failBlock;
        
        private Tabs _activeTab;
        private Tabs ActiveTab
        {
            get => _activeTab;
            set
            {
                var oldValue = _activeTab;
                _activeTab = value;
                
                OnActiveTabUpdated(oldValue, _activeTab);
            }
        }

        private SavedServerListContainer _savedServerListContainer;

        private bool _isServerListUpdating;

        private bool _isConnecting;

        private bool _isFirstTimeUpdated;
        
        private readonly List<ServerListElement> _serverListElements = new();
        
        private ServerListElement _lastActiveServerListElement;

        private BackendInteractionService _backendInteractionService;
        
        [Inject]
        private void Inject(BackendInteractionService backend)
        {
            _backendInteractionService = backend;
        }

        private void Awake()
        {
            _savedServerListContainer = new SavedServerListContainer();
            _savedServerListContainer.Initialize();
        }

        private void OnEnable()
        {
            _serversTab.Button.onClick.AddListener(OnServersTabClicked);
            _favoritesTab.Button.onClick.AddListener(OnFavoritesTabClicked);
            _historyTab.Button.onClick.AddListener(OnHistoryTabClicked);
            
            _refreshButton.onClick.AddListener(OnRefreshButtonClicked);
        }
        
        private void Start()
        {
            ActiveTab = Tabs.All;
        }

        private void OnDisable()
        {
            _serversTab.Button.onClick.RemoveListener(OnServersTabClicked);
            _favoritesTab.Button.onClick.RemoveListener(OnFavoritesTabClicked);
            _historyTab.Button.onClick.RemoveListener(OnHistoryTabClicked);
            
            _refreshButton.onClick.RemoveListener(OnRefreshButtonClicked);

            ClearServerListElements();
        }

        private async Task UpdateServerList()
        {
            if (_isServerListUpdating)
            {
                return;
            }
            
            _isFirstTimeUpdated = true;
            
            _isServerListUpdating = true;

            ClearServerListElements();
            
            _serverListFailBlock.Hide();
            
            _serverListLoadingBlock.gameObject.SetActive(true);
            
            var remainingTimeToWipe = await _backendInteractionService.GetRemainingTimeToWipe();

            _serversWipeTipLocalize.SetEntry(
                new EntryContainer(
                    remainingTimeToWipe.Days is > 0 or -1 ?
                        "TheProgressOfAllServersWillReset" :
                        "TheProgressOfAllServersWillReset_Warning",
                    new LocalVariable[]
                    {
                        new("days", 
                            new StringVariable()
                            {
                                Value = remainingTimeToWipe.Days == -1 ? "?" : remainingTimeToWipe.Days.ToString()
                            }),
                        new("hours", 
                            new StringVariable()
                            {
                                Value = remainingTimeToWipe.Hours == -1 ? "?" : remainingTimeToWipe.Hours.ToString()
                            }),
                        new("minutes", 
                            new StringVariable()
                            {
                                Value = remainingTimeToWipe.Minutes == -1 ? "?" : remainingTimeToWipe.Minutes.ToString()
                            })
                    }));
            
            var serverList = await _backendInteractionService.GetServers();
            
            _serverListLoadingBlock.gameObject.SetActive(false);

            if (serverList.RequestResult != BackendInteractionService.RequestResult.Successful)
            {
                switch (serverList.RequestResult)
                {
                    case BackendInteractionService.RequestResult.Successful:
                    {
                        break;
                    }
                    case BackendInteractionService.RequestResult.Obsolete:
                    {
                        _serverListFailBlock.Show(ServerListFailBlock.FailType.Obsolete);
                        break;
                    }
                    case BackendInteractionService.RequestResult.NoConnection:
                    {
                        _serverListFailBlock.Show(ServerListFailBlock.FailType.NoConnection);
                        break;
                    }
                    case BackendInteractionService.RequestResult.UnknownError:
                    {
                        _serverListFailBlock.Show(ServerListFailBlock.FailType.UnknownError);
                        break;
                    }
                    default:
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
                
                goto Exit;
            }

            if (serverList.Servers.Count == 0)
            {
                _serverListFailBlock.Show(ServerListFailBlock.FailType.Empty);
                goto Exit;
            }

            var serversPing = 
                await _backendInteractionService.GetServersPing(serverList.Servers);
            
            InstantiateServerListElements(serversPing);
            
            Exit:
            
            _isServerListUpdating = false;
        }

        private void ClearServerListElements()
        {
            foreach (var e in _serverListElements)
            {
                e.Clicked -= OnClicked;
                e.HoldCompleted -= OnHoldCompleted;
                e.FavoriteButtonClicked -= OnFavoriteButtonClicked;
                
                Destroy(e.gameObject);
            }
            
            _serverListElements.Clear();
        }

        private void InstantiateServerListElements(Dictionary<BackendInteractionService.ServerInfo, long> serversPing)
        {
            ClearServerListElements();
            
            foreach (var s in serversPing)
            {
                var e = Instantiate(_serverListElementPrefab, _serverListContainer.transform);
                
                e.Initialize(
                    s.Key.UniqueId,
                    s.Key.Name,
                    s.Key.MaxPlayersCount,
                    s.Key.CurrentPlayersCount,
                    s.Value,
                    _savedServerListContainer.FavoriteServersNameHistory.Contains(s.Key.Name));
                
                e.Clicked += OnClicked;
                e.HoldCompleted += OnHoldCompleted;
                e.FavoriteButtonClicked += OnFavoriteButtonClicked;
                
                _serverListElements.Add(e);
            }

            ActiveTab = ActiveTab;
        }

        private async void Connect(string serverName, string uniqueId)
        {
            if (_isConnecting)
            {
                return;
            }

            _isConnecting = true;
            
            _serverListFailBlock.Hide();
            
            _connectingStatusBlock.Show();

            var serverConnectionInfo = await _backendInteractionService.GetServerConnectionInfo(uniqueId);
            
            switch (serverConnectionInfo.RequestResult)
            {
                case BackendInteractionService.RequestResult.Successful:
                {
                    NetworkService.singleton.networkAddress = serverConnectionInfo.ConnectionInfo.PublicIp;
                    
                    if (NetworkService.singleton.transport is PortTransport portTransport)
                    {
                        portTransport.Port = serverConnectionInfo.ConnectionInfo.ExternalPort;
                    }
                    
                    _savedServerListContainer.AddToHistory(serverName);
                    
                    NetworkService.singleton.StartClient();

                    while (NetworkClient.isConnecting)
                    {
                        await UniTask.Delay(1000);
                    }

                    if (!NetworkClient.isConnected)
                    {
                        _failBlock.Show(MainMenuFailBlock.FailType.NoConnection);
                    }
                    
                    break;
                }
                case BackendInteractionService.RequestResult.Obsolete:
                {
                    _failBlock.Show(MainMenuFailBlock.FailType.Obsolete);
                    break;
                }
                case BackendInteractionService.RequestResult.NoConnection:
                {
                    _failBlock.Show(MainMenuFailBlock.FailType.NoConnection);
                    break;
                }
                case BackendInteractionService.RequestResult.UnknownError:
                {
                    _failBlock.Show(MainMenuFailBlock.FailType.UnknownError);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _connectingStatusBlock.Hide();

            _isConnecting = false;
        }

        private void ApplyTabToServerList(Tabs tab)
        {
            foreach (var e in _serverListElements)
            {
                switch(tab)
                {
                    case Tabs.All:
                    {
                        e.gameObject.SetActive(true);
                        break;
                    }
                    case Tabs.Favorites:
                    {
                        e.gameObject.SetActive(_savedServerListContainer.FavoriteServersNameHistory.Contains(e.Name));
                        break;
                    }
                    case Tabs.History:
                    {
                        e.gameObject.SetActive(_savedServerListContainer.VisitedServersNameHistory.Contains(e.Name));
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tab), tab, null);
                }
            }
        }

        #region Callbacks
        
        private void OnServersTabClicked()
        {
            ActiveTab = Tabs.All;
        }

        private void OnFavoritesTabClicked()
        {
            ActiveTab = Tabs.Favorites;
        }

        private void OnHistoryTabClicked()
        {
            ActiveTab = Tabs.History;
        }

        private async void OnRefreshButtonClicked()
        {
            await UpdateServerList();
        }

        private void OnClicked(ServerListElement element)
        {
            if (_lastActiveServerListElement != null)
            {
                _lastActiveServerListElement.SetActive(false);
            }

            element.SetActive(true);
            
            _lastActiveServerListElement = element;
        }
        
        private void OnHoldCompleted(ServerListElement element)
        {
            Connect(element.Name, element.UniqueId);
        }

        private void OnFavoriteButtonClicked(ServerListElement element)
        {
            if (_savedServerListContainer.FavoriteServersNameHistory.Contains(element.Name))
            {
                _savedServerListContainer.RemoveFromFavorite(element.Name);
            }
            else
            {
                _savedServerListContainer.AddToFavorite(element.Name);
            }
        }

        private void OnActiveTabUpdated(Tabs oldValue, Tabs newValue)
        {
            switch (oldValue)
            {
                case Tabs.All:
                {
                    _serversTab.SetActive(false);
                    break;
                }
                case Tabs.Favorites:
                {
                    _favoritesTab.SetActive(false);
                    break;
                }
                case Tabs.History:
                {
                    _historyTab.SetActive(false);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(oldValue), oldValue, null);
                }
            }

            switch (newValue)
            {
                case Tabs.All:
                {
                    _serversTab.SetActive(true);
                    break;
                }
                case Tabs.Favorites:
                {
                    _favoritesTab.SetActive(true);
                    break;
                }
                case Tabs.History:
                {
                    _historyTab.SetActive(true);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null);
                }
            }
            
            ApplyTabToServerList(newValue);
        }
        
        protected override async void OnViewUpdate(bool value)
        {
            if (value && !_isFirstTimeUpdated)
            {
                await UpdateServerList();
            }
        }
        
        #endregion
    }
}
