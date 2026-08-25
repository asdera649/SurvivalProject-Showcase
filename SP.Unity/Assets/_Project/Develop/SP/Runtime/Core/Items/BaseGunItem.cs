using System.Collections.Generic;
using SP.Runtime.Core.Objects.Models;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Systems.Input;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.UI;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    public class BaseGunItem : BaseCombatItem
    {
        #region Structs
        
        protected enum WayOfSpendingAmmo
        {
            SpendSeparateItem,
            SpendOneself
        }

        #endregion
        
        private readonly CustomCommand _cmdReload = new(nameof(CmdReload));
        private readonly CustomClientRpc _rpcReload = new(nameof(RpcReload));

        [Header("Prefabs(BaseGunItem)")]
        [SerializeField] private CountdownTimer _countdownTimerPrefab;

        [Header("References(BaseGunItem)")]
        [SerializeField] private Sprite _reloadButtonSprite;

        [Header("Settings(BaseGunItem)")]
        [SerializeField] private WayOfSpendingAmmo _wayOfSpendingAmmo;
        protected WayOfSpendingAmmo GetWayOfSpendingAmmo => _wayOfSpendingAmmo;

        [SerializeField] private BaseItem _ammoType;
        [SerializeField] private int _capacity;
        [SerializeField] private float _reloadTime;

        [Header("Animations(BaseGunItem)")]
        [SerializeField] private float _upperBodyID;
        [SerializeField] private bool _useSecondHand = true;

        [CustomSyncVar] protected int ammo;

        protected GunModel GunModel => Model as GunModel;

        public override string GetQuantityText
        {
            get
            {
                if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
                {
                    return ammo.ToString();
                }
                else
                {
                    return base.GetQuantityText;
                }
            }
        }
    
        private bool _isReloading;
        protected bool IsReloading => _isReloading;
        
        private static readonly int UpperBodyID = Animator.StringToHash("UpperBodyID");

        private ReloadInputAction _reloadInputAction;
        
        private CountdownTimer _countdownTimer;

        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (isLocalPlayer)
            {
                if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
                {
                    if (Player != null)
                    {
                        _reloadInputAction = Player.Input.AddInputAction<ReloadInputAction>(
                            "Reload",
                            _reloadButtonSprite,
                            InputAction.ClickType.Down);
                        
                        _reloadInputAction.Executed += OnReloadExecuted;
                    }
                    else
                    {
                        Debug.LogWarning("Player is null!");
                    }
                }
                
                _countdownTimer = Instantiate(_countdownTimerPrefab, Loader.Instance.Canvas.transform);
            }

            if (NetworkAim != null)
            {
                if (GunModel != null)
                {
                    NetworkAim.AimIK.solver.transform = GunModel.AimTransform;
                    NetworkAim.LimbIK.solver.target = GunModel.SecondHandTransform;
                }

                if (_useSecondHand)
                {
                    NetworkAim.LimbIK.enabled = true;
                }
            }
            else
            {
                Debug.LogWarning("Aim controller is null!");
            }

            if (NetworkAnimator != null)
            {
                NetworkAnimator.animator.SetFloat(UpperBodyID, _upperBodyID);
                NetworkAnimator.animator.SetLayerWeight(3, 1);
            }
            else
            {
                Debug.LogWarning("Network animator is null!");
            }
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
                {
                    if (_reloadInputAction != null)
                    {
                        _reloadInputAction.Executed -= OnReloadExecuted;
                        
                        if (Player != null)
                        {
                            Player.Input.RemoveInputAction(_reloadInputAction);
                        }
                        else
                        {
                            Debug.LogWarning("Player is null!");
                        }
                        
                        _reloadInputAction = null;
                    }
                }
                
                if (_countdownTimer != null)
                {
                    Destroy(_countdownTimer.gameObject);
                }
            }

            if (NetworkAim != null && NetworkAnimator != null)
            {
                NetworkAim.AimIK.solver.transform = NetworkAnimator.animator.GetBoneTransform(HumanBodyBones.Neck);
                NetworkAim.LimbIK.solver.target = null;

                if (_useSecondHand)
                {
                    NetworkAim.LimbIK.enabled = false;
                }
            }
            else
            {
                if (NetworkAim == null)
                {
                    Debug.LogWarning("Aim controller is null!");
                }

                if (NetworkAnimator == null)
                {
                    Debug.LogWarning("Network animator is null!");
                }
            }

            if (NetworkAnimator != null)
            {
                NetworkAnimator.animator.SetLayerWeight(3, 0);
            }
            else
            {
                Debug.LogWarning("Network animator is null!");
            }

            base.OnItemInactive();
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (_reloadInputAction != null)
            {
                _reloadInputAction.TotalAmmo = Inventory.GetTotal(_ammoType);
            }
        }
        
        protected override List<Action> GetActions()
        {
            var actions = base.GetActions();

            if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
            {
                actions.AddRange(new List<Action>
                {
                    new(nameof(Discharge), Discharge),
                });
            }

            return actions;
        }
        
        private void Discharge()
        {
            if (!isServer)
            {
                return;
            }

            if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem && ammo > 0)
            {
                var item = Instantiate(_ammoType);
                item.Quantity = ammo;
                
                ammo = 0;
                
                Inventory.Add(item);
            }
        }
        
        private bool CanReload()
        {
            if (_wayOfSpendingAmmo != WayOfSpendingAmmo.SpendSeparateItem ||
                _isReloading ||
                ammo >= _capacity ||
                Inventory.GetTotal(_ammoType) == 0)
            {
                return false;
            }

            return true;
        }

        private void OnReloadExecuted(InputAction inputAction)
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
            {
                if (Reload())
                {
                    if (isLocalPlayer && !isServer)
                    {
                        _cmdReload.Send();
                    }
                }
            }
        }

        private void CmdReload()
        {
            if (!isServer)
            {
                return;
            }

            if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
            {
                Reload();
            }
        }

        private bool Reload()
        {
            if (!CanReload())
            {
                return false;
            }

            if (IsCast)
            {
                StopCast();
            }

            _isReloading = true;

            StartCast(_reloadTime);

            return true;
        }

        protected override void OnStartCast()
        {
            base.OnStartCast();

            if (!_isReloading)
            {
                return;
            }
            
            if (GunModel != null)
            {
                GunModel.Reload();
            }
            
            if (isLocalPlayer)
            {
                _countdownTimer.StartTimer("Reload", _reloadTime);
            }

            if (isServer)
            {
                _rpcReload.Send();
            }
        }
        
        private void RpcReload()
        {
            if (isLocalPlayer || isServer)
            {
                return;
            }
            
            if (_wayOfSpendingAmmo == WayOfSpendingAmmo.SpendSeparateItem)
            {
                Reload();
            }
        }

        protected override void OnStopCast()
        {
            base.OnStopCast();

            if (!_isReloading)
            {
                return;
            }
            
            if (GunModel != null)
            {
                GunModel.StopReload();
            }
            
            _isReloading = false;
        }

        protected override void OnCastCompleted()
        {
            base.OnCastCompleted();

            if (!_isReloading)
            {
                return;
            }
            
            if (isServer)
            {
                var total = Inventory.GetTotal(_ammoType);

                if (total > 0 && ammo < _capacity)
                {
                    var tempAmmo = ammo;
                    ammo = Mathf.Clamp(ammo + total, 0, _capacity);
                    Inventory.Remove(_ammoType, ammo - tempAmmo);
                }
            }

            _isReloading = false;
        }
    }
}
