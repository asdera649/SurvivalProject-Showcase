using FMODUnity;
using SP.Runtime.Core.Entities;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Environment
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Bush : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private StudioEventEmitter _leavesRustlingEventEmitter;

        [Header("Settings")] 
        [SerializeField] private float _delayBetweenLeavesRustling = 1;

        private float _time;
        
        private Rigidbody _rigidbody;
        private Rigidbody Rigidbody
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
        
        private SphereCollider _sphereCollider;
        private SphereCollider SphereCollider
        {
            get
            {
                if (_sphereCollider == null)
                {
                    _sphereCollider = GetComponent<SphereCollider>();
                }

                return _sphereCollider;
            }
        }

        public void OnValidate()
        {
            var ignoreRaycastLayer = LayerUtils.GetIgnoreRaycastLayer();
            
            Rigidbody.isKinematic = true;
            
            if (SphereCollider.gameObject.layer != ignoreRaycastLayer)
            {
                SphereCollider.gameObject.layer = ignoreRaycastLayer;
            }
            
            SphereCollider.isTrigger = true;
        }

        private void Update()
        {
            _time += Time.deltaTime;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            PlayerLeavesRustling(other);
        }
        
        private void OnTriggerExit(Collider other)
        {
            PlayerLeavesRustling(other);
        }
        
        private void PlayerLeavesRustling(Component other)
        {
            if (_time <= _delayBetweenLeavesRustling)
            {
                return;
            }
            
            if (ComponentUtils.TryGetComponentInParent<Character>(other, out _))
            {
                _leavesRustlingEventEmitter.Play();

                _time = 0;
            }
        }
    }
}