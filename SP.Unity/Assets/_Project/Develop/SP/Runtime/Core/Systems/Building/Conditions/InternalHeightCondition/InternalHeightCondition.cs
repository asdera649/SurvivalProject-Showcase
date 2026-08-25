using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalHeightCondition
{
    [Condition(
        "Internal Height Condition",
        "Check and denies the actions, if this piece exceeds the maximum height of the construction.",
        ConditionTarget.PieceBehaviour)]
    public class InternalHeightCondition : ConditionBehaviour
    {
        [Header("Settings")]
        [SerializeField] private InternalHeightConditionSettings _conditionSettings;
        
        public override bool CheckForPlacement()
        {
            return transform.position.y <= _conditionSettings.MaxHeightInWorld;
        }
    }
}