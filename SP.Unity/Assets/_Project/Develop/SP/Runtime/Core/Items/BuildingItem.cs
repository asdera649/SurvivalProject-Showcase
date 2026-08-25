using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Systems.Inventory;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/BuildingItem")]
    public class BuildingItem : BaseBuildingItem
    {
        private readonly CustomCommand _cmdPlacePiece = new(nameof(CmdPlacePiece));
        
        [Header("Settings(BuildingItem)")]
        [FormerlySerializedAs("pieceId")]
        [SerializeField] private int _pieceId;

        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (!isLocalPlayer)
            {
                return;
            }
            
            SelectPiece(_pieceId);
        }
        
        protected override void Place()
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (builder.CurrentPreview == null)
            {
                return;
            }
            
            _cmdPlacePiece.Send(
                new CustomVector3(builder.CurrentPreview.transform.position), 
                new CustomVector3(builder.CurrentPreview.transform.eulerAngles), 
                new NetworkSocket(Loader.Instance.MainCamera.Builder.CurrentSocket));
        }
        
        private void CmdPlacePiece(CustomVector3 position, CustomVector3 rotation, NetworkSocket networkSocket)
        {
            if (!isServer)
            {
                return;
            }

            PlacePiece(_pieceId, position.ToVector3(), rotation.ToVector3(), networkSocket);
        }
        
        protected override void OnPiecePlace(PieceBehaviour piece)
        {
            base.OnPiecePlace(piece);
            
            Quantity--;
        }
    }
}