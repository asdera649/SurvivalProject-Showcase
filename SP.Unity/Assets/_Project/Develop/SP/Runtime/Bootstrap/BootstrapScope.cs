using SP.Runtime.Bootstrap.Services.BackendInteractionService;
using SP.Runtime.Bootstrap.Services.SettingsService;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Bootstrap
{
    public class BootstrapScope : LifetimeScope
    {
        [Space(10)]
        
        [Header("References")]
        [SerializeField] private SettingsService _settingsService;

        [Header("Settings")] 
        [SerializeField] private bool _disableVersionChecking;
        [SerializeField] private string _backendUrl;
        
        protected override void Awake()
        {
            DontDestroyOnLoad(this);
            
            base.Awake();
        }
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<LoadingService.LoadingService>(Lifetime.Scoped);

            var backend = new BackendInteractionService(_backendUrl, _disableVersionChecking);

            builder.RegisterInstance(backend);
            
            builder.RegisterComponent(_settingsService).AsSelf();

            builder.RegisterEntryPoint<BootstrapFlow>();
        }
    }
}
