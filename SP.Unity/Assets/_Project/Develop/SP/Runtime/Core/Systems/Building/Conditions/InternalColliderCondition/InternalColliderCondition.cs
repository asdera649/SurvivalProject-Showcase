using System.Linq;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalColliderCondition
{
    [Condition(
        "Internal Collider Condition",
        "Check and denies the actions, if the collider of this piece entering in collision with others collider.",
        ConditionTarget.PieceBehaviour)]
    public class InternalColliderCondition : ConditionBehaviour
    {
        [Header("Settings")]
        [SerializeField] private InternalColliderConditionSettings _conditionSettings;
        
        public override bool CheckForPlacement()
        {
            var colliders = PhysicUtils.GetTypesByBox<Collider>(
                Piece.MeshBoundsToWorld.center,
                Piece.MeshBoundsToWorld.extents,
                transform.rotation,
                _conditionSettings.LayerMask,
                QueryTriggerInteraction.Ignore);

            return colliders.All(c => c.transform.IsChildOf(transform));
        }
    }
}
