using Mirror;
using RootMotion;
using RootMotion.FinalIK;
using SP.Runtime.Core.Movement;
using UnityEngine;

namespace SP.Runtime.Core.Systems.NetworkAim
{
    [RequireComponent(typeof(CharacterMotor))]
    public class NetworkAim : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private AimIK _aimIK;
        public AimIK AimIK => _aimIK;
        
        [SerializeField] private LimbIK _limbIK;
        public LimbIK LimbIK => _limbIK;
    
        [Header("Settings")]
        [SerializeField] private float _weightSmoothTime = 0.07f;
        [SerializeField] private float _targetSwitchSmoothTime = 0.1f;
        [Space(10)]
        [SerializeField] private bool _smoothTurnTowardsTarget = true;
        [SerializeField] private float _maxRadiansDelta = 3;
        [SerializeField] private float _maxMagnitudeDelta = 3;
        [SerializeField] private float _slerpSpeed = 10;
        [SerializeField] private float _smoothDampTime;
        [Space(10)]
        [SerializeField] private float _minDistance = 1f;
        [Space(10)]
        [SerializeField] private Vector3 _offset = Vector3.up;
        [SerializeField] private Vector3 _pivotOffsetFromRoot = Vector3.up;

        [SyncVar(hook = nameof(OnAimDirectionUpdate))]
        private Vector3 _aimDirection;
        public Vector3 AimDirection
        {
            get => _aimDirection;
            set
            {
                var oldValue = _aimDirection;
                _aimDirection = value;
            
                OnAimDirectionUpdate(oldValue, _aimDirection);
            }
        }
        
        private Vector3 Pivot => AimIK.transform.position + AimIK.transform.rotation * _pivotOffsetFromRoot;
    
        private const string _targetName = "AimTarget";
        private const float _targetDistance = 5;
        
        private const float _turnToTargetMlp = 1;

        private Vector3 _lastAimDirection;
        
        private float _weight;
        private Transform _target;
        private Transform _lastTarget;

        #region Cached
        
        private Vector3 _cachedDirection;
        private Vector3 _cachedLastPosition;
        private float _cachedSwitchWeight;
        private float _cachedWeightV;
        private float _cachedLimbPositionVelocity;
        private float _cachedLimbRotationVelocity;
        private float _cachedSwitchWeightV;
        private bool _cachedLastSmoothTowardsTarget;
        private float _cachedYawV;
        private float _cachedPitchV;
        private float _cachedDirMagV;
        
        #endregion

        private CharacterMotor _characterMotor;
        private CharacterMotor CharacterMotor
        {
            get
            {
                if (_characterMotor == null)
                {
                    _characterMotor = GetComponent<CharacterMotor>();
                }

                return _characterMotor;
            }
        }

        private void Start()
        {
            _cachedDirection = AimIK.solver.IKPosition - Pivot;
            _cachedLastPosition = AimIK.solver.IKPosition;

            _target = new GameObject(_targetName).transform;
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            syncDirection = SyncDirection.ClientToServer;
        }

        private void OnDestroy()
        {
            if (_target != null)
            {
                Destroy(_target.gameObject);
            }
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }
            
