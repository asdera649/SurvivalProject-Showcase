using Mirror;
using SP.Runtime.Core.Entities.Buildings;
using SP.Runtime.Core.Systems.Building.Utilities;
using SP.Runtime.Core.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Craft
{
    public class WorkbenchLevelHandler : NetworkBehaviour
    {
        public event UnityAction WorkbenchLevelUpdateAction;
        
        [SerializeField] private Vector3 _offset = Vector3.up;
        [SerializeField] private float _updateRate = 0.5f;

        private Vector3 Position => transform.position + _offset;
        
        private int _currentWorkbenchLevel;
        public int CurrentWorkbenchLevel
        {
            get => _currentWorkbenchLevel;
            private set
            {
                var oldValue = _currentWorkbenchLevel;
                _currentWorkbenchLevel = value;

                if (_currentWorkbenchLevel != oldValue)
                {
                    WorkbenchLevelUpdateAction?.Invoke();
                }
            }
        }

        private bool _isInvoked;

        public override void OnStartServer()
        {
            base.OnStartServer();

            Invoke();
        }

        public override void OnStopServer()
        {
            CancelInvoke(nameof(UpdateWorkbenchLevel));
            
            base.OnStopServer();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            Invoke();
        }

        public override void OnStopLocalPlayer()
        {
            CancelInvoke(nameof(UpdateWorkbenchLevel));
            
            base.OnStopLocalPlayer();
        }

        private void Invoke()
        {
            if (_isInvoked)
            {
                return;
            }
            
            InvokeRepeating(nameof(UpdateWorkbenchLevel), _updateRate, _updateRate);

            _isInvoked = true;
        }
        
        private void UpdateWorkbenchLevel()
        {
            var currentLevel = 0;
            
            var results = PhysicUtils.GetBySphere<BaseWorkbenchEntity>(Position, 0.01f, -1, QueryTriggerInteraction.Collide);

            foreach (var r in results)
            {
                if (!BuildUtils.Linecast(Position, r.Bounds.center, out _, -1, QueryTriggerInteraction.Ignore))
                {
                    if (r.Object.WorkbenchLevel > currentLevel)
                    {
                        currentLevel = r.Object.WorkbenchLevel;
                    }
                }
            }

            CurrentWorkbenchLevel = currentLevel;
        }
    }
}