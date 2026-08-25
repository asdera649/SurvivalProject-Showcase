using KinematicCharacterController;
using Mirror;
using SP.Runtime.Core.Systems.ValueContainer;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Movement
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class CharacterMotor : NetworkBehaviour, IMotor, ICharacterController
    {
        public event UnityAction<float> LandedAction;
        public event UnityAction LeaveStableGroundAction;
    
        [Header("Settings"), Header("Stable Movement")]
        [SerializeField] private float _maxStableMoveSpeed = 4.3f;

        [SerializeField] private float _stableMovementSharpness = 15;
        [SerializeField] private float _orientationSharpness = 10;
    
        [Header("Air Movement")]
        [SerializeField] private float _maxAirMoveSpeed = 5;
        [SerializeField] private float _airAccelerationSpeed = 1;
        [SerializeField] private float _drag = 0.1f;
    
        [Header("Jumping")]
        [SerializeField] private bool _allowJumpingWhenSliding;
        [SerializeField] private float _jumpSpeed = 12;
        [SerializeField] private float _jumpPreGroundingGraceTime;
        [SerializeField] private float _jumpPostGroundingGraceTime;

        [Header("Misc")]
        [SerializeField] private Vector3 _gravity = new(0, -30f, 0);

        public float CurrentMoveSpeed =>
            _maxStableMoveSpeed - _maxStableMoveSpeed * _slowDownsContainer.GetTotal();
        
        private bool _lockMovement;
        public bool LockMovement
        {
            get => _lockMovement;
            set
            {
                if (value)
                {
                    Move(Vector3.zero);
                }
                
                _lockMovement = value;
            }
        }
        
        public Vector3 CustomLookVector { get; set; }

        private readonly ClampedFloatContainer _slowDownsContainer = new();
        public ClampedFloatContainer SlowDownsContainer => _slowDownsContainer;
        
        private readonly BoolContainer _jumpLockContainer = new();
        public BoolContainer JumpLockContainer => _jumpLockContainer;
        
        #region Cached
        
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private Vector3 _lastVelocity;
        private bool _jumpRequested;
        private bool _jumpConsumed;
        private bool _jumpedThisFrame;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump;
    
        #endregion
        
        private KinematicCharacterMotor _motor;
        public KinematicCharacterMotor Motor
        {
            get
            {
                if (_motor == null)
                {
                    _motor = GetComponent<KinematicCharacterMotor>();
                }

                return _motor;
            }
        }
        
        private void Awake()
        {
            Motor.CharacterController = this;
        }

        private void OnEnable()
        {
            Motor.enabled = true;
        }

        private void OnDisable()
        {
            Motor.enabled = false;
        }

        public void Move(Vector3 direction)
        {
            if (!CanMove())
            {
                return;
            }
        
            var moveInputVector = new Vector3(direction.x, 0, direction.z);

            _moveInputVector = moveInputVector.normalized;
            _lookInputVector = _moveInputVector;
        }

        public void Jump()
        {
            if (!CanJump())
            {
                return;
            }

            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            
        }
        
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (CustomLookVector != Vector3.zero)
            {
                _lookInputVector = CustomLookVector;
            }

            if (_lookInputVector != Vector3.zero && _orientationSharpness > 0f)
            {
                var smoothedLookInputDirection = Vector3.Slerp(
                    Motor.CharacterForward,
                    _lookInputVector,
                    1 - Mathf.Exp(-_orientationSharpness * deltaTime)).normalized;
            
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        }
        
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetMovementVelocity;

            if (Motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = Motor.GetDirectionTangentToSurface(
                    currentVelocity,
                    Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            
                var inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                var reorientedInput = Vector3.Cross(
                    Motor.GroundingStatus.GroundNormal,
                    inputRight).normalized * _moveInputVector.magnitude;
            
                targetMovementVelocity = reorientedInput * CurrentMoveSpeed;
            
                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetMovementVelocity,
                    1 - Mathf.Exp(-_stableMovementSharpness * deltaTime));
            }
            else
            {
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    targetMovementVelocity = _moveInputVector * _maxAirMoveSpeed;
                
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        var perpendicularObstructionNormal = Vector3.Cross(
                            Vector3.Cross(
                                Motor.CharacterUp,
                                Motor.GroundingStatus.GroundNormal),
                            Motor.CharacterUp).normalized;
                    
                        targetMovementVelocity = Vector3.ProjectOnPlane(
                            targetMovementVelocity,
                            perpendicularObstructionNormal);
                    }

                    var velocityDiff = Vector3.ProjectOnPlane(
                        targetMovementVelocity - currentVelocity,
                        _gravity);
                
                    currentVelocity += velocityDiff * (_airAccelerationSpeed * deltaTime);
                }
            
                currentVelocity += _gravity * deltaTime;
                currentVelocity *= 1f / (1f + _drag * deltaTime);
            }
        
            {
                _jumpedThisFrame = false;
                _timeSinceJumpRequested += deltaTime;
            
                if (_jumpRequested)
                {
                    if (!_jumpConsumed &&
                        ((_allowJumpingWhenSliding ? 
                             Motor.GroundingStatus.FoundAnyGround :
                             Motor.GroundingStatus.IsStableOnGround) ||
                         _timeSinceLastAbleToJump <= _jumpPostGroundingGraceTime))
                    {
                        var jumpDirection = Motor.CharacterUp;

                        if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                        {
                            jumpDirection = Motor.GroundingStatus.GroundNormal;
                        }
                    
                        Motor.ForceUnground();
                    
                        currentVelocity += (jumpDirection * _jumpSpeed) - 
                                           Vector3.Project(currentVelocity, Motor.CharacterUp);
                    
                        _jumpRequested = false;
                        _jumpConsumed = true;
                        _jumpedThisFrame = true;
                    }
                }

                if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
                {
                    OnLanded();
                }
                else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
                {
                    OnLeaveStableGround();
                }

                _lastVelocity = currentVelocity;
            }
        }
        
        public void AfterCharacterUpdate(float deltaTime)
        {
            if (_jumpRequested && _timeSinceJumpRequested > _jumpPreGroundingGraceTime)
            {
                _jumpRequested = false;
            }

            if (_allowJumpingWhenSliding ? 
                    Motor.GroundingStatus.FoundAnyGround :
                    Motor.GroundingStatus.IsStableOnGround)
            {
                if (!_jumpedThisFrame)
                {
                    _jumpConsumed = false;
                }

                _timeSinceLastAbleToJump = 0f;
            }
            else
            {
                _timeSinceLastAbleToJump += deltaTime;
            }
        }

        #region Utilities
        
        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        private bool CanMove()
        {
            return !LockMovement;
        }
        
        private bool CanJump()
        {
            return CanMove() && !_jumpLockContainer.GetTotal();
        }

        #endregion

        #region Callbacks
        
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            
        }

        private void OnLanded()
        {
            LandedAction?.Invoke(_lastVelocity.y);
        }

        private void OnLeaveStableGround()
        {
            LeaveStableGroundAction?.Invoke();
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            
        }
        
        #endregion
    }
}