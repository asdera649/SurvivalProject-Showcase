using SP.Runtime.Core.Services;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Destruction
{
    [RequireComponent(typeof(Rigidbody))]
    public class DebrisPiece : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _delay = 1;
        
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
        
        private void OnEnable()
        {
            Invoke(nameof(Destroy), _delay);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Destroy));
            
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }
        
        private void Destroy()
        {
            Loader.Instance.PoolService.ReleaseObject(this);
        }
    }
}