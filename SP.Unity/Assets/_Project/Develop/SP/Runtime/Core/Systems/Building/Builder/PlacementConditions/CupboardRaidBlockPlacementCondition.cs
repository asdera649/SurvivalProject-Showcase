using SP.Runtime.Core.Entities.Buildings;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Builder.PlacementConditions
{
    public class CupboardRaidBlockPlacementCondition : BasePlacementCondition
    {
        public override bool CanPlace(Vector3 placePosition)
        {
            if (!CupboardEntity.CheckRaidBlock(placePosition))
            {
                return false;
            }

            return true;
        }
    }
}