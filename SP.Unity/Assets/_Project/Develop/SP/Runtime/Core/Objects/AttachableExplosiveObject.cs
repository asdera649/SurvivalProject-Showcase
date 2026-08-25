using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Objects
{
    public class AttachableExplosiveObject : TimedExplosiveObject
    {
        private const string _dummyName = "Dummy";
        
        private bool _isAttached;
        
        private GameObject _dummy;

        private void OnDestroy()
        {
            DestroyDummy();
        }
        
        private void LateUpdate()
        {
            UpdateAttached();
        }

        private void UpdateAttached()
        {
            if (!_isAttached)
            {
                return;
            }
            
            if (_dummy != null)
            {
                transform.SetPositionAndRotation(_dummy.transform.position, _dummy.transform.rotation);
            }
            else
            {
                Destroy();
            }
        }

        [ServerCallback]
        private void Destroy()
        {
            NetworkServer.Destroy(gameObject);
        }

        protected override void OnCollision(RaycastHit hit)
        { 
            base.OnCollision(hit);
            
            if (ComponentUtils.TryGetComponentInParent<BaseEntity>(hit.transform, out var entity) &&
                CheckOwner(entity))
            {
                return;
            }
            
            SetAttached(hit);
        }
        
        private void SetAttached(RaycastHit hit)
        {
            if (_isAttached)
            {
                return;
            }
            
            SetActivePhysics(false);
            
            InstantiateDummy(hit.point, GetRotationByDirection(hit.normal), hit.transform);
            
            _isAttached = true;
        }

        [ServerCallback]
        protected override void OnDelayCompleted()
        {
            Explode(default, _dummy != null ? _dummy.GetComponentInParent<BuildingEntity>() : null);
        }

        #region Dummy
        
        private void InstantiateDummy(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (_dummy != null)
            {
                return;
            }

            _dummy = new GameObject(_dummyName);
            _dummy.transform.SetPositionAndRotation(position, rotation);
            _dummy.transform.SetParent(parent);
        }

        private void DestroyDummy()
        {
            if (_dummy == null)
            {
                return;
            }
            
            Destroy(_dummy);
        }
        
        private void SetActivePhysics(bool value)
        {
            Rigidbody.isKinematic = !value;

            var colliders = GetComponentsInChildren<Collider>();
            
            foreach (var c in colliders)
            {
                c.enabled = value;
            }
        }

        private Quaternion GetRotationByDirection(Vector3 normal)
        {
            var forward = Vector3.Cross(Vector3.Cross(normal, Vector3.up), normal);
            
            if (forward == Vector3.zero || normal == Vector3.zero)
            {
                return Quaternion.identity;
            }
            
            return Quaternion.LookRotation(forward, normal);
        }
        
        #endregion
    }
}
