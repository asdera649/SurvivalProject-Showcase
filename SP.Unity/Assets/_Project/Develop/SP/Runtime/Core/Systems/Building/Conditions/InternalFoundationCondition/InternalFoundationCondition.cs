using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalFoundationCondition
{
    [Condition(
        "Internal Foundation Condition",
        "A separate verification condition for foundations.",
        ConditionTarget.PieceBehaviour)]
    public class InternalFoundationCondition : ConditionBehaviour
    {
        [Header("Settings")]
        [SerializeField] private InternalFoundationConditionSettings _conditionSettings;
        
        public override bool CheckForPlacement()
        {
            var isEmpty = true;
            var minDistanceToGround = float.MaxValue;
            
            var hits = Physics.BoxCastAll(
                Piece.MeshBoundsToWorld.center,
                Piece.MeshBoundsToWorld.extents,
                -transform.up,
                transform.rotation,
                _conditionSettings.CastDistance,
                _conditionSettings.LayerMask,
                QueryTriggerInteraction.Ignore);
            
            foreach (var h in hits)
            {
                if (!h.transform.IsChildOf(transform))
                {
                    isEmpty = false;
                    
                    if (h.distance < minDistanceToGround)
                    {
                        minDistanceToGround = h.distance;
                    }
                    
                    if (h.transform.gameObject.layer != LayerUtils.GetGroundLayer())
                    {
                        return false;
                    }
                }
            }

            if (minDistanceToGround < _conditionSettings.MinAllowedDistanceToGround)
            {
                return false;
            }

            return !isEmpty;
        }
    }
}
