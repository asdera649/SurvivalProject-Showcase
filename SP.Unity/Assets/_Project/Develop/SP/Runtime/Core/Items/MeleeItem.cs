using System;
using System.Linq;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items.Settings;
using SP.Runtime.Core.Items.Settings.Impact;
using SP.Runtime.Core.Systems.Input;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.UI.Hud;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/MeleeItem")]
    public class MeleeItem : BaseCombatItem
    {
        private readonly CustomCommand _cmdStartCast = new(nameof(CmdStartCast));
        private readonly CustomClientRpc _rpcStartCast = new(nameof(RpcStartCast));

        [Header("Settings(MeleeItem)")]
        [SerializeField] private float _impactLength = 1;
        [Range(0, 1)]
        [SerializeField] private float _slowDownAfterCast;
        [SerializeField] private ImpactSettings _impactSettings;
        
        [Header("Animations(MeleeItem)")]
        [SerializeField] private int _attackAnimationID;
        
        private readonly float _castTime = 0.6f;
        private readonly float _fullCastTime = 1.1f;
        
        private readonly int _cancelAttack = Animator.StringToHash("CancelAttack");
        private readonly int _attackID = Animator.StringToHash("AttackID");
        private readonly int _attack = Animator.StringToHash("Attack");
        
        private bool _isCasted;
        private float _currentCastDuration;
        
        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (!isLocalPlayer)
            {
                return;
            }
            
            if (Player != null)
            {
                Player.Hud.Mode = BaseHud.HudMode.Melee;
                Player.Input.CurrentAttackType = BaseInput.AttackType.Holding;
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Input.ResetSettings();
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }

            ResetAnimation();

            base.OnItemInactive();
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();

            #region Queue

            if (!isLocalPlayer && isServer)
            {
                if (CanCast() && GetFromQueue())
                {
                    CmdStartCast();
                }
            }

            #endregion
        }
        
        private void ResetAnimation()
        {
            if (NetworkAnimator != null)
            {
                NetworkAnimator.animator.SetTrigger(_cancelAttack);
            }
            else
            {
                Debug.LogWarning("Animator is null!");
            }
        }

        public override void Use()
        {
            base.Use();
            
            if (NetworkAim != null)
            {
                if (NetworkAim.AimDirection != Vector3.zero)
                {
                    LocalShot();
                }
            }
            else
            {
                Debug.LogWarning("Network aim is null!");
            }
        }
        
        private bool CanCast()
        {
            return !IsCast && CheckSelectionTime();
        }
        
        private void LocalShot()
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (CanCast())
            {
                StartCast(_fullCastTime);

                if (isLocalPlayer && !isServer)
                {
                    _cmdStartCast.Send();
                }
            }
        }

        private void CmdStartCast()
        {
            if (!isServer)
            {
                return;
            }

            if (CanCast())
            {
                StartCast(_fullCastTime);
            }
            else
            {
                AddToQueue();
            }
        }

        private void RpcStartCast()
        {
            if (isLocalPlayer || isServer)
            {
                return;
            }

            StartCast(_fullCastTime, true);
        }

        protected override void OnStartCast()
        {
            base.OnStartCast();

            _isCasted = false;
            _currentCastDuration = 0;

            if (isLocalPlayer)
            {
                if (CharacterMotor != null)
                {
                    CharacterMotor.LockMovement = true;
                }
                else
                {
                    Debug.LogWarning("Character motor is null!");
                }

                SlowDown(true, _slowDownAfterCast);
            }

            if (NetworkAnimator != null)
            {
                NetworkAnimator.animator.ResetTrigger(_cancelAttack);

                NetworkAnimator.animator.SetInteger(_attackID, _attackAnimationID);
                NetworkAnimator.animator.SetTrigger(_attack);
            }
            else
            {
                Debug.LogWarning("Animator is null!");
            }

            if (Model != null)
            {
                Model.Use();
            }

            if (isServer)
            {
                _rpcStartCast.Send();
            }
        }

        protected override void OnStopCast()
        {
            base.OnStopCast();

            ResetAnimation();

            if (!isLocalPlayer)
            {
                return;
            }

            if (CharacterMotor != null)
            {
                CharacterMotor.LockMovement = false;
            }
            else
            {
                Debug.LogWarning("Character motor is null!");
            }

            SlowDown(false, _slowDownAfterCast);
        }

        protected override void OnDuringCast()
        {
            base.OnDuringCast();

            if (_isCasted)
            {
                return;
            }
            
            _currentCastDuration += Time.deltaTime;

            if (_currentCastDuration > _castTime)
            {
                Cast();
                _isCasted = true;
            }
        }

        protected override void OnCastCompleted()
        {
            base.OnCastCompleted();

            if (!isLocalPlayer)
            {
                return;
            }
            
            SlowDown(false, _slowDownAfterCast);
        }

        private void Cast()
        {
            if (isLocalPlayer)
            {
                if (CharacterMotor != null)
                {
                    CharacterMotor.LockMovement = false;
                }
                else
                {
                    Debug.LogWarning("Character motor is null!");
                }
            }

            if (Entity != null && NetworkAim != null)
            {
                var castDirection = NetworkAim.AimDirection == Vector3.zero ?
                    Transform.forward :
                    NetworkAim.AimDirection;
                
                var hits = Physics.RaycastAll(
                        AimOriginPosition,
                        castDirection,
                        _impactLength,
                        -1,
                        QueryTriggerInteraction.Collide).OrderBy(h => h.distance).ToArray();

                var size = 0;
                
                foreach (var h in hits)
                {
                    size++;
                    
                    if (h.collider.isTrigger)
                    {
                        continue;
                    }
                    
                    if (ComponentUtils.TryGetComponentInParent(h.transform, out BaseEntity targetEntity) &&
                        targetEntity != Entity)
                    {
                        OnHit(targetEntity);
                    }
                    
                    break;
                }

                Array.Resize(ref hits, size);
                
                _impactSettings.SpawnImpactsByHits(hits);
            }
            else
            {
                if (Entity == null)
                {
                    Debug.LogWarning("Entity is null!");
                }

                if (NetworkAim == null)
                {
                    Debug.LogWarning("Aim controller is null!");
                }
            }
        }

        private void OnHit(BaseEntity targetEntity)
        {
            if (!isServer)
            {
                return;
            }
            
            if (SendDamage(targetEntity))
            {
                ToLoseStrength();
            }
        }
    }
}