            if (AimDirection != Vector3.zero)
            {
                _target.position = transform.position + AimDirection * _targetDistance;
            }
            else
            {
                _target.position = transform.position + _lastAimDirection * _targetDistance;
            }
        }

        private void LateUpdate()
        {
            UpdateAim();
        }

        private void UpdateAim()
        {
            AimIK.solver.IKPositionWeight = Mathf.SmoothDamp(AimIK.solver.IKPositionWeight, _weight, ref _cachedWeightV,
                _weightSmoothTime);

            if (AimIK.solver.IKPositionWeight >= 0.999f && _weight > AimIK.solver.IKPositionWeight)
            {
                AimIK.solver.IKPositionWeight = 1;
            }

            if (AimIK.solver.IKPositionWeight <= 0.001f && _weight < AimIK.solver.IKPositionWeight)
            {
                AimIK.solver.IKPositionWeight = 0;
            }

            LimbIK.solver.IKPositionWeight = Mathf.SmoothDamp(LimbIK.solver.IKPositionWeight, _weight, ref _cachedLimbPositionVelocity,
                _weightSmoothTime);
            
            if (LimbIK.solver.IKPositionWeight >= 0.999f && _weight > LimbIK.solver.IKPositionWeight)
            {
                LimbIK.solver.IKPositionWeight = 1;
            }

            if (LimbIK.solver.IKPositionWeight <= 0.001f && _weight < LimbIK.solver.IKPositionWeight)
            {
                LimbIK.solver.IKPositionWeight = 0;
            }
            
            LimbIK.solver.IKRotationWeight = Mathf.SmoothDamp(LimbIK.solver.IKRotationWeight, _weight, ref _cachedLimbRotationVelocity,
                _weightSmoothTime);
            
            if (LimbIK.solver.IKRotationWeight >= 0.999f && _weight > LimbIK.solver.IKRotationWeight)
            {
                LimbIK.solver.IKRotationWeight = 1;
            }

            if (LimbIK.solver.IKRotationWeight <= 0.001f && _weight < LimbIK.solver.IKRotationWeight)
            {
                LimbIK.solver.IKRotationWeight = 0;
            }

            if (AimIK.solver.IKPositionWeight <= 0)
            {
                return;
            }

            _cachedSwitchWeight = Mathf.SmoothDamp(_cachedSwitchWeight, 1, ref _cachedSwitchWeightV, _targetSwitchSmoothTime);

            if (_cachedSwitchWeight >= 0.999f)
            {
                _cachedSwitchWeight = 1;
            }
            
            AimIK.solver.IKPosition = Vector3.Lerp(_cachedLastPosition, _target.position + _offset, _cachedSwitchWeight);

            if (_smoothTurnTowardsTarget != _cachedLastSmoothTowardsTarget)
            {
                _cachedDirection = AimIK.solver.IKPosition - Pivot;
                _cachedLastSmoothTowardsTarget = _smoothTurnTowardsTarget;
            }

            if (_smoothTurnTowardsTarget)
            {
                var targetDirection = AimIK.solver.IKPosition - Pivot;

                if (_slerpSpeed > 0)
                {
                    _cachedDirection = Vector3.Slerp(_cachedDirection, targetDirection, Time.deltaTime * _slerpSpeed);
                }

                if (_maxRadiansDelta > 0 || _maxMagnitudeDelta > 0)
                {
                    _cachedDirection = Vector3.RotateTowards(_cachedDirection, targetDirection,
                        Time.deltaTime * _maxRadiansDelta, _maxMagnitudeDelta);
                }

                if (_smoothDampTime > 0)
                {
                    var pitch = Mathf.SmoothDampAngle(V3Tools.GetPitch(_cachedDirection), V3Tools.GetPitch(targetDirection),
                        ref _cachedPitchV, _smoothDampTime);
                    
                    var yaw = Mathf.SmoothDampAngle(V3Tools.GetYaw(_cachedDirection), V3Tools.GetYaw(targetDirection),
                        ref _cachedYawV, _smoothDampTime);
                    
                    var dirMag = Mathf.SmoothDamp(_cachedDirection.magnitude, targetDirection.magnitude,
                        ref _cachedDirMagV, _smoothDampTime);

                    _cachedDirection = Quaternion.Euler(pitch, yaw, 0) * Vector3.forward * dirMag;
                }

                AimIK.solver.IKPosition = Pivot + _cachedDirection;
            }

            ApplyMinDistance();
            
            UpdateRootRotation();
        }

        private void ApplyMinDistance()
        {
            var direction = AimIK.solver.IKPosition - Pivot;
            direction = direction.normalized * Mathf.Max(direction.magnitude, _minDistance);

            AimIK.solver.IKPosition = Pivot + direction;
        }

        private void UpdateRootRotation()
        {
            var max = Mathf.Lerp(180, _turnToTargetMlp, AimIK.solver.IKPositionWeight);
        
            if (max < 180)
            {
                var faceDirLocal = Quaternion.Inverse(AimIK.transform.rotation) * (AimIK.solver.IKPosition - Pivot);
                var angle = Mathf.Atan2(faceDirLocal.x, faceDirLocal.z) * Mathf.Rad2Deg;
        
                var rotation = 0f;

                if (angle > max)
                {
                    rotation = angle - max;
                }

                if (angle < -max)
                {
                    rotation = angle + max;
                }
            
                var direction = Quaternion.AngleAxis(rotation, AimIK.transform.up) * AimIK.transform.rotation * Vector3.forward;
            
                CharacterMotor.CustomLookVector = AimDirection != Vector3.zero ? direction : Vector3.zero;
            }
        }

        private void OnAimDirectionUpdate(Vector3 oldValue, Vector3 newValue)
        {
            if (oldValue != Vector3.zero || newValue != Vector3.zero)
            {
                _lastAimDirection = oldValue;
            }
            
            _weight = newValue == Vector3.zero ? 0 : 1;
        }
    }
}
