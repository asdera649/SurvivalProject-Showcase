using SP.Runtime.Loading.UI.LoadingMenu;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Loading
{
    public class LoadingScope : LifetimeScope
    {
        [Header("References")] 
        [SerializeField] private LoadingMenu _loadingMenu;
        
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_loadingMenu);
            builder.RegisterEntryPoint<LoadingFlow>();
        }
    }
}
