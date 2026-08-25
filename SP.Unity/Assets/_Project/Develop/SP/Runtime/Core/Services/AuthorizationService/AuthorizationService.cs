using System;
using System.Collections.Generic;
using System.Linq;
using CI.QuickSave;
using Cysharp.Threading.Tasks;
using Mirror;
using Newtonsoft.Json;
using SP.Runtime.Bootstrap.Services.SettingsService;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Core.UI;
using SP.Runtime.Core.UI.Respawn;
using SP.Runtime.LoadingService;
using SP.Runtime.Meta.Services.NetworkService;
using UnityEngine;
using UnityEngine.Events;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Core.Services.AuthorizationService
{
    [RequireComponent(typeof(SaveHandler))]
    public class AuthorizationService : MonoBehaviour, ILoadUnit
    {
        #region Structs

        [Serializable]
        public class MapPlacemark
        {
            public MapPlacemark()
            {
                
            }
            
            [JsonConstructor]
            public MapPlacemark(
                string uniqueId,
                string name,
                string description,
                Vector3 worldPosition,
                string icon,
                Color? color)
            {
                UniqueId = uniqueId;
                Name = name;
                Description = description;
                WorldPosition = worldPosition;
                Icon = icon;
                Color = color;
            }
            
            public MapPlacemark(
                string name,
                string description,
                Vector3 worldPosition,
                string icon,
                Color? color)
            {
                UniqueId = Guid.NewGuid().ToString();
                Name = name;
                Description = description;
                WorldPosition = worldPosition;
                Icon = icon;
                Color = color;
            }

            public string UniqueId { get; }
            public string Name { get; }
            public string Description { get; }
            public Vector3 WorldPosition { get; }
            public string Icon { get; }
            public Color? Color { get; }
        }
        
        [Serializable]
        public class RespawnPoint
        {
            public RespawnPoint()
            {
                
            }
            
            public RespawnPoint(string name, Vector3 position, float baseCooldown)
            {
                UniqueId = Guid.NewGuid().ToString();
                Name = name;
                Position = position;

                BaseCooldown = baseCooldown;
                
                Setup(0);
            }
            
            public RespawnPoint(string uniqueId, string name, float cooldown)
            {
                UniqueId = uniqueId;
                Name = name;
                
                Setup(cooldown);
            }

            public string UniqueId { get; }
            public string Name { get; private set; }
            public Vector3 Position { get; }

            public float BaseCooldown { get; }
            
            private float _cooldown;
            public float Cooldown
            {
                get => _cooldown;
                private set => _cooldown = Mathf.Clamp(value, 0, float.MaxValue);
            }
            
            private float _lastTime;

            public void Setup(string respawnPointName)
            {
                Name = respawnPointName;
            }
            
            public void Setup(float cooldown)
            {
                Cooldown = cooldown;
                _lastTime = Time.time;
            }

            public void UpdateCooldown()
            {
                Cooldown -= Time.time - _lastTime;
                _lastTime = Time.time;
            }
        }
        
        [Serializable]
        private class User
        {
            [JsonConstructor]
            public User(string uniqueId, string token, List<MapPlacemark> mapPlacemarks, bool isNewUser)
            {
                UniqueId = uniqueId;
                Token = token;
                MapPlacemarks = mapPlacemarks ?? new List<MapPlacemark>();
                IsNewUser = isNewUser;
            }
            
            public User(string token)
            {
                UniqueId = Guid.NewGuid().ToString();
                Token = token;
                MapPlacemarks = new List<MapPlacemark>();
                IsNewUser = true;
            }

            public string UniqueId { get; }
            public string Token { get; }
            public List<MapPlacemark> MapPlacemarks { get; }
            public bool IsNewUser;

            [JsonIgnore] public Player Player;
            [JsonIgnore] public List<RespawnPoint> RespawnPoints = new();
            [JsonIgnore] public NetworkConnectionToClient ConnectionToClient;
        }
        
        #endregion
        
        #region Messages

        private struct LogInMsg : NetworkMessage
        {
            public string NickName;
            public string Token;
        }
        
        private struct LogInResponseMsg : NetworkMessage
        {
            //public string Token;
        }
        
        private struct SuccessfulLogInMsg : NetworkMessage
        {
            
        }

        private struct RespawnMsg : NetworkMessage
        {
            public string NickName; // Временное решение:
                                    // при респавне отправляем никнейм из SettingsService, в будущем надо 
                                    // сохранять никнейм в User
            public string RespawnPointUniqueId;
        }

        private struct RespawnDataRequestMsg : NetworkMessage
        {
        
        }

        private struct RespawnDataResponseMsg : NetworkMessage
        {
            public List<RespawnPoint> RespawnPoints;
        }
        
        private struct MapPlacemarksRequestMsg : NetworkMessage
        {
        
        }

        private struct MapPlacemarksResponseMsg : NetworkMessage
        {
            public List<MapPlacemark> Placemarks;
        }
        
        #endregion
        
        public event UnityAction<List<MapPlacemark>> LocalMapPlacemarksUpdated;
        
        [Header("Prefabs")] 
        [SerializeField] private Player _playerPrefab;
        [SerializeField] private GameObject _emptyPlayer;
        [SerializeField] private AuthorizationProcessPanel _authorizationProcessPanelPrefab;
        [SerializeField] private RespawnMenu _respawnMenuPrefab;

        private readonly List<User> _registeredUsers = new();

        //private readonly Dictionary<NetworkConnection, int> _registeredUsersCountPerConnection = new();
        
        // Wrapper, нужен для системы сохранения(SaveHandler)
        [SaveHandler.Saved]
        private List<User> RegisteredUsersWrapper
        {
            get => _registeredUsers;
            set
            {
                _registeredUsers.Clear();
                _registeredUsers.AddRange(value);
            }
        }

        private List<MapPlacemark> _localMapPlacemarks = new();
        public List<MapPlacemark> LocalMapPlacemarks
        {
            get => _localMapPlacemarks;
            private set
            {
                _localMapPlacemarks = value;
                LocalMapPlacemarksUpdated?.Invoke(_localMapPlacemarks);
            }
        }
        
        private bool _haveLocalPlayer;
        private bool HaveLocalPlayer 
        {
            get => _haveLocalPlayer;
            set
            {
                var oldValue = _haveLocalPlayer;
                _haveLocalPlayer = value;

                if (oldValue != _haveLocalPlayer)
                {
                    OnLocalPlayerUpdate(_haveLocalPlayer);
                }
            }
        }

        private const string _storedAuthorizationDataFileName = "StoredAuthorizationData";
        private const string _tokenKeyName = "Token";
        
        private const int _maxUsersCountPerConnection = 3;
        private const float _authorizationProcessPanelDestroyDelay = 1.5f;
        private const float _updateRate = 1;
        
        private AuthorizationProcessPanel _authorizationProcessPanel;
        private RespawnMenu _respawnMenu;
        
        private SaveHandler _saveHandler;
        public SaveHandler SaveHandler
        {
            get
            {
                if (_saveHandler == null)
                {
                    _saveHandler = GetComponent<SaveHandler>();
                }

                return _saveHandler;
            }
        }

        private SettingsService _settingsService;
        private IObjectResolver _container;
        
        [Inject]
        private void Inject(SettingsService settingsService, IObjectResolver container)
        {
            _settingsService = settingsService;
            _container = container;
        }
        
        //[Client]
        private void Start()
        {
            InstantiateAuthorizationProcessPanel();

            InstantiateRespawnMenu();
        }
        
        public UniTask Load()
        {
            InitializeServer();
            InitializeClient();
            
            return UniTask.CompletedTask;
        }
        
        [Server]
        private void InitializeServer()
        {
            NetworkService.singleton.ClientDisconnected += OnClientDisconnected;
            
            NetworkServer.RegisterHandler<LogInMsg>(OnLogIn);
            NetworkServer.RegisterHandler<RespawnMsg>(OnRespawn);
            NetworkServer.RegisterHandler<RespawnDataRequestMsg>(OnRespawnDataRequest);
            NetworkServer.RegisterHandler<MapPlacemarksRequestMsg>(OnMapPlacemarksRequest);
            
            InvokeRepeating(nameof(UpdateRespawnPointsCooldown), _updateRate, _updateRate);
        }
        
        [Client]
        private void InitializeClient()
        {
            NetworkClient.RegisterHandler<LogInResponseMsg>(OnLogInResponse);
            NetworkClient.RegisterHandler<SuccessfulLogInMsg>(OnSuccessfulLogIn);
            NetworkClient.RegisterHandler<RespawnDataResponseMsg>(OnRespawnDataResponse);
            NetworkClient.RegisterHandler<MapPlacemarksResponseMsg>(OnMapPlacemarksResponse);

            LogIn();
        }

        private void OnDestroy()
        {
            CleanUpServer();
            CleanUpClient();
        }

        private void CleanUpServer()
        {
            NetworkService.singleton.ClientDisconnected -= OnClientDisconnected;
            
            CancelInvoke(nameof(UpdateRespawnPointsCooldown));
            
            _registeredUsers.Clear();
        }

        private void CleanUpClient()
        {
            DestroyAuthorizationProcessPanel();

            DestroyRespawnMenu();   
        }
        
        private void Update()
        {
            UpdateLocalPlayer();
        }

        private void UpdateLocalPlayer()
        {
            HaveLocalPlayer = NetworkClient.localPlayer != null && NetworkClient.localPlayer.gameObject != null;
        }
        
        [Server]
        private void UpdateRespawnPointsCooldown()
        {
            foreach (var p in _registeredUsers.SelectMany(u => u.RespawnPoints))
            {
                p.UpdateCooldown();
            }
        }
        
        [Server]
        public Player InstantiatePlayer(string uniqueId, string nickName, Vector3 position)
        {
            var output = Player.Instantiate(_playerPrefab, uniqueId, nickName, position);
                
            _container.InjectGameObject(output.gameObject);
            
            return output;
        }
        
        [Server]
        public void RestorePlayerReference(Player player)
        {
            foreach (var u in _registeredUsers.Where(u => u.UniqueId == player.UniqueId && u.Player == null))
            {
                u.Player = player;
                return;
            }
        }
        
        [Server]
        private void ResetConnection(NetworkConnection conn)
        {
            foreach (var u in _registeredUsers.Where(u => u.ConnectionToClient == conn))
            {
                if (u.Player != null)
                {
                    NetworkServer.ReplacePlayerForConnection(
                        u.ConnectionToClient,
                        Instantiate(_emptyPlayer),
                        ReplacePlayerOptions.KeepActive);
                }
                    
                NetworkServer.DestroyPlayerForConnection(u.ConnectionToClient);
                    
                u.ConnectionToClient = null;
                    
                if (u.Player != null)
                {
                    var handlers = u.Player.GetComponents<IAuthorizationCallbacks>();

                    foreach (var h in handlers)
                    {
                        h.OnPlayerUnauthorized();
                    }
                }

                break;
            }

            // if (_registeredUsersCountPerConnection.ContainsKey(conn))
            // {
            //     _registeredUsersCountPerConnection.Remove(conn);
            // }
        }
        
        #region AuthorizationUI

        //[Client]
        private void InstantiateAuthorizationProcessPanel()
        {
            if (_authorizationProcessPanel != null)
            {
                return;
            }

            _authorizationProcessPanel = Instantiate(_authorizationProcessPanelPrefab);
            
            _authorizationProcessPanel.Exit += OnExit;
        }
        
        //[Client]
        private void DestroyAuthorizationProcessPanel()
        {
            if (_authorizationProcessPanel == null)
            {
                return;
            }

            _authorizationProcessPanel.Exit -= OnExit;
            
            _authorizationProcessPanel.Destroy();
        }

        #endregion

        #region RespawnUI

        //[Client]
        private void InstantiateRespawnMenu()
        {
            if (_respawnMenu != null)
            {
                return;
            }
            
            _respawnMenu = Instantiate(_respawnMenuPrefab.gameObject).GetComponent<RespawnMenu>();
            _respawnMenu.DefaultRespawnAction += Respawn;
            _respawnMenu.RespawnAction += Respawn;
        }
        
        //[Client]
        private void DestroyRespawnMenu()
        {
            if (_respawnMenu == null)
            {
                return;
            }

            _respawnMenu.DefaultRespawnAction -= Respawn;
            _respawnMenu.RespawnAction -= Respawn;
            Destroy(_respawnMenu.gameObject);
        }
        
        [Client]
        private void UpdateRespawnMenu()
        {
            if (_respawnMenu == null)
            {
                return;
            }
            
            _respawnMenu.SetView(!HaveLocalPlayer);

            if (!HaveLocalPlayer)
            {
                _respawnMenu.DestroyRespawnButtons();

                RequestRespawnPoints();
            }
        }

        #endregion
        
        #region AuthorizationMessages
        
        [Client]
        private void LogIn()
        {
            var saveWriter = QuickSaveWriter.Create(_storedAuthorizationDataFileName);

            if (!saveWriter.Exists(_tokenKeyName))
            {
                saveWriter.Write(_tokenKeyName, Guid.NewGuid().ToString());
                saveWriter.Commit();
            }
            
            var saveReader = QuickSaveReader.Create(_storedAuthorizationDataFileName);

            var token = saveReader.Read<string>(_tokenKeyName);

            // if (FileAccess.Exists(_storedAuthorizationDataFileName, false))
            // {
            //     var saveReader = QuickSaveReader.Create(_storedAuthorizationDataFileName);
            //
            //     if (saveReader.Exists(NetworkService.singleton.networkAddress))
            //     {
            //         token = saveReader.Read<string>(NetworkService.singleton.networkAddress);
            //     }
            // }
            
            NetworkClient.Send(new LogInMsg
            {
                NickName = _settingsService.SettingsData.NickName,
                Token = token
            });
        }
        
        [Client]
        private void OnExit()
        {
            NetworkService.singleton.StopHost();
        }
        
        [Server]
        private void OnLogIn(NetworkConnectionToClient conn, LogInMsg message)
        {
            if (message.Token == string.Empty)
            {
                return;
            }
            
            if (TryGetUser(message.Token, out var user))
            {
                if (user.ConnectionToClient != null)
                {
                    return;
                }
                
                user.ConnectionToClient = conn;
                    
                if (user.IsNewUser)
                {
                    user.Player = InstantiatePlayer(user.UniqueId, message.NickName, NetworkService.singleton.GetStartPosition().position);
                }
                
                if (user.Player != null)
                {
                    NetworkServer.AddPlayerForConnection(conn, user.Player.gameObject);
                
                    var handlers = user.Player.GetComponents<IAuthorizationCallbacks>();
                
                    foreach (var h in handlers)
                    {
                        h.OnPlayerAuthorized();
                    }
                }
                    
                user.IsNewUser = false;

                conn.Send(new SuccessfulLogInMsg());
            }
            else
            {
                // if (_registeredUsersCountPerConnection.TryGetValue(conn, out var value))
                // {
                //     if (value >= _maxUsersCountPerConnection)
                //     {
                //         return;
                //     }
                // }

                //var newToken = Guid.NewGuid().ToString();
                
                _registeredUsers.Add(new User(message.Token));
                
                // if (_registeredUsersCountPerConnection.ContainsKey(conn))
                // {
                //     _registeredUsersCountPerConnection[conn]++;
                // }
                // else
                // {
                //     _registeredUsersCountPerConnection.Add(conn, 1);
                // }

                conn.Send(new LogInResponseMsg
                {
                    //Token = newToken
                });
            }
        }
        
        [Client]
        private void OnLogInResponse(LogInResponseMsg message)
        {
            // var saveWriter = QuickSaveWriter.Create(_storedAuthorizationDataFileName);
            //     
            // saveWriter.Write(NetworkService.singleton.networkAddress, message.Token);
            //
            // saveWriter.Commit();
            
            LogIn();
        }

        [Client]
        private void OnSuccessfulLogIn(SuccessfulLogInMsg message)
        {
            UpdateRespawnMenu();
                
            Invoke(nameof(DestroyAuthorizationProcessPanel), _authorizationProcessPanelDestroyDelay);
        }
        
        #endregion
        
        #region RespawnMessages

        [Client]
        private void RequestRespawnPoints()
        {
            NetworkClient.Send(new RespawnDataRequestMsg());
        }
        
        [Server]
        private void OnRespawnDataRequest(NetworkConnection conn, RespawnDataRequestMsg message)
        {
            if (TryGetUser(conn, out var user))
            {
                SendRespawnPoints(user.ConnectionToClient, user.RespawnPoints);
            }
        }
        
        [Server]
        private void SendRespawnPoints(NetworkConnection conn, List<RespawnPoint> respawnPoints)
        {
            if (conn == null)
            {
                return;
            }
            
            conn.Send(new RespawnDataResponseMsg
            {
                RespawnPoints = respawnPoints
            });
        }
        
        [Client]
        private void OnRespawnDataResponse(RespawnDataResponseMsg message)
        {
            _respawnMenu.Initialize(message.RespawnPoints);
        }
        
        [Client]
        private void Respawn()
        {
            NetworkClient.Send(new RespawnMsg
            {
                NickName = _settingsService.SettingsData.NickName,
                RespawnPointUniqueId = null
            });
        }

        [Client]
        private void Respawn(string uniqueId)
        {
            NetworkClient.Send(new RespawnMsg
            {
                NickName = _settingsService.SettingsData.NickName,
                RespawnPointUniqueId = uniqueId
            });
        }

        [Server]
        private void OnRespawn(NetworkConnectionToClient conn, RespawnMsg message)
        {
            if (TryGetUser(conn, out var user) && user.Player == null)
            {
                var respawnPosition = NetworkService.singleton.GetStartPosition().position;
                
                if (TryGetRespawnPoint(user, message.RespawnPointUniqueId, out var respawnPoint))
                {
                    if (respawnPoint.Cooldown == 0)
                    {
                        respawnPosition = respawnPoint.Position;
                        respawnPoint.Setup(respawnPoint.BaseCooldown);
                    }
                }

                var player = InstantiatePlayer(user.UniqueId, message.NickName, respawnPosition);
                
                NetworkServer.AddPlayerForConnection(conn, player.gameObject);
                RestorePlayerReference(player);
            }
        }
        
        [Server]
        public bool TryAddRespawnPoint(
            string ownerUniqueId, 
            string respawnPointName,
            Vector3 position, 
            float baseCooldown,
            out RespawnPoint output)
        {
            output = null;
            
            if (TryGetUserByUniqueId(ownerUniqueId, out var user))
            {
                output = new RespawnPoint(respawnPointName, position, baseCooldown);

                user.RespawnPoints.Add(output);
                    
                SendRespawnPoints(user.ConnectionToClient, user.RespawnPoints);
            }
            
            return output != null;
        }

        [Server]
        public void RemoveRespawnPoint(RespawnPoint respawnPoint)
        {
            if (TryGetRespawnPointOwner(respawnPoint, out var user))
            {
                if (user.RespawnPoints.Contains(respawnPoint))
                {
                    user.RespawnPoints.Remove(respawnPoint);
                    SendRespawnPoints(user.ConnectionToClient, user.RespawnPoints);
                }
            }
        }

        #endregion

        #region MapMessages

        [Client]
        public void RequestMapPlacemarks()
        {
            NetworkClient.Send(new MapPlacemarksRequestMsg());
        }
        
        [Server]
        private void OnMapPlacemarksRequest(NetworkConnection conn, MapPlacemarksRequestMsg message)
        {
            if (TryGetUser(conn, out var user))
            {
                SendMapPlacemarks(user.ConnectionToClient, user.MapPlacemarks);
            }
        }
        
        [Server]
        private void SendMapPlacemarks(NetworkConnection conn, List<MapPlacemark> placemarks)
        {
            if (conn == null)
            {
                return;
            }

            conn.Send(new MapPlacemarksResponseMsg()
            {
                Placemarks = placemarks
            });
        }
        
        [Client]
        private void OnMapPlacemarksResponse(MapPlacemarksResponseMsg message)
        {
            LocalMapPlacemarks = message.Placemarks;
        }

        [Server]
        public bool TryAddMapPlacemark(
            string ownerUniqueId,
            string placemarkName,
            string placemarkDescription,
            Vector3 worldPosition,
            string icon,
            Color? color,
            out MapPlacemark output)
        {
            output = null;
            
            if (TryGetUserByUniqueId(ownerUniqueId, out var user))
            {
                output = new MapPlacemark(
                    placemarkName, 
                    placemarkDescription,
                    worldPosition,
                    icon,
                    color
                );

                user.MapPlacemarks.Add(output);
                
                SendMapPlacemarks(user.ConnectionToClient, user.MapPlacemarks);
            }
            
            return output != null;
        }
        
        [Server]
        public bool TryAddMapPlacemark(
            NetworkConnection ownerConnection,
            string placemarkName,
            string placemarkDescription,
            Vector3 worldPosition,
            string icon,
            Color? color,
            out MapPlacemark output)
        {
            output = null;

            if (TryGetUser(ownerConnection, out var user))
            {
                TryAddMapPlacemark(
                    user.UniqueId,
                    placemarkName,
                    placemarkDescription,
                    worldPosition,
                    icon,
                    color,
                    out output);
            }
            
            return output != null;
        }
        
        [Server]
        public void RemoveMapPlacemark(MapPlacemark placemark)
        {
            if (TryGetMapPlacemarkOwner(placemark, out var user))
            {
                if (user.MapPlacemarks.Contains(placemark))
                {
                    user.MapPlacemarks.Remove(placemark);
                    
                    SendMapPlacemarks(user.ConnectionToClient, user.MapPlacemarks);
                }
            }
        }
        
        [Server]
        public void RemoveMapPlacemark(string placemarkUniqueId)
        {
            if (TryGetMapPlacemarkOwner(placemarkUniqueId, out var user))
            {
                if (TryGetMapPlacemark(user, placemarkUniqueId, out var placemark))
                {
                    user.MapPlacemarks.Remove(placemark);
                    
                    SendMapPlacemarks(user.ConnectionToClient, user.MapPlacemarks);
                }
            }
        }

        #endregion
        
        #region Callbacks
        
        private void OnLocalPlayerUpdate(bool value)
        {
            UpdateRespawnMenu();
        }

        private void OnClientDisconnected(NetworkConnectionToClient conn)
        {
            ResetConnection(conn);
        }
        
        #endregion
        
        #region Utilities
        
        private bool TryGetUser(string token, out User output)
        {
            output = _registeredUsers.FirstOrDefault(u => u.Token == token);

            return output != null;
        }

        private bool TryGetUserByUniqueId(string uniqueId, out User output)
        {
            output = _registeredUsers.FirstOrDefault(u => u.UniqueId == uniqueId);

            return output != null;
        }

        private bool TryGetUser(NetworkConnection conn, out User output)
        {
            output = _registeredUsers.FirstOrDefault(u => u.ConnectionToClient == conn);

            return output != null;
        }

        private bool TryGetRespawnPointOwner(RespawnPoint respawnPoint, out User output)
        {
            output = null;
            
            foreach (var u in from u in _registeredUsers from p in u.RespawnPoints where p == respawnPoint select u)
            {
                output = u;
                return output != null;
            }
            
            return output != null;
        }

        private bool TryGetRespawnPoint(User user, string respawnPointUniqueId, out RespawnPoint output)
        {
            output = user.RespawnPoints.FirstOrDefault(p => p.UniqueId == respawnPointUniqueId);

            return output != null;
        }
        
        private bool TryGetMapPlacemarkOwner(MapPlacemark placemark, out User output)
        {
            output = null;

            TryGetMapPlacemarkOwner(placemark.UniqueId, out output);
            
            return output != null;
        }
        
        private bool TryGetMapPlacemarkOwner(string placemarkUniqueId, out User output)
        {
            output = null;
            
            foreach (var u in from u in _registeredUsers from p in u.MapPlacemarks where p.UniqueId == placemarkUniqueId select u)
            {
                output = u;
                return output != null;
            }
            
            return output != null;
        }
        
        private bool TryGetMapPlacemark(User user, string placemarkUniqueId, out MapPlacemark output)
        {
            output = user.MapPlacemarks.FirstOrDefault(p => p.UniqueId == placemarkUniqueId);

            return output != null;
        }
        
        #endregion
    }
}
