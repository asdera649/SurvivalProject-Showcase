using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SP.Runtime.Bootstrap.Services.BackendInteractionService;
using SP.Runtime.Loading.UI.LoadingMenu;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace SP.Runtime.Loading
{
    [UsedImplicitly]
    public class LoadingFlow : IStartable
    {
        public LoadingFlow(BackendInteractionService backendInteractionService, LoadingMenu loadingMenu)
        {
            _backendInteractionService = backendInteractionService;
            _loadingMenu = loadingMenu;
        }
        
        private readonly BackendInteractionService _backendInteractionService;
        private readonly LoadingMenu _loadingMenu;
        
        public async void Start()
        {
            await UniTask.Delay(500);

            var result = await _backendInteractionService.CheckVersion();

            if (result != BackendInteractionService.RequestResult.Successful)
            {
                Debug.LogWarning("The game version is outdated, further loading is suspended!");
            }
            
            switch (result)
            {
                case BackendInteractionService.RequestResult.Successful:
                {
                    SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath("Meta"));
                    break;
                }
                case BackendInteractionService.RequestResult.Obsolete:
                {
                    _loadingMenu.ShowFailBlock(LoadingMenuFailBlock.FailType.Obsolete);
                    break;
                }
                case BackendInteractionService.RequestResult.NoConnection:
                {
                    _loadingMenu.ShowFailBlock(LoadingMenuFailBlock.FailType.NoConnection);
                    break;
                }
                case BackendInteractionService.RequestResult.UnknownError:
                {
                    _loadingMenu.ShowFailBlock(LoadingMenuFailBlock.FailType.UnknownError);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
