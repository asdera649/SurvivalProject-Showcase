using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using SP.Runtime.Core.Entities.Buildings;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/SleepingBagItem")]
    public class SleepingBagItem : BuildingItem
    {
        protected override void OnPiecePlace(PieceBehaviour piece)
        {
            if (isServer)
            {
                if (Player != null)
                {
                    if (piece.TryGetComponent<SleepingBagEntity>(out var sleepingBag))
                    {
                        sleepingBag.Initialize(Player.UniqueId);
                    }
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }

            // base.OnPiecePlace() должен вызыватся после основной логики, потому что в base.OnPiecePlace()
            // мы декрементируем Quantity и SleepingBagItem уничтожается, так и не выполнив основную логику.
            base.OnPiecePlace(piece);
        }
    }
}
