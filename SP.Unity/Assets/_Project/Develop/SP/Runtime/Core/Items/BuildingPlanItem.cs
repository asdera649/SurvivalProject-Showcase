using System;
using System.Collections.Generic;
using System.Linq;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Systems.Building.Builder.PlacementConditions;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Core.Systems.Hiding;
using SP.Runtime.Core.Systems.Input;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.UI.Notification;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/BuildingPlanItem")]
    public class BuildingPlanItem : BaseBuildingItem
    {
        #region Structs
        
        [Serializable]
        public class Building
        {
            [FormerlySerializedAs("PieceName")]
            [SerializeField] private string _name;
            public string Name => _name;
            
            [FormerlySerializedAs("PieceId")]
            [SerializeField] private int _pieceId;
            public int PieceId => _pieceId;
            
            [FormerlySerializedAs("Icon")]
            [SerializeField] private Sprite _icon;
            public Sprite Icon => _icon;
            
            [FormerlySerializedAs("RequiredItem")]
            [SerializeField] private RequiredItem _requiredItem;
            public RequiredItem RequiredItem => _requiredItem;
        }
        
        #endregion
        
        private readonly CustomCommand _cmdPlaceBuilding = new(nameof(CmdPlaceBuilding));

        [Header("Settings(BuildingPlanItem)")]
        [SerializeField] private Building[] _buildingPool;

        private int _currentBuildingIndex;
        private int CurrentBuildingIndex
        {
            get => _currentBuildingIndex;
            set
            {
                var oldValue = _currentBuildingIndex;
                _currentBuildingIndex = value;

                if (oldValue != _currentBuildingIndex)
                {
                    OnCurrentBuildingIndexUpdated();
                }   
            }
        }
        
        private readonly Dictionary<AdditionalInputAction, Building> _instancedAdditionalInputActions = new();

        private InputAction _buildingChangeInputAction;

        private CostPlacementCondition _costPlacementCondition;
        
        protected override void OnItemActive()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    _buildingChangeInputAction = Player.Input.AddInputAction<BuildingChangeInputAction>(
                        "Change building",
                        null,
                        InputAction.ClickType.Down);

                    foreach (var b in _buildingPool)
                    {
                        var additionalInputAction = _buildingChangeInputAction.AddAdditionalInputAction(
                            "{" + b.Name + "}",
                            "-" + b.RequiredItem.Quantity + " {" + b.RequiredItem.Item.Name + "}",
                            b.Icon);

                        additionalInputAction.ExecuteAction += OnBuildingChangeExecute;
                        
                        _instancedAdditionalInputActions.Add(additionalInputAction, b);
                    }

                    UpdateInputActionIcon();
                    
                    if (Player.TryGetComponent<Hider>(out var hider))
                    {
                        hider.HidingMode = Hider.HandlersHidingMode.NotFull;
                    }
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
                
                SelectPiece(_buildingPool[CurrentBuildingIndex].PieceId);
            }

            if (isLocalPlayer || isServer)
            {
                if (Inventory != null)
                {
                    _costPlacementCondition = new CostPlacementCondition(
                        Inventory,
                        _buildingPool[CurrentBuildingIndex].RequiredItem);
                    
                    builder.AddPlacementCondition(_costPlacementCondition);
                }
                else
                {
                    Debug.LogWarning("Inventory is null!");
                }
            }

            base.OnItemActive();
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    if (_buildingChangeInputAction != null)
                    {
                        foreach (var a in _instancedAdditionalInputActions)
                        {
                            a.Key.ExecuteAction -= OnBuildingChangeExecute;
                            _buildingChangeInputAction.RemoveAdditionalInputAction(a.Key);
                        }
                        
                        _instancedAdditionalInputActions.Clear();
                        
                        Player.Input.RemoveInputAction(_buildingChangeInputAction);
                        _buildingChangeInputAction = null;
                    }
                    
                    if (Player.TryGetComponent<Hider>(out var hider))
                    {
                        hider.HidingMode = Hider.HandlersHidingMode.Full;
                    }
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }
            
            if (isLocalPlayer || isServer)
            {
                builder.RemovePlacementCondition(_costPlacementCondition);
                _costPlacementCondition = null;
            }

            base.OnItemInactive();
        }

        private void OnBuildingChangeExecute(AdditionalInputAction additionalInputAction)
        {
            CurrentBuildingIndex = _instancedAdditionalInputActions.Keys.ToList().IndexOf(additionalInputAction);
            
            if (CurrentBuildingIndex == -1)
            {
                return;
            }

            SelectPiece(_buildingPool[CurrentBuildingIndex].PieceId);

            UpdateInputActionIcon();
        }
        
        private void UpdateInputActionIcon()
        {
            _buildingChangeInputAction.Icon = _buildingPool[CurrentBuildingIndex].Icon;
        }

        protected override bool CanPlace(Vector3 position, out BasePlacementCondition reason)
        {
            if (!base.CanPlace(position, out reason))
            {
                return false;
            }

            if (!_costPlacementCondition.CanPlace(position))
            {
                reason = _costPlacementCondition;
                return false;
            }
        
            return true;
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
            
            _cmdPlaceBuilding.Send(
                CurrentBuildingIndex, 
                new CustomVector3(builder.CurrentPreview.transform.position), 
                new CustomVector3(builder.CurrentPreview.transform.eulerAngles), 
                new NetworkSocket(Loader.Instance.MainCamera.Builder.CurrentSocket));
        }
        
        private void CmdPlaceBuilding(
            int buildingIndex,
            CustomVector3 position,
            CustomVector3 rotation,
            NetworkSocket networkSocket)
        {
            if (!isServer)
            {
                return;
            }

            CurrentBuildingIndex = buildingIndex;
            
            PlacePiece(
                _buildingPool[CurrentBuildingIndex].PieceId,
                position.ToVector3(),
                rotation.ToVector3(),
                networkSocket);
        }
        
        protected override void OnPiecePlace(PieceBehaviour piece)
        {
            base.OnPiecePlace(piece);
        
            if (!isServer)
            {
                return;
            }

            var building = _buildingPool[CurrentBuildingIndex];
            
            Inventory.Remove(building.RequiredItem.Item, building.RequiredItem.Quantity);
        }

        protected override void OnPieceFailedPlace(Vector3 position, BasePlacementCondition reason)
        {
            base.OnPieceFailedPlace(position, reason);
            
            if (!isServer)
            {
                return;
            }
            
            if (Player != null)
            {
                if (reason is CostPlacementCondition)
                {
                    Player.SendNotification("{NotEnoughResources}", NotificationType.Warning);
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }
        }

        private void OnCurrentBuildingIndexUpdated()
        {
            if (CurrentBuildingIndex == -1)
            {
                return;
            }

            _costPlacementCondition.RequiredItem = _buildingPool[CurrentBuildingIndex].RequiredItem;
        }
    }
}
