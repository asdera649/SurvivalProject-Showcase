using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;

namespace SP.Runtime.Core.Systems.Building.Conditions
{
    [Condition(
        "Internal Water Condition",
        "Check and denies the actions, if the piece is in the water.",
        ConditionTarget.PieceBehaviour)]
    public class InternalWaterCondition : ConditionBehaviour
    {
        public override bool CheckForPlacement()
        {
            return !Water.Water.TryGetDistanceToWaterLevel(transform.position, out _);
        }
    }
}