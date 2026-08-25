using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalColliderCondition
{
    [CreateAssetMenu(menuName = "ConditionSettings/InternalColliderConditionSettings")]
    public class InternalColliderConditionSettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private LayerMask _layerMask;
        public LayerMask LayerMask => _layerMask;
    }
}