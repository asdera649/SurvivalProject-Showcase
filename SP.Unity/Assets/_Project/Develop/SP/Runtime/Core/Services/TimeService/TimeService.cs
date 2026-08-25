using Mirror;
using SP.Runtime.Core.Services.SaveService;
using UnityEngine;

namespace SP.Runtime.Core.Services.TimeService
{
    [RequireComponent(typeof(SaveHandler))]
    public class TimeService : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _updateRate = 1;
        
        [SaveHandler.Saved] 
        [SyncVar] private float _time;
        public float Time => _time;
        
        [SaveHandler.Saved] 
        [SyncVar] private int _currentDay = 1;
        public int CurrentDay => _currentDay;
        
        private const int _secondsInOneDay = 86400;
        
        private float _lastTime;
        
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
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            InvokeRepeating(nameof(UpdateTime), _updateRate, _updateRate);
            
            _lastTime = UnityEngine.Time.time;
        }

        public override void OnStopServer()
        {
            CancelInvoke(nameof(UpdateTime));
            
            base.OnStopServer();
        }
        
        [ServerCallback]
        private void UpdateTime()
        {
            _time += UnityEngine.Time.time - _lastTime;
            
            _currentDay = Mathf.CeilToInt(_time / _secondsInOneDay);
            
            _lastTime = UnityEngine.Time.time;
        }
        
        [ServerCallback]
        public void SetTime(float value)
        {
            _time = value;

            UpdateTime();
        }
    }
}
