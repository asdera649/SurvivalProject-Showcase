using Mirror;
using SP.Runtime.Core.Objects;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    public class ThrowingItem : GunItem
    {
        [Header("Prefabs(ThrowingItem)")]
        [SerializeField] private PhysicalObject _throwingObjectPrefab;
        
        [Header("Settings(ThrowingItem)")]
        [SerializeField] private float _throwingPower = 10;
        [SerializeField] private Vector3 _throwingDirectionOffset = new (0, 0.5f, 0);
        [SerializeField] private bool _addRandomTorque;
        [SerializeField] private float _torqueStrength = 10f;
    
        private static readonly int _throw = Animator.StringToHash("Throw");

        protected override void OnShot(Vector3 direction)
        {
            if (Model != null)
            {
                Model.Use();
            }
            
            if (NetworkAnimator != null)
            {
                NetworkAnimator.animator.SetTrigger(_throw);
            }
            else
            {
                Debug.Log("Network animator is null!");
            }

            if (!isServer)
            {
                return;
            }

            direction += _throwingDirectionOffset;
        
            var quaternion = Quaternion.LookRotation(direction);
            
            var throwingObject = Instantiate(
                    _throwingObjectPrefab,
                    AimOriginPosition,
                    new Quaternion(0, quaternion.y, 0, quaternion.w));
        
            OnThrow(throwingObject);
            
            throwingObject.Rigidbody.AddForce(direction * _throwingPower, ForceMode.Impulse);

            if (_addRandomTorque)
            {
                throwingObject.Rigidbody.AddTorque(new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized * _torqueStrength, ForceMode.Impulse);
            }

            NetworkServer.Spawn(throwingObject.gameObject);
        }

        protected virtual void OnThrow(PhysicalObject throwingObject)
        { 
        
        }
    }
}
