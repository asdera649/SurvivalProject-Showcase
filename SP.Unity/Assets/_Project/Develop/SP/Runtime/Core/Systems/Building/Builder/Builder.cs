using System.Collections.Generic;
using EasyBuildSystem.Features.Scripts.Core.Base.Builder;
using EasyBuildSystem.Features.Scripts.Core.Base.Builder.Enums;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Group;
using EasyBuildSystem.Features.Scripts.Core.Base.Manager;
using EasyBuildSystem.Features.Scripts.Extensions;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Services.MainCamera;
using SP.Runtime.Core.Systems.Building.Builder.PlacementConditions;
using SP.Runtime.Core.Systems.Building.Conditions.InternalHeightCondition;
using SP.Runtime.Core.Systems.Building.Conditions.InternalUnbuildableAreaCondition;
using SP.Runtime.Core.UI.Building.Builder;
using UnityEngine;
using InternalColliderCondition = SP.Runtime.Core.Systems.Building.Conditions.InternalColliderCondition;
using InternalPhysicsCondition = SP.Runtime.Core.Systems.Building.Conditions.InternalPhysicsCondition.InternalPhysicsCondition;

namespace SP.Runtime.Core.Systems.Building.Builder
{
    [RequireComponent(typeof(MainCamera))]
    public class Builder : BuilderBehaviour
    {
        [Header("Prefabs")] 
        public BuilderTip BuilderTipPrefab;

        [Header("Settings")] 
        public float DelayForSpawnBuilderTip = 0.3f;
        
        public bool TurnRelativeTerrain { get; set; }

        public override Ray GetRay => 
            MainCamera.Camera.ScreenPointToRay(new Vector3(BuildPoint.x, BuildPoint.y));

        public Vector2 BuildPoint { get; set; }
        
        private readonly List<BasePlacementCondition> _placementConditions = new();

        private float _currentDelayForSpawnBuilderTip;

        private bool _allowPlace;
        
        private ConditionBehaviour _firstNotAllowingPlaceReason;
        private BasePlacementCondition _secondNotAllowingPlaceReason;
        
        private BuilderTip _builderTip;
        
        private MainCamera _mainCamera;
        private MainCamera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = GetComponent<MainCamera>();
                }

