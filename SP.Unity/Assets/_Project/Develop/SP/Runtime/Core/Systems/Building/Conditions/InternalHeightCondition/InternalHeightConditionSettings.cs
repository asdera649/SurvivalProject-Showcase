using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalHeightCondition
{
    [CreateAssetMenu(menuName = "ConditionSettings/InternalHeightConditionSettings")]
    public class InternalHeightConditionSettings : ScriptableObject
    {
        [Header("Settings")] 
        [SerializeField] private float _maxHeightInWorld = 30;
        public float MaxHeightInWorld => _maxHeightInWorld;
    }
}