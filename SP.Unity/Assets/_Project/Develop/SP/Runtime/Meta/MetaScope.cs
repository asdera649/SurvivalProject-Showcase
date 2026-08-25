using SP.Runtime.Meta.UI.MainMenu.Blocks.ServerListBlock;
using SP.Runtime.Meta.UI.MainMenu.Blocks.SettingsBlock;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Meta
{
    public class MetaScope : LifetimeScope
    {
        [Space(10)]
        
        [SerializeField] private ServerListBlock _serverListBlock;
        [SerializeField] private SettingsBlock _settingsBlock;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_serverListBlock);
            builder.RegisterComponent(_settingsBlock);
            
            builder.RegisterEntryPoint<MetaFlow>();
        }
    }
}
