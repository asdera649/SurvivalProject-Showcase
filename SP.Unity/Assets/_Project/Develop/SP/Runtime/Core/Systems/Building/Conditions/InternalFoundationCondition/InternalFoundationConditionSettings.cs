using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalFoundationCondition
{
    [CreateAssetMenu(menuName = "ConditionSettings/InternalFoundationConditionSettings")]
    public class InternalFoundationConditionSettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private float _castDistance = 6;
        public float CastDistance => _castDistance;
        
        [SerializeField] private float _minAllowedDistanceToGround = 0.25f;
        public float MinAllowedDistanceToGround => _minAllowedDistanceToGround;

        [SerializeField] private LayerMask _layerMask;
        public LayerMask LayerMask => _layerMask;
    }
}