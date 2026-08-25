using JetBrains.Annotations;
using SP.Runtime.Bootstrap.Services.SettingsService;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace SP.Runtime.Bootstrap
{
    [UsedImplicitly]
    public class BootstrapFlow : IStartable
    {
        public BootstrapFlow(LoadingService.LoadingService loadingService, SettingsService settingsService)
        {
            _loadingService = loadingService;
            _settingsService = settingsService;
        }
        
        private readonly LoadingService.LoadingService _loadingService;
        private readonly SettingsService _settingsService;
        
        public async void Start()
        {
            await _loadingService.BeginLoading(_settingsService);
            
            SceneManager.LoadScene(SceneUtility.GetBuildIndexByScenePath("Loading"));
        }
    }
}
