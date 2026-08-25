using System.Collections;
using Mirror;
using SP.Runtime.Meta.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Meta.Services.NetworkService
{
    public class NetworkService : NetworkManager
    {
        public event UnityAction ServerStarted; 
        public event UnityAction<NetworkConnectionToClient> ClientDisconnected;
        
        [Header("Prefabs")] 
        [SerializeField] private ServerLoadingMenu _serverLoadingMenuPrefab;     
        
        public new static NetworkService singleton => (NetworkService)NetworkManager.singleton;
        
        private ServerLoadingMenu _serverLoadingMenu;

        private int _backupMaxConnections;

        private bool _isNewConnectionsLocked;
        
        // Способ временно запретить новые подключения к серверу, возможно не самый лучший способ.
        public void SetLockNewConnections(bool value)
        {
            if (!NetworkServer.active)
            {
                Debug.LogError("Can not SetLockNewConnections() because NetworkServer is not active");
                return;
            }

            if (_isNewConnectionsLocked == value)
            {
                Debug.LogWarning("Can not SetLockNewConnections() because SetLockNewConnections() " +
                                 "is already active/inactive");
                return;
            }
            
            if (value)
            {
                _backupMaxConnections = NetworkServer.maxConnections;
                NetworkServer.maxConnections = 0;
            }
            else
            {
                if (NetworkServer.maxConnections != 0)
                {
                    Debug.LogWarning("While locking new connections, “maxConnections” has been changed, this should not happen");
                }
                
                NetworkServer.maxConnections = _backupMaxConnections;
            }
            
            _isNewConnectionsLocked = value;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            ServerStarted?.Invoke();
        }
        
        public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandler)
        {
            InstantiateServerLoadingMenu();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            ClientDisconnected?.Invoke(conn);
        }

        #region ServerLoadingUI

        private void InstantiateServerLoadingMenu()
        {
            if (_serverLoadingMenu != null)
            {
                return;
            }
            
            _serverLoadingMenu = Instantiate(_serverLoadingMenuPrefab.gameObject).GetComponent<ServerLoadingMenu>();
            
            StartCoroutine(nameof(LoadingProgress));
        }
        
        private void DestroyServerLoadingMenu()
        {
            if (_serverLoadingMenu == null)
            {
                return;
            }
            
            Destroy(_serverLoadingMenu.gameObject);
        }

        private IEnumerator LoadingProgress()
        {
            yield return new WaitForSeconds(1f);

            while (loadingSceneAsync is { isDone: false })
            {
                _serverLoadingMenu.Progress = loadingSceneAsync.progress;
                yield return new WaitForEndOfFrame();
            }
            
            DestroyServerLoadingMenu();

            yield return null;
        }

        #endregion
    }
}