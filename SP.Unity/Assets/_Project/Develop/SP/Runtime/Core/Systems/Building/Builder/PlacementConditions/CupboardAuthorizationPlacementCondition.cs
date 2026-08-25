using SP.Runtime.Core.Entities.Buildings;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using SP.Runtime.Core.Entities.Player;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Building.Builder.PlacementConditions
{
    public class CupboardAuthorizationPlacementCondition : BasePlacementCondition
    {
        public CupboardAuthorizationPlacementCondition(Player player)
        {
            _player = player;
        }

        private readonly Player _player;
        
        public override bool CanPlace(Vector3 placePosition)
        {
            if (!CupboardEntity.CheckAuthorization(_player.UniqueId, placePosition))
            {
                return false;
            }

            return true;
        }
    }
}