using System.Collections.Generic;
using System.Linq;
using Mirror;
using SP.Runtime.Core.Systems.Building.Utilities;
using SP.Runtime.Core.Systems.ValueContainer;
using SP.Runtime.Core.Utilities;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Interaction
{
    public class InteractionSeeker: NetworkBehaviour
    {
        #region Structs
        
        public enum InteractionLayer
        {
            Default,
            Building
        }
        
        #endregion
        
        public event UnityAction<InteractionHandler> InvokeAction;
        public event UnityAction<InteractionHandler> DismissAction;

        [Header("Settings")]
        [SerializeField] private float _searchRadius = 2;
        [SerializeField] private float _searchRadiusForFloors = 3.65f;
        [SerializeField] private Vector3 _offset = Vector3.up;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private float _updateRate = 0.1f;

        private Vector3 Position => transform.position + _offset;

        private readonly InteractionLayerContainer _layerContainer = new();
        
        private readonly List<InteractionHandler> _invokedHandlers = new();
        public IReadOnlyList<InteractionHandler> InvokedHandlers => _invokedHandlers;
        
        private void Awake()
        {
            _layerContainer.Add(InteractionLayer.Default);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            InvokeRepeating(nameof(UpdateInteractionHandlers), _updateRate, _updateRate);
        }

        public override void OnStopLocalPlayer()
        {
            CancelInvoke(nameof(UpdateInteractionHandlers));

            CleanUp();
            
            base.OnStopLocalPlayer();
        }

        private void CleanUp()
        {
            for (var i = _invokedHandlers.Count - 1; i >= 0; i--)
            {
                DismissInteractionHandler(_invokedHandlers[i]);
            }
        }
        
        public Container<InteractionLayer>.Element AddLayer(InteractionLayer interactionLayer)
        {
            var output = _layerContainer.Add(interactionLayer);
            
            UpdateInteractionHandlers();

            return output;
        }
        
        public void RemoveLayer(Container<InteractionLayer>.Element interactionLayer)
        {
            _layerContainer.Remove(interactionLayer);
            
            UpdateInteractionHandlers();
        }
        
        private void UpdateInteractionHandlers()
        {
            var currentLayers = _layerContainer.GetUniqueEnumerable();
            
            var foundHandlers = PhysicUtils.GetBySphere<InteractionHandler>(
                Position,
                _searchRadiusForFloors,
                _layerMask,
                QueryTriggerInteraction.Ignore).Where(h => currentLayers.Contains(h.Object.InteractionLayer)).ToList();
            
            for (var i = foundHandlers.Count - 1; i >= 0; i--)
            {
                if (foundHandlers[i].Object.HandlerType == InteractionHandler.InteractionHandlerType.Default &&
                    Vector3.Distance(Position, foundHandlers[i].GetClosestPoint(Position)) > _searchRadius)
                {
                    foundHandlers.RemoveAt(i);
                    continue;
                }

                if (BuildUtils.Linecast(Position, foundHandlers[i].GetClosestPoint(Position), 0.01f, out var hitInfo, _layerMask,
                        QueryTriggerInteraction.Ignore) &&
                    (!ComponentUtils.TryGetComponentInParent<InteractionHandler>(hitInfo.transform, out var handler) ||
                     handler != foundHandlers[i].Object))
                {
                    foundHandlers.RemoveAt(i);
                    continue;
                }
                
                InvokeInteractionHandler(foundHandlers[i].Object);
            }
            
            for (var i = _invokedHandlers.Count - 1; i >= 0; i--)
            {
                if (_invokedHandlers[i] == null)
                {
                    _invokedHandlers.RemoveAt(i);
                    continue;
                }

                if (foundHandlers.All(h => h.Object != _invokedHandlers[i]))
                {
                    DismissInteractionHandler(_invokedHandlers[i]);
                }
            }
        }
        
        private void InvokeInteractionHandler(InteractionHandler handler)
        {
            if (_invokedHandlers.Contains(handler))
            {
                return;
            }
            
            handler.InvokeInteraction();
            
            handler.DestroyAction += OnInteractionHandlerDestroy;
            
            _invokedHandlers.Add(handler);
            
            InvokeAction?.Invoke(handler);
        }
        
        private void DismissInteractionHandler(InteractionHandler handler)
        {
            if (!_invokedHandlers.Contains(handler))
            {
                return;
            }
            
            handler.DestroyAction -= OnInteractionHandlerDestroy;
            
            handler.DismissInteraction();
            
            _invokedHandlers.Remove(handler);
            
            DismissAction?.Invoke(handler);
        }

        #region Callbacks
        
        private void OnInteractionHandlerDestroy(InteractionHandler handler)
        {
            DismissInteractionHandler(handler);
        }
        
        #endregion
    }
}
