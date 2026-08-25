using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalUnbuildableAreaCondition
{
    [Condition(
        "Internal Unbuildable Area Condition",
        "Check and denies the actions, if this piece entering in a UnbuildableArea component.",
        ConditionTarget.PieceBehaviour)]
    public class InternalUnbuildableAreaCondition : ConditionBehaviour
    {
        public override bool CheckForPlacement()
        {
            var areas = PhysicUtils.GetTypesBySphere<UnbuildableArea>(
                transform.position,
                0.01f,
                -1,
                QueryTriggerInteraction.Collide);
            
            return areas.Count == 0;
        }
    }
}