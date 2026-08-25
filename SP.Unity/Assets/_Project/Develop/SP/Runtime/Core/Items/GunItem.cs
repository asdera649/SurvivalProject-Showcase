using System;
using System.Linq;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items.Settings;
using SP.Runtime.Core.Items.Settings.Impact;
using SP.Runtime.Core.Objects;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Systems.Input;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.UI.Hud;
using SP.Runtime.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/GunItem")]
    public class GunItem : BaseGunItem
    {
        #region Structs

        private enum ShootingType
        { 
            SingleClick,
            Holding
        }

        #endregion

        private readonly CustomCommand _cmdShot = new(nameof(CmdShot));
        private readonly CustomClientRpc _rpsShot = new(nameof(RpcShot));

        [Header("Prefabs(GunItem)")]
        [SerializeField] private Bullet _bulletPrefab;
        
        [Header("Settings(GunItem)")]
        [SerializeField] private ShootingType _shootingType;
        [SerializeField] private float _rateOfFire;
        [SerializeField] private float _shootingDistance;
        [SerializeField] private float _scatter;
        [Range(0, 1)]
        [SerializeField] private float _slowDownWhenAiming;
        [SerializeField] private ImpactSettings _impactSettings;

        private readonly int _isAiming = Animator.StringToHash("IsAiming");
        
        private Vector3 _shotDirection;
        
        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (!isLocalPlayer)
            {
                return;
            }
            
            if (Player != null)
            {
                Player.Hud.Mode = BaseHud.HudMode.Gun;
                Player.Input.CurrentAttackType = _shootingType == ShootingType.Holding ?
                    BaseInput.AttackType.Holding :
                    BaseInput.AttackType.SingleClick;
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
                
                SetAim(false);
            }

            base.OnItemInactive();
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();

            #region Queue

            if (!isLocalPlayer && isServer)
            {
                if (CanShot() && GetFromQueue())
                {
                    CmdShot(new CustomVector3(0, 0, 0));
                }
            }

            #endregion

            if (!isLocalPlayer)
            {
                return;
            }

            if (!IsActive)
            {
                return;
            }
            
            if (NetworkAim != null)
            {
                SetAim(NetworkAim.AimDirection != Vector3.zero);
            }
            else
            {
                Debug.LogWarning("Network aim is null!");
            }
        }
        
        private void SetAim(bool value)
        {
            if (!isLocalPlayer)
            {
                return;
            }
            
            if (NetworkAnimator != null)
            {
                if (NetworkAnimator.animator.GetBool(_isAiming) != value)
                {
                    NetworkAnimator.animator.SetBool(_isAiming, value);
                }
            }
            else
            {
                Debug.LogWarning("Network animator is null!");
            }
        
            SlowDown(value, _slowDownWhenAiming);
        }
        
        public override void Use()
        {
            LocalShot();
        }
        
        private bool CanShot()
        {
            bool value;

            if (GetWayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
            {
                value = ammo > 0;
            }
            else
            {
                value = Quantity > 0;
            }

            return !IsCast && !IsReloading && value && CheckSelectionTime();
        }

        private void LocalShot()
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (NetworkAim != null)
            {
                if (NetworkAim.AimDirection != Vector3.zero && CanShot())
                {
                    if (_shootingType == ShootingType.SingleClick)
                    {
                        _shotDirection = NetworkAim.AimDirection;
                    }

                    StartCast(_rateOfFire);

                    if (isLocalPlayer && !isServer)
                    {
                        _cmdShot.Send(new CustomVector3(_shotDirection));
                    }
                }
            }
            else
            {
                Debug.LogWarning("Aim controller is null!");
            }
        }

        public void CmdShot(CustomVector3 direction)
        {
            if (!isServer)
            {
                return;
            }

            if (_shootingType == ShootingType.SingleClick)
            {
                _shotDirection = direction.ToVector3();
            }

            if (CanShot())
            {
                StartCast(_rateOfFire);
            }
            else
            {
                AddToQueue();
            }
        }

        protected override void OnStartCast()
        {
            base.OnStartCast();

            if (IsReloading)
            {
                return;
            }

            if (NetworkAim != null)
            {
                var direction = (_shootingType == ShootingType.SingleClick ? _shotDirection : NetworkAim.AimDirection) + 
                                new Vector3(
                                    Random.Range(-_scatter, _scatter),
                                    Random.Range(-_scatter, _scatter), 
                                    Random.Range(-_scatter, _scatter));
                
                OnShot(direction);

                if (isServer)
                {
                    if (GetWayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
                    {
                        ammo--;
                    }
                    else
                    {
                        Quantity--;
                    }
                    
                    _rpsShot.Send(new CustomVector3(direction));
                    
                    ToLoseStrength();
                }
            }
            else
            {
                Debug.LogWarning("Aim controller is null!");
            }
        }

        private void RpcShot(CustomVector3 direction)
        {
            if (isLocalPlayer || isServer)
            {
                return;
            }

            OnShot(direction.ToVector3());
        }

        protected virtual void OnShot(Vector3 direction)
        {
            Shot(direction);

            if (GunModel != null)
            {
                GunModel.Use();
            }
        }

        protected void Shot(Vector3 direction)
        {
            var bulletFirstPoint = GunModel != null ? GunModel.MuzzleTransform.position : AimOriginPosition;
            
            var bulletSecondPoint = AimOriginPosition + direction.normalized * _shootingDistance;
            
            var hits = Physics.RaycastAll(
                    AimOriginPosition, 
                    direction, 
                    _shootingDistance, 
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
                
                bulletSecondPoint = h.point;
            
                if (isServer)
                {
                    if (ComponentUtils.TryGetComponentInParent<BaseEntity>(h.transform, out var targetEntity) &&
                        targetEntity != Entity)
                    {
                        SendDamage(targetEntity);
                    }
                }
                
                break;
            }
            
            Array.Resize(ref hits, size);

            if (Loader.Instance.PoolService.TrySpawnObject(
                    _bulletPrefab,
                    bulletFirstPoint,
                    Quaternion.identity,
                    out var bullet))
            {
                bullet.Initialize(bulletFirstPoint - bullet.transform.position, bulletSecondPoint - bullet.transform.position);

                _impactSettings.SpawnImpactsByHits(hits);
            }
        }
    }
}
