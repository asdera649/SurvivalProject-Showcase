using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Services
{
    public class Loader : Singleton<Loader>
    {
        [Header("Prefabs")]
        [SerializeField] private MainCamera.MainCamera _PCMainCameraPrefab;
        [SerializeField] private MainCamera.MainCamera _mobileMainCameraPrefab;
        [SerializeField] private DirectionalLight _directionalLightPrefab;
        [SerializeField] private Canvas _canvasPrefab;

        [Header("References")] 
        [SerializeField] private BuildingService.BuildingService _buildingService;
        public BuildingService.BuildingService BuildingService => _buildingService;
        
        [SerializeField] private PoolService.PoolService _poolService;
        public PoolService.PoolService PoolService => _poolService;

        public MainCamera.MainCamera MainCamera { get; private set; }

        public DirectionalLight DirectionalLight { get; private set; }

        public Canvas Canvas { get; private set; }

        private void Awake()
        {
            MainCamera = InstantiateMainCamera();
            DirectionalLight = Instantiate(_directionalLightPrefab);
            Canvas = Instantiate(_canvasPrefab);
        }
        
        private MainCamera.MainCamera InstantiateMainCamera()
        {
            MainCamera.MainCamera output = null;
            
            var runtimePlatform = Application.platform;

            if (runtimePlatform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor)
            {
                output = Instantiate(_PCMainCameraPrefab);
            }
            else if (runtimePlatform == RuntimePlatform.Android)
            {
                output = Instantiate(_mobileMainCameraPrefab);
            }

            if (output == null)
            {
                output = Instantiate(_PCMainCameraPrefab);
            }
 
            return output;
        }
    }
}
