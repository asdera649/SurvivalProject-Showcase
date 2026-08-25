using SP.Runtime.Core.Services;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Builder.PlacementConditions
{
    public class DistancePlacementCondition : BasePlacementCondition
    {
        public DistancePlacementCondition(Transform ownerTransform)
        {
            _ownerTransform = ownerTransform;
        }

        private readonly Transform _ownerTransform;
        
        public override bool CanPlace(Vector3 placePosition)
        {
            if (Vector3.Distance(
                    _ownerTransform.position, placePosition) > Loader.Instance.BuildingService.MaxBuildingDistance)
            {
                return false;
            }

            return true;
        }
    }
}