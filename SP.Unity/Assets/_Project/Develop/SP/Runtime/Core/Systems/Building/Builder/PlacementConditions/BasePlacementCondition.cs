using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Builder.PlacementConditions
{
    public class BasePlacementCondition
    {
        public virtual bool CanPlace(Vector3 placePosition)
        {
            return true;
        }
    }
}