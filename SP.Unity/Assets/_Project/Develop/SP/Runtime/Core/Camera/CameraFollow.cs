using System;
using SP.Runtime.Core.Camera.Input;
using SP.Runtime.Core.Systems.Building.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Camera
{
    [RequireComponent(typeof(BaseCameraInput))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _rotationSensitivity = 0.3f;
        
        [Header("Zoom")]
        [SerializeField] private Vector3 _minZoomOffset = new (0, 18, -12);
        [SerializeField] private Vector3 _defaultZoom = new (0, 14, -9);
        [SerializeField] private Vector3 _maxZoomOffset = new (0, 9, -4);
        [SerializeField] private float _zoomSensitivity = 15;

        [Header("Obstacle Detection")] 
        [SerializeField] private float _firstStageOfDetectionRadius = 0.15f;
        [SerializeField] private float _secondStageOfDetectionHeight = 10; 
        [SerializeField] private Vector3 _offsetFromTarget = new(0, 1.7f, 0);
        [SerializeField] private float _marginFromObstacle = 0.5f;
        [SerializeField] private LayerMask _layerMask;
        
        private Transform _target;
        public Transform Target
        {
            get => _target;
            set
            {
                _target = value;

                _targetPositionCache = _target.position;
                
                UpdatePosition();
            }
        }

        private Vector3 CurrentOffset => 
            CurrentZoom <= 0.5f ? 
                Vector3.Lerp(_minZoomOffset, _defaultZoom, CurrentZoom * 2) :
                Vector3.Lerp(_defaultZoom, _maxZoomOffset, (CurrentZoom - 0.5f) * 2);

        private float _currentZoom = 0.5f;
        private float CurrentZoom
        {
            get => _currentZoom;
            set => _currentZoom = Mathf.Clamp01(value);
        }
        
        private float _cameraHorizontalPosition;

        private Vector3 _currentOffsetCache;
        
        private Vector3 _targetPositionCache;

        private float _zoomBlockedTime;
        
        private BaseCameraInput _cameraInput;
        private BaseCameraInput CameraInput
        {
            get
            {
                if (_cameraInput == null)
                {
                    _cameraInput = GetComponent<BaseCameraInput>();
                }

                return _cameraInput;
            }
        }

        private void OnEnable()
        {
            CameraInput.SwipeAction += OnSwipe;
            CameraInput.PinchAction += OnPinch;
        }
        
        private void OnDisable()
        {
            CameraInput.SwipeAction -= OnSwipe;
            CameraInput.PinchAction -= OnPinch;
        }

        private void LateUpdate()
        {
            UpdatePosition();
            
            UpdateObstacleDetection();

            _zoomBlockedTime -= Time.deltaTime;
        }
        
        private void UpdatePosition()
        {
            if (Target == null)
            {
                return;
            }
            
            _currentOffsetCache = Vector3.Lerp(_currentOffsetCache, CurrentOffset, _zoomSensitivity * Time.deltaTime);

            var offset = Quaternion.AngleAxis(_cameraHorizontalPosition, Vector3.up) * _currentOffsetCache;
            
            _targetPositionCache = Vector3.Lerp(_targetPositionCache, Target.position, 10 * Time.deltaTime);
            
            transform.position = _targetPositionCache + offset;
            
            transform.LookAt(_targetPositionCache);
        }
        
        private void UpdateObstacleDetection()
        {
            if (Target == null)
            {
                return;
            }

            var positionBake = transform.position;

            //
            // First stage of detection.
            //
            if (BuildUtils.CheckSphere(
                    transform.position,
                    _firstStageOfDetectionRadius,
                    out var hitPoint,
                    _layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                if (hitPoint != transform.position)
                {
                    transform.position = 
                        hitPoint - (hitPoint - transform.position).normalized * _firstStageOfDetectionRadius;
                }
                
                goto main;
            }

            //
            // Second stage of detection.
            //
            if (BuildUtils.Linecast(
                    positionBake,
                    positionBake + Vector3.up * _secondStageOfDetectionHeight,
                    0.1f,
                    out _,
                    _layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                goto main;
            }
            
            return;
            
            main:
            
            var start = Target.position + _offsetFromTarget;

            var direction = (start - positionBake).normalized;
            direction.y = 0;
                
            if (BuildUtils.Linecast(
                    start,
                    positionBake,
                    out var hit, 
                    _layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                transform.position = hit.point;
                    
                transform.LookAt(Target.position);

                transform.position = hit.point + direction * _marginFromObstacle;
            }
        }

        private void OnSwipe(Vector2 deltaPosition)
        {
            _cameraHorizontalPosition -= deltaPosition.x * _rotationSensitivity;
        }
        
        private void OnPinch(Vector2 center, float delta)
        {
            if (_zoomBlockedTime > 0)
            {
                return;
            }

            //var oldZoom = CurrentZoom;
            
            CurrentZoom += delta;

            // if (oldZoom < 0.5f && CurrentZoom >= 0.5f)
            // {
            //     _zoomBlockedTime = 0.6f;
            //     return;
            // }
            //
            // if (oldZoom > 0.5f && CurrentZoom <= 0.5f)
            // {
            //     _zoomBlockedTime = 0.6f;
            // }
        }
    }
}
