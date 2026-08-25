using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalUnbuildableAreaCondition
{
    [RequireComponent(typeof(SphereCollider))]
    public class UnbuildableArea : MonoBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private float _radius = 10;
        
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

        private void OnValidate()
        {
            var ignoreRaycastLayer = LayerUtils.GetIgnoreRaycastLayer();

            if (SphereCollider.gameObject.layer != ignoreRaycastLayer)
            {
                SphereCollider.gameObject.layer = ignoreRaycastLayer;
            }
            
            SphereCollider.isTrigger = true;
            SphereCollider.center = Vector3.zero;
            SphereCollider.radius = _radius;
        }
    }
}