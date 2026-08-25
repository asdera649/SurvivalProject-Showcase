using SP.Runtime.Core.Services;
using SP.Runtime.Core.UI.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Interaction
{
    public class InteractionHandler : MonoBehaviour
    {
        #region Structs
        
        public enum InteractionHandlerType
        {
            Default,
            Floor
        }
        
        #endregion
        
        public event UnityAction ExecutionAction;
        public event UnityAction<InteractionHandler> DestroyAction;

        [Header("Prefabs")]
        [SerializeField] private InteractionButton _interactionButtonPrefab;

        [Header("Settings")] 
        [SerializeField] private Sprite _icon;
        [SerializeField] private bool _holdMode;
        public bool HoldMode
        {
            get => _holdMode;
            set
            {
                _holdMode = value;
                
                if (_interactionButton != null)
                {
                    _interactionButton.Initialize(_icon, _holdMode);
                }
            }
        }

        [Space(10)]
        
        [SerializeField] private Vector3 _offset;
        [SerializeField] private Transform _customOffset;
        
        [Space(10)]
        
        [SerializeField] private InteractionHandlerType _handlerType;
        public InteractionHandlerType HandlerType => _handlerType;
        
        [SerializeField] private InteractionSeeker.InteractionLayer _interactionLayer;
        public InteractionSeeker.InteractionLayer InteractionLayer => _interactionLayer;

        private Vector3 Position
        {
            get
            {
                if (_customOffset != null)
                {
                    return _customOffset.transform.position;
                }

                return transform.position + _offset;
            }
        }
        
        private InteractionButton _interactionButton;
        public InteractionButton InteractionButton => _interactionButton;

        private void OnDestroy()
        {
            DismissInteraction();
            
            DestroyAction?.Invoke(this);
        }

        private void Update()
        {
            UpdateInteractionButton();
        }

        private void UpdateInteractionButton()
        {
            if (_interactionButton == null)
            {
                return;
            }

            _interactionButton.transform.position = GetInteractionButtonScreenPosition();
        }

        public void InvokeInteraction()
        {
            if (_interactionButton != null)
            {
                return;
            }
            
            _interactionButton = Instantiate(
                _interactionButtonPrefab,
                GetInteractionButtonScreenPosition(),
                Quaternion.identity,
                Loader.Instance.Canvas.transform);
            
            _interactionButton.Initialize(_icon, _holdMode);
            
            _interactionButton.Clicked += OnClick;
        }

        public void DismissInteraction()
        {
            if (_interactionButton == null)
            {
                return;
            }
            
            _interactionButton.Clicked -= OnClick;
            Destroy(_interactionButton.gameObject);
        }

        private Vector3 GetInteractionButtonScreenPosition()
        {
            return RectTransformUtility.WorldToScreenPoint(Loader.Instance.MainCamera.Camera, Position);
        }

        #region Callbacks
        
        private void OnClick()
        {
            ExecutionAction?.Invoke();
        }
        
        #endregion
    }
}
