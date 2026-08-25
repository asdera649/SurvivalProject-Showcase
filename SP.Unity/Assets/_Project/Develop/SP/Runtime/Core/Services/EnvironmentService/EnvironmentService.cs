using FMODUnity;
using Mirror;
using SP.Runtime.Core.Systems;
using SP.Runtime.Utilities;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Services.EnvironmentService
{
    public class EnvironmentService : NetworkBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private EnvironmentSettings _environmentSettings;
        [SerializeField] private float _secondsInFullDay = 1800;
        [SerializeField] private float _updateRate = 0.1f;

        [Header("References")]
        [SerializeField] private StudioEventEmitter[] _forestEventEmitters;
        
        [SyncVar(hook = nameof(OnCurrentTimeUpdate))] 
        private float _currentTime = 0.09f; 
        private float CurrentTime
        {
            get => _currentTime;
            set => _currentTime = Mathf.Clamp01(value);
        }

        private const string _hourParameter = "Hour";

        private EnvironmentHandler _environmentHandler;

        private PostProcessingHandler _postProcessingHandler;
        private TimeService.TimeService _timeService;

        [Inject]
        private void Inject(PostProcessingHandler postProcessingHandler, TimeService.TimeService timeService)
        {
            _postProcessingHandler = postProcessingHandler;
            _timeService = timeService;
        }
        
        private void Awake()
        {
            _environmentHandler = new EnvironmentHandler(_environmentSettings);
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            InvokeRepeating(nameof(UpdateTime), _updateRate, _updateRate);
        }

        public override void OnStopServer()
        {
            CancelInvoke(nameof(UpdateTime));
            
            base.OnStopServer();
        }

        [ServerCallback]
        private void UpdateTime()
        {
            var currentDay = _timeService.Time / _secondsInFullDay;

            CurrentTime = 1 - (Mathf.CeilToInt(currentDay) - currentDay);
        }

        #region Callbacks

        [ClientCallback]
        private void OnCurrentTimeUpdate(float oldValue, float newValue)
        {
            var directionalLight = Loader.Instance.DirectionalLight;
            
            _environmentHandler.Apply(
                newValue,
                ref directionalLight,
                ref _postProcessingHandler);

            foreach (var f in _forestEventEmitters)
            {
                f.SetParameter(
                    _hourParameter,
                    EnumUtils.GetValueIndex(_environmentSettings.GetTimeOfDay(newValue)));
            }
        }

        #endregion
    }
}
