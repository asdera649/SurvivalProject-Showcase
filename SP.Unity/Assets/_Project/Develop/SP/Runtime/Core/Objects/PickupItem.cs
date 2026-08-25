using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.Water.BuoyancyObject;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    [RequireComponent(typeof(BuoyancyObject), typeof(Rigidbody), typeof(NetworkRigidbodyUnreliable))]
    public class PickupItem : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _pickupCooldown = 1.5f;
        
        private float _cooldown;
        
        private BaseItem _receivedItem;
        
        private Rigidbody _rigidbody;
        public Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody>();
                }

                return _rigidbody;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            Invoke(nameof(DestroyPickupItem), _receivedItem.DespawnTime);
        }
        
        public override void OnStopServer()
        {
            CancelInvoke(nameof(DestroyPickupItem));
            
            base.OnStopServer();
        }

        [ServerCallback]
        public void Initialize(BaseItem item)
        {
            _receivedItem = item;
            _cooldown = _pickupCooldown;
        }

        private void Update()
        {
            UpdateCooldown();
        }
        
        private void UpdateCooldown()
        {
            _cooldown -= Time.deltaTime;
        }

        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            if (!ComponentUtils.TryGetComponentInParent(other.transform, out Character character))
            {
                return;
            }
            
            Pickup(character);
        }

        [ServerCallback]
        private void Pickup(Character character)
        {
            if (!CanPickup(character))
            {
                return;
            }

            character.Inventory.Add(_receivedItem);
            
            _receivedItem = null;

            DestroyPickupItem();
        }

        [ServerCallback]
        private void DestroyPickupItem()
        {
            NetworkServer.Destroy(gameObject);
        }

        #region Utilities

        private bool CanPickup(Character character)
        {
            return _cooldown <= 0 && character.Inventory.CanAdd(_receivedItem);
        }

        #endregion
    }
}
