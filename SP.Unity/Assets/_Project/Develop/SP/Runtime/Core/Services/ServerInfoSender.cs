using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SP.Runtime.Bootstrap.Services.BackendInteractionService;
using SP.Runtime.LoadingService;
using SP.Runtime.Meta.Services.NetworkService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Core.Services
{
    [UsedImplicitly]
    public class ServerInfoSender : ILoadUnit, IStartable, ITickable
    {
        private const float _sendRate = 30;

        private bool _isSending;
        
        private float _currentTime;

        private bool _isInitialized;
        
        private bool _isLogSuccessful = true;
        
        private bool _isLogError = true;
        
        private BackendInteractionService _backendInteractionService;
        
        [Inject]
        private void Inject(BackendInteractionService backend)
        {
            _backendInteractionService = backend;
        }
        
        public UniTask Load()
        {
            _currentTime = _sendRate;

            _isInitialized = true;

            return UniTask.CompletedTask;
        }
        
        public void Start()
        {
            
        }
        
        public async void Tick()
        {
            if (!_isInitialized)
            {
                return;
            }
            
            if (_isSending)
            {
                return;
            }
            
            _currentTime += Time.deltaTime;

            if (_currentTime >= _sendRate)
            {
                await Send();

                _currentTime = 0;
            }
        }

        private async Task Send()
        {
            if (!_isInitialized)
            {
                return;
            }
            
            _isSending = true;

            if (_isLogSuccessful && _isLogError)
            {
                Debug.Log("[ServerInfoSender]: Sending server information...");
            }

            var serverState = new BackendInteractionService.ServerState
            {
                MaxPlayersCount = NetworkService.singleton.maxConnections,
                CurrentPlayersCount = NetworkService.singleton.numPlayers
            };

            var result = await _backendInteractionService.TrySendServerInfo(serverState);
            
            if (result)
            {
                if (_isLogSuccessful)
                {
                    Debug.Log("[ServerInfoSender]: Successful sending! " +
                           "Let's continue sending, with no log output.... If something goes wrong, we'll display a message.");
                }

                _isLogSuccessful = false;
                _isLogError = true;
            }
            else
            {
                if (_isLogError)
                {
                    Debug.LogWarning("[ServerInfoSender]: Sending failed! " +
                                  "Let's keep trying, in case of success a message will be displayed...");
                }

                _isLogSuccessful = true;
                _isLogError = false;
            }

            _isSending = false;
        }
    }
}