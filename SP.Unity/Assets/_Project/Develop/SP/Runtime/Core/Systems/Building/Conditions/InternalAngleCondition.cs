using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions
{
    [Condition(
        "Internal Angle Condition",
        "Checks the angle of the piece.",
        ConditionTarget.PieceBehaviour)]
    public class InternalAngleCondition : ConditionBehaviour
    {
        private const float _maxAngle = 60;
        
        public override bool CheckForPlacement()
        {
            return CheckAngle(transform.eulerAngles);
        }
        
        private bool CheckAngle(Vector3 euler)
        {
            var defaultRotation = Quaternion.identity;
            var currentRotation = Quaternion.Euler(new Vector3(euler.x, 0, euler.z));;

            var angle = Quaternion.Angle(defaultRotation, currentRotation);

            return angle < _maxAngle;
        }
    }
}