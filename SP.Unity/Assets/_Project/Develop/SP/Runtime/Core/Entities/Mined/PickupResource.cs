using FMODUnity;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Entities.Mined
{
    [RequireComponent(typeof(InteractionHandler))]
    public class PickupResource : NetworkBehaviour
    {
        public event UnityAction<PickupResource> DestroyAction;

        [Header("References")] 
        [SerializeField] private StudioEventEmitter _pickupEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private BaseItem _receivedItem;
        [SerializeField] private int _receivedQuantity = 1;

        private InteractionHandler _interactionHandler;
        private InteractionHandler InteractionHandler
        {
            get
            {
                if (_interactionHandler == null)
                {
                    _interactionHandler = GetComponent<InteractionHandler>();
                }

                return _interactionHandler;
            }
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            InteractionHandler.ExecutionAction += OnExecution;
        }

        public override void OnStopClient()
        {
            InteractionHandler.ExecutionAction -= OnExecution;
            
            base.OnStopClient();
        }

        private void OnDestroy()
        {
            DestroyAction?.Invoke(this);
        }

        [ClientCallback]
        private void OnExecution()
        {
            _pickupEventEmitter.Play();
            
            CmdPickup();
        }

        [Command(requiresAuthority = false)]
        private void CmdPickup(NetworkConnectionToClient sender = null)
        {
            if (sender == null ||
                !sender.identity.TryGetComponent(out Systems.Inventory.Inventory targetInventory))
            {
                return;
            }

            InstantiateItem(targetInventory);
        }

        [ServerCallback]
        private void InstantiateItem(Systems.Inventory.Inventory targetInventory)
        {
            var temp = BaseItem.Instantiate(_receivedItem);
            temp.Quantity = _receivedQuantity;
            
            targetInventory.Add(temp);
            
            NetworkServer.Destroy(gameObject);
        }
    }
}