                return _mainCamera;
            }
        }

        public override void Start()
        {
            base.Start();

            InstantiateBuilderTip();
        }

        public void OnDestroy()
        {
            DestroyBuilderTip();
        }

        private void InstantiateBuilderTip()
        {
            if (_builderTip != null)
            {
                return;
            }
            
            _builderTip = Instantiate(BuilderTipPrefab, Loader.Instance.Canvas.transform);
            _builderTip.gameObject.SetActive(false);
        }

        private void DestroyBuilderTip()
        {
            if (_builderTip == null)
            {
                return;
            }
            
            Destroy(_builderTip.gameObject);
            _builderTip = null;
        }

        public void OnValidate()
        {
            // Правильнее было бы выбрать CameraType = RayType.TopDown, но эта часть
            // пакета, работает не корректно.
            // Поэтому выбрал RayType.ThirdPerson и переопределил GetRay, как у RayType.TopDown,
            // вроде работает стабильно.
            // В будущем хотелось бы либо написать свою систему строительства,
            // либо обновить текущий пакет до последней версии(код там вроде более грамотный).
            
            CameraType = RayType.ThirdPerson;
            RaycastOriginTransform = null;
            RayDetection = DetectionType.Vector;
            
            UsePlacementMode = true;
            ResetModeAfterPlacement = false;
            UseDestructionMode = false;
            UseEditionMode = false;
            
            Source = null;
            PlacementClips = null;
            DestructionClips = null;
            EditionClips = null;
        }

        public void ResetSettings()
        {
            ChangeMode(BuildMode.None);
            TurnRelativeTerrain = false;
            BuildPoint = Vector2.zero;
        }

        #region PlacementCondition
        
        public void AddPlacementCondition(BasePlacementCondition condition)
        {
            if (_placementConditions.Contains(condition))
            {
                return;
            }
            
            _placementConditions.Add(condition);
        }
        
        public void RemovePlacementCondition(BasePlacementCondition condition)
        {
            if (!_placementConditions.Contains(condition))
            {
                return;
            }
            
            _placementConditions.Remove(condition);
        }

        private bool CheckPlacementConditions(Vector3 position, out BasePlacementCondition reason)
        {
            reason = null;
            
            foreach (var c in _placementConditions)
            {
                if (!c.CanPlace(position))
                {
                    reason = c;
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion

        public override void Update()
        {
            base.Update();
            
            if (!HasSocket)
            {
                UpdatePreviewRotation();
            }
            
            UpdateNotAllowingPlaceReasons();

            UpdateBuilderTip();

            UpdatePreviewColor();
        }

        private void UpdateNotAllowingPlaceReasons()
        {
            _allowPlace = true;
            
            _firstNotAllowingPlaceReason = null;
            _secondNotAllowingPlaceReason = null;

            if (CurrentPreview != null)
            {
                if (!Loader.Instance.BuildingService.CanPlace(
                        CurrentPreview,
                        CurrentSocket,
                        out var conditionBehaviourReason))
                {
                    _firstNotAllowingPlaceReason = conditionBehaviourReason;

                    _allowPlace = false;
                }
                
                if (!CheckPlacementConditions(
                        CurrentPreview.transform.position,
                        out var placementConditionsReason))
                {
                    _secondNotAllowingPlaceReason = placementConditionsReason;

                    _allowPlace = false;
                }
            }
        }

        private void UpdateBuilderTip()
        {
            var value = _firstNotAllowingPlaceReason != null || _secondNotAllowingPlaceReason != null;

            if (!_builderTip.gameObject.activeInHierarchy)
            {
                if (value)
                {
                    _currentDelayForSpawnBuilderTip += Time.deltaTime;
                }
                else
                {
                    _currentDelayForSpawnBuilderTip = 0;
                }

                if (_currentDelayForSpawnBuilderTip < DelayForSpawnBuilderTip)
                {
                    return;
                }
            }

            _builderTip.gameObject.SetActive(value);
                
            if (!_builderTip.gameObject.activeInHierarchy)
            {
                return;
            }
            
            _builderTip.transform.position = RectTransformUtility.WorldToScreenPoint(
                Loader.Instance.MainCamera.Camera,
                CurrentPreview.transform.position);

            var entry = "ImpossibleToBuild";

            if (_firstNotAllowingPlaceReason != null)
            {
                switch (_firstNotAllowingPlaceReason)
                {
                    case InternalUnbuildableAreaCondition:
                    {
                        entry = "YouCantBuildInThisPlace";
                        break;
                    }
                    case InternalPhysicsCondition:
                    {
                        entry = "InsufficientStability";
                        break;
                    }
                    case InternalColliderCondition.InternalColliderCondition:
                    {
                        entry = "AnotherObjectIsInTheWay";
                        break;
                    }
                    case InternalHeightCondition:
                    {
                        entry = "MaxConstructionHeightReached";
                        break;
                    }
                }
            }

            if (_secondNotAllowingPlaceReason != null)
            {
                switch (_secondNotAllowingPlaceReason)
                {
                    case CostPlacementCondition:
                    {
                        entry = "NotEnoughResources";
                        break;
                    }
                    case CupboardAuthorizationPlacementCondition:
                    {
                        entry = "TerritoryIsClaimedShort";
                        break;
                    }
                    case CupboardRaidBlockPlacementCondition:
                    {
                        entry = "UnderAttackShort";
                        break;
                    }
                    case DistancePlacementCondition:
                    {
                        entry = "TooFarAway";
                        break;
                    }
                }
            }
            
            _builderTip.SetText(entry);
        }

        private void UpdatePreviewRotation()
        {
            if (CurrentPreview == null)
            {
                return;
            }

            if (TurnRelativeTerrain)
            {
                if (Physics.Raycast(GetRay, out var hitInfo, Mathf.Infinity, BuildManager.Instance.FreeLayers))
                {
                    UpdateRotation(hitInfo.normal);
                }
            }
            
            CurrentPreview.transform.Rotate(
                0,
                MainCamera.transform.localEulerAngles.y - CurrentPreview.transform.localEulerAngles.y,
                0,
                Space.Self);
            
            CurrentPreview.transform.rotation *= Quaternion.Euler(CurrentRotationOffset);
        }

        private void UpdateRotation(Vector3 normal)
        {
            var forward = Vector3.Cross(Vector3.Cross(normal, Vector3.up), normal);

            if (forward != Vector3.zero && normal != Vector3.zero)
            {
                CurrentPreview.transform.rotation = Quaternion.LookRotation(forward, normal);
            }
        }

        private void UpdatePreviewColor()
        {
            if (CurrentPreview == null)
            {
                return;
            }

            AllowPlacement = _allowPlace;

            CurrentPreview.gameObject.ChangeAllMaterialsColorInChildren(
                CurrentPreview.Renderers.ToArray(),
                AllowPlacement ? CurrentPreview.PreviewAllowedColor : CurrentPreview.PreviewDeniedColor,
                SelectedPrefab.PreviewColorLerpTime,
                SelectedPrefab.PreviewUseColorLerpTime);
        }

        #region Callbacks
        
        public override void PlacePrefab(GroupBehaviour group = null) { }

        public override void DestroyPrefab() { }

        public override void EditPrefab() { }
        
        #endregion
    }
}