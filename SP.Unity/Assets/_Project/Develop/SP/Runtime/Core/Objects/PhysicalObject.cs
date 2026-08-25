using System.Linq;
using Mirror;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    [RequireComponent(typeof(Rigidbody), typeof(NetworkRigidbodyUnreliable))]
    public class PhysicalObject : NetworkBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _collisionOffset = 0.01f;
        protected float CollisionOffset => _collisionOffset;
        
        private Vector3 _lastPosition;
        
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

        private NetworkRigidbodyUnreliable _networkRigidbody;
        protected NetworkRigidbodyUnreliable NetworkRigidbody
        {
            get
            {
                if (_networkRigidbody == null)
                {
                    _networkRigidbody = GetComponent<NetworkRigidbodyUnreliable>();
                }
                
                return _networkRigidbody;
            }
        }
        
        private void Awake()
        {
            UpdateLastPosition();
        }
        
        private void FixedUpdate()
        {
            UpdateCustomCollision();
        }
        
        private void UpdateCustomCollision()
        {
            var hits = Raycast(transform.position);
            
            foreach (var h in hits)
            {
                if (ComponentUtils.TryGetComponentInParent(h.transform, out PhysicalObject physicalObject) &&
                    physicalObject == this)
                {
                    continue;
                }

                var raycastHit = h;
                
                raycastHit.point += (_lastPosition - transform.position).normalized * _collisionOffset;
                
                OnCollision(raycastHit);
                break;
            }

            UpdateLastPosition();
        }

        private void UpdateLastPosition()
        {
            _lastPosition = transform.position;
        }
        
        #region Callbacks
        
        protected virtual void OnCollision(RaycastHit hit)
        { 
    
        }
        
        #endregion
        
        #region Utilities

        private IOrderedEnumerable<RaycastHit> Raycast(Vector3 position)
        {
            var direction = (position - _lastPosition).normalized;
            var distance = Vector3.Distance(position, _lastPosition);
            
            return Physics.RaycastAll(
                _lastPosition,
                direction,
                distance,
                -1,
                QueryTriggerInteraction.Ignore).OrderBy(h => h.distance);
        }

        #endregion
    }
}
