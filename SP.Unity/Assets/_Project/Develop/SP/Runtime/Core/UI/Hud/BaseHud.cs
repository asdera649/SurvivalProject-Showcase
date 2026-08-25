using System;
using System.Collections.Generic;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Services;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.UI.Hud
{
    public class BaseHud : MonoBehaviour
    {
        #region Structs
        
        private class HitsComparer : IComparer<RaycastHit>
        {
            public int Compare(RaycastHit x, RaycastHit y)
            {
                if (x.distance > y.distance)
                {
                    return 1;
                }
                else if (x.distance < y.distance)
                {
                    return -1;
                }

                return 0;
            }
        }
        
        public enum HudMode
        {
            None,
            Build,
            Melee,
            Gun,
            Provision
        }
        
        #endregion
        
        [Header("Prefabs")]
        [SerializeField] private Crosshair _crosshairPrefab;
        [SerializeField] private CrosshairLine _linePrefab;

        [Header("Settings")] 
        [SerializeField] private Color _defaultCrosshairColor;
        [SerializeField] private Color _crosshairCombatModeColor;
        [SerializeField] private Color _crosshairBuildModeColor;
        
        [Space(10)]
        
        [SerializeField] private float _buildModeCrosshairSmoothness = 12;
        [SerializeField] private float _combatModeCrosshairSmoothness = 18.5f;

        [Space(10)] 
        [SerializeField] private bool _ignoreWater;
        [SerializeField] private LayerMask _layerMask;
        
        [Space(10)]
        [SerializeField] private HudSettings _hudSettings;

        public AimingHelperConfiguration.ItemContainer CurrentAimingHelperConfiguration;
        
        private HudMode _mode = HudMode.None;
        public HudMode Mode
        {
            get => _mode;
            set
            {
                var oldMode = _mode;
                _mode = value;

                OnModeUpdate(oldMode, _mode);
            }
        }
        
        public Transform Target { get; set; }
        
        private float _aimingRange = _defaultAimingRange;
        public float AimingRange
        {
            get => _aimingRange;
            set => _aimingRange = Mathf.Clamp01(value);
        }
        
        public Vector3 AimOriginOffset { get; set; }

        public Vector3 AimDirection { get; private set; }

        private Vector3 AimOriginPosition
        {
            get
            {
                if (Target == null)
                {
                    Debug.LogWarning("The target has not been set!");
                    return Vector3.zero;
                }
                
                return Target.position + AimOriginOffset;
            }
        }
        
        public Vector2 BuildPoint => _crosshair != null ? _crosshair.transform.position : Vector2.zero;

        protected bool IsAimingTarget { get; private set; }

        private const float _defaultAimingRange = 1;
        
        private const int _maxHitsCount = 128;
        
        private readonly RaycastHit[] _hitsCache = new RaycastHit[_maxHitsCount];
        private readonly RaycastHit[] _hitsOnObstaclesCache = new RaycastHit[_maxHitsCount];
        
        private bool _isCrosshairFirstUpdate;
        
        private Vector2 _screenPoint;
        private Vector2 _screenPointDelta;

        private HitsComparer _hitsComparer;
        
        private Crosshair _crosshair;
        private CrosshairLine _line;

        private Services.Canvas _canvas;

        private void Awake()
        {
            _hitsComparer = new HitsComparer();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _canvas = Loader.Instance.Canvas;
            
            _crosshair = Instantiate(_crosshairPrefab, _canvas.transform);
            _line = Instantiate(_linePrefab, _canvas.transform);

            Mode = Mode;
            
            SetScreenPoint(Vector2.zero);
        }

        private void OnDestroy()
        {
            if (_crosshair != null)
            {
                Destroy(_crosshair.gameObject);
                _crosshair = null;
            }

            if (_line != null)
            {
                Destroy(_line.gameObject);
                _line = null;
            }
        }

        public void ResetSettings()
        {
            Mode = HudMode.None;
            Target = null;
            AimOriginOffset = Vector3.zero;
            AimingRange = _defaultAimingRange;
        }
        
        public virtual void SetView(bool value)
        {
            
        }
        
        protected void SetScreenPoint(Vector2 screenPoint)
        {
            if (_screenPoint != Vector2.zero && screenPoint != Vector2.zero)
            {
                _screenPointDelta = screenPoint - _screenPoint;
            }
            
            _screenPoint = screenPoint;
        }

        private void LateUpdate()
        {
            switch (Mode)
            {
                case HudMode.Build:
                {
                    UpdateBuildMode();
                    break;
                }
                case HudMode.Melee:
                {
                    UpdateCombatMode();
                    break;
                }
                case HudMode.Gun:
                {
                    UpdateCombatMode();
                    break;
                }
            }
            
            _screenPointDelta = Vector2.zero;
        }
        
        private void UpdateBuildMode()
        {
            _crosshair.gameObject.SetActive(true);
            _line.gameObject.SetActive(false);

            UpdateBuildModeCrosshair();
        }
        
        private void UpdateBuildModeCrosshair()
        {
            var crutch = (Screen.width + Screen.height) / 2;
            var position = new Vector2(_crosshair.transform.position.x, _crosshair.transform.position.y);
            
            position += _screenPointDelta * crutch;

            _crosshair.transform.position = Vector3.Lerp(
                _crosshair.transform.position,
                ClampByScreen(position),
                _buildModeCrosshairSmoothness * Time.deltaTime);
        }

        private void UpdateCombatMode()
        {
            var isActive = Target != null && _screenPoint != Vector2.zero;
            
            _crosshair.gameObject.SetActive(isActive);
            _line.gameObject.SetActive(isActive);

            if (isActive)
            {
                if (!_isCrosshairFirstUpdate)
                {
                    UpdateCombatModeCrosshair(false);
                    _isCrosshairFirstUpdate = true;
                }
                else
                {
                    UpdateCombatModeCrosshair();
                }
                
                UpdateLine();
                
                return;
            }
            
            AimDirection = Vector3.zero;
            
            UpdateHelpWithAiming(null, null);
            
            _isCrosshairFirstUpdate = false;
        }
        
        private void UpdateCombatModeCrosshair(bool isSmooth = true)
        {
            var crutch = (Screen.width + Screen.height) / 2;
            var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            
            var position = screenCenter + _screenPoint * (AimingRange * crutch);

            position = ClampByScreen(position);

            _crosshair.transform.position = isSmooth ? 
                Vector3.Lerp(
                    _crosshair.transform.position,
                    position,
                    _combatModeCrosshairSmoothness * Time.deltaTime) : 
                position;
        }
        
        private Vector2 ClampByScreen(Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp(value.x, 0, Screen.width),
                Mathf.Clamp(value.y, 0, Screen.height));
        }

        private void UpdateLine()
        {
            var screenPointRay = Loader.Instance.MainCamera.Camera.ScreenPointToRay(_crosshair.transform.position);
            
            var hitsCount = Physics.RaycastNonAlloc(
                screenPointRay,
                _hitsCache,
                Mathf.Infinity,
                _layerMask,
                QueryTriggerInteraction.Collide);
            
            Array.Sort(_hitsCache, 0, hitsCount, _hitsComparer);
            
            for (var h = 0; h < hitsCount; h++)
            {
                if (_ignoreWater)
                {
                    if (_hitsCache[h].collider.isTrigger)
                    {
                        continue;
                    }
                }
                else
                {
                    if (_hitsCache[h].collider.isTrigger && 
                        _hitsCache[h].transform.gameObject.layer != LayerUtils.GetWaterLayer())
                    {
                        continue;
                    }
                }
                
                if (_hitsCache[h].transform.root.gameObject != Target.root.gameObject)
                {
                    var hitsOnObstaclesCount = Physics.RaycastNonAlloc(
                        AimOriginPosition,
                        (_hitsCache[h].point - AimOriginPosition).normalized,
                        _hitsOnObstaclesCache,
                        Mathf.Infinity, 
                        _layerMask, 
                        QueryTriggerInteraction.Collide);
                    
                    Array.Sort(_hitsOnObstaclesCache, 0, hitsOnObstaclesCount, _hitsComparer);
                    
                    for (var o = 0; o < hitsOnObstaclesCount; o++)
                    {
                        if (_ignoreWater)
                        {
                            if (_hitsOnObstaclesCache[o].collider.isTrigger)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (_hitsOnObstaclesCache[o].collider.isTrigger && 
                                _hitsOnObstaclesCache[o].transform.gameObject.layer != LayerUtils.GetWaterLayer())
                            {
                                continue;
                            }
                        }
                        
                        if (_hitsOnObstaclesCache[o].transform.root.gameObject != Target.root.gameObject)
                        {
                            _line.LineRenderer.Points = new[]
                            {
                                RectTransformUtility.WorldToScreenPoint(
                                    Loader.Instance.MainCamera.Camera,
                                    AimOriginPosition) / _canvas.transform.localScale.x,
                                RectTransformUtility.WorldToScreenPoint(
                                    Loader.Instance.MainCamera.Camera,
                                    _hitsOnObstaclesCache[o].point) / _canvas.transform.localScale.x
                            };
                            
                            AimDirection = (_hitsOnObstaclesCache[o].point - AimOriginPosition).normalized;

                            UpdateHelpWithAiming(
                                _hitsCache[h].transform.gameObject,
                                _hitsOnObstaclesCache[o].transform.gameObject,
                                _hitsOnObstaclesCache[o].point);
                            
                            return;
                        }
                    }
                }
            }
            
            _line.LineRenderer.Points = null;
            
            UpdateHelpWithAiming(null, null);
            
            AimDirection = Vector3.zero;
        }

        private void UpdateHelpWithAiming(
            GameObject crosshairHit,
            GameObject crosshairLineHit,
            Vector3 hitPoint = default)
        {
            IsAimingTarget = false;

            if (!_hudSettings.HelpWithAiming)
            {
                return;
            }
            
            if (crosshairHit == null)
            {
                _crosshair.SetActiveRedCrosshair(false);
            }
            
            if (crosshairLineHit == null)
            {
                _line.SetActiveRedCrosshair(false);
            }
            
            if (crosshairHit == null && crosshairLineHit == null)
            {
                return;
            }

            if (CurrentAimingHelperConfiguration == null)
            {
                return;
            }

            if (ComponentUtils.TryGetComponentInParent<BaseEntity>(crosshairLineHit.transform, out var entity))
            {
                IsAimingTarget = CurrentAimingHelperConfiguration.ContainsTargetType(entity) &&
                                 (hitPoint == default ||
                                  Vector3.Distance(AimOriginPosition, hitPoint) < CurrentAimingHelperConfiguration.TargetDistance);
            }
            
            _crosshair.SetActiveRedCrosshair(
                ComponentUtils.TryGetComponentInParent(crosshairHit.transform, out entity) &&
                CurrentAimingHelperConfiguration.ContainsTargetType(entity));
            
            _line.SetActiveRedCrosshair(IsAimingTarget);
        }

        #region Callbacks
        
        protected virtual void OnModeUpdate(HudMode oldValue, HudMode newValue)
        {
            _crosshair.Image.rectTransform.anchoredPosition = Vector2.zero;
            
            _crosshair.SetColor(_defaultCrosshairColor);
            
            switch (Mode)
            {
                case HudMode.Build:
                {
                    _crosshair.SetColor(_crosshairBuildModeColor);
                    break;
                }
                case HudMode.Melee:
                {
                    _crosshair.SetColor(_crosshairCombatModeColor);
                    break;
                }
                case HudMode.Gun:
                {
                    _crosshair.SetColor(_crosshairCombatModeColor);
                    break;
                }
            }
            
            _crosshair.gameObject.SetActive(false);
            
            _line.gameObject.SetActive(false);

            AimDirection = Vector2.zero;
        }
        
        #endregion
    }
}