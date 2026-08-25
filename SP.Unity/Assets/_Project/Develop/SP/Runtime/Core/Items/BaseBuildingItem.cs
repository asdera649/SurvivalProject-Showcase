using System;
using EasyBuildSystem.Features.Scripts.Core.Base.Builder.Enums;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using EasyBuildSystem.Features.Scripts.Core.Base.Socket;
using Mirror;
using SP.Runtime.Core.Entities.Buildings;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Services.BuildingService;
using SP.Runtime.Core.Systems.Building.Builder;
using SP.Runtime.Core.Systems.Building.Builder.PlacementConditions;
using SP.Runtime.Core.Systems.Hiding;
using SP.Runtime.Core.Systems.Input;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.UI.Hud;
using SP.Runtime.Core.UI.Notification;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Items
{
    #region Structs
    
    [Serializable]
    public struct NetworkSocket
    {
        public NetworkSocket(SocketBehaviour socket) : this()
        {
            if (socket == null)
            {
                return;
            }

            if (socket.AttachedPiece.TryGetComponent<NetworkIdentity>(out var identity))
            {
                AttachedPieceNetId = identity.netId;
            }
            
            SocketIndex = GetSocketIndex(socket.AttachedPiece, socket);
        }

        public uint AttachedPieceNetId;
        public int SocketIndex;

        public bool TryGetSocket(out SocketBehaviour output)
        {
            output = null;

            // Прибегнул к такой хитрости, так как мой CustomCommand/CustomClientRPC не может отправлять
            // струткру данных, в который вложена другая струтура данных (например NetworkIdentity),
            // которую нужно сериализовывать по особенному, не так как примитивные типы данных.
            var attachedPiece = Utils.GetSpawnedInServerOrClient(AttachedPieceNetId);
            
            if (attachedPiece != null && attachedPiece.TryGetComponent(out PieceBehaviour piece))
            {
                if (piece.Sockets.Length > SocketIndex && SocketIndex > -1)
                {
                    output = piece.Sockets[SocketIndex];
                }
            }

            return output != null;
        }
            
        private int GetSocketIndex(PieceBehaviour piece, SocketBehaviour socket)
        {
            var output = -1;

            var sockets = piece.Sockets;
        
            for (var i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] == socket)
                {
                    output = i;
                    break;
                }
            }

            return output;
        }
    }
        
    #endregion
    
    public class BaseBuildingItem : BaseItem
    {
        [Header("References(BaseBuildingItem)")]
        [SerializeField] private Sprite _turnButtonSprite;
        [SerializeField] private Sprite _confirmationButtonSprite;
        
        [Header("Settings(BaseBuildingItem)")]
        [SerializeField] private bool _possibilityOfRotation;
        
        [FormerlySerializedAs("turnRelativeToTerrain")] 
        [SerializeField] private bool _turnRelativeToTerrain;
        
        private InputAction _rotateInputAction;
        private InputAction _confirmInputAction;

        private DistancePlacementCondition _distancePlacementCondition;
        private CupboardAuthorizationPlacementCondition _cupboardAuthorizationPlacementCondition;
        private CupboardRaidBlockPlacementCondition _cupboardRaidBlockPlacementCondition;
        
        private BuildingService _buildingService;
        protected Builder builder;
        
        protected Player Player { private set; get; }

        protected override void OnInitialize(Inventory owner)
        {
            base.OnInitialize(owner);
            
            _buildingService = Loader.Instance.BuildingService;
            builder = Loader.Instance.MainCamera.Builder;
            
            Player = owner.GetComponent<Player>();
        }

        protected override void OnDeinitialize()
        {
            Player = null;

            base.OnDeinitialize();
        }

        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Hud.Mode = BaseHud.HudMode.Build;

                    if (_possibilityOfRotation)
                    {
                        _rotateInputAction = Player.Input.AddInputAction<InputAction>(
                            "Rotate",
                            _turnButtonSprite,
                            InputAction.ClickType.Down);

                        _rotateInputAction.Executed += OnRotateExecuted;
                    }

                    _confirmInputAction = Player.Input.AddInputAction<InputAction>(
                        "Confirm",
                        _confirmationButtonSprite,
                        InputAction.ClickType.Up);

                    _confirmInputAction.Executed += OnConfirmExecuted;

                    Player.Hud.AimJoystick.Interactable = true;
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }

            if (isLocalPlayer || isServer)
            {
                if (Transform != null)
                {
                    _distancePlacementCondition = new DistancePlacementCondition(Transform);
                    builder.AddPlacementCondition(_distancePlacementCondition);
                }
                else
                {
                    Debug.LogWarning("Transform is null!");
                }

                if (Player != null)
                {
                    _cupboardAuthorizationPlacementCondition = new CupboardAuthorizationPlacementCondition(Player);
                    builder.AddPlacementCondition(_cupboardAuthorizationPlacementCondition);
                    
                    _cupboardRaidBlockPlacementCondition = new CupboardRaidBlockPlacementCondition();
                    builder.AddPlacementCondition(_cupboardRaidBlockPlacementCondition);
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Hud.ResetSettings();
                    
                    if (_confirmInputAction != null)
                    {
                        _confirmInputAction.Executed -= OnConfirmExecuted;
                        Player.Input.RemoveInputAction(_confirmInputAction);
                        _confirmInputAction = null;
                    }

                    if (_rotateInputAction != null)
                    {
                        _rotateInputAction.Executed -= OnRotateExecuted;
                        Player.Input.RemoveInputAction(_rotateInputAction);
                        _rotateInputAction = null;
                    }
                    
                    Player.Hud.AimJoystick.Interactable = false;
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
                
                builder.ResetSettings();
            }

            if (isLocalPlayer || isServer)
            {
                if (_distancePlacementCondition != null)
                {
                    builder.RemovePlacementCondition(_distancePlacementCondition);
                    _distancePlacementCondition = null;
                }

                if (_cupboardAuthorizationPlacementCondition != null)
                {
                    builder.RemovePlacementCondition(_cupboardAuthorizationPlacementCondition);
                    _cupboardAuthorizationPlacementCondition = null;
                }
                
                if (_cupboardRaidBlockPlacementCondition != null)
                {
                    builder.RemovePlacementCondition(_cupboardRaidBlockPlacementCondition);
                    _cupboardRaidBlockPlacementCondition = null;
                }
            }

            base.OnItemInactive();
        }
        
        private void OnConfirmExecuted(InputAction inputAction)
        {
            Place();
        }

        private void OnRotateExecuted(InputAction inputAction)
        {
            builder.RotatePreview(builder.SelectedPrefab.RotationAxis);
        }
        
        protected void SelectPiece(int pieceId)
        {
            builder.ChangeMode(BuildMode.None);

            builder.SelectPrefab(_buildingService.BuildManager.GetPiece(pieceId));
            builder.TurnRelativeTerrain = _turnRelativeToTerrain;
            
            builder.ChangeMode(BuildMode.Placement);
        }

        
        protected virtual void Place()
        {
            
        }
        
        protected virtual bool CanPlace(Vector3 position, out BasePlacementCondition reason)
        {
            reason = null;
            
            if (!_distancePlacementCondition.CanPlace(position))
            {
                reason = _distancePlacementCondition;
                return false;
            }

            if (!_cupboardAuthorizationPlacementCondition.CanPlace(position))
            {
                reason = _cupboardAuthorizationPlacementCondition;
                return false;
            }
            
            if (!_cupboardRaidBlockPlacementCondition.CanPlace(position))
            {
                reason = _cupboardRaidBlockPlacementCondition;
                return false;
            }
            
            return true;
        }
        
        protected void PlacePiece(int pieceId, Vector3 position, Vector3 rotation, NetworkSocket networkSocket)
        {
            if (!isServer)
            {
                return;
            }
            
            if (!CanPlace(position, out var reason))
            {
                OnPieceFailedPlace(position, reason);
                return;
            }

            networkSocket.TryGetSocket(out var socket);
        
            if (_buildingService.TryPlacePiece(
                    pieceId,
                    position,
                    rotation,
                    socket,
                    out var output))
            {
                OnPiecePlace(output);
            }
        }
        
        protected virtual void OnPiecePlace(PieceBehaviour piece)
        {
            
        }

        protected virtual void OnPieceFailedPlace(Vector3 position, BasePlacementCondition reason)
        {
            if (!isServer)
            {
                return;
            }

            if (Player != null)
            {
                switch (reason)
                {
                    case DistancePlacementCondition:
                    {
                        Player.SendNotification("{TooFarAway}", NotificationType.Warning);
                        break;
                    }
                    case CupboardAuthorizationPlacementCondition:
                    {
                        CupboardEntity.CheckAuthorization(Player, position);
                        break;
                    }
                    case CupboardRaidBlockPlacementCondition:
                    {
                        CupboardEntity.CheckRaidBlock(Player, position);
                        break;
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }
        }

    }
}