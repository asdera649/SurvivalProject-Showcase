using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.UI;
using SP.Runtime.Core.UI.Hud;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    [CreateAssetMenu(menuName = "Items/MedicalItem")]
    public class MedicalItem : BaseItem
    {
        private readonly CustomCommand _cmdUseMedicalItem = new(nameof(CmdUseMedicalItem));
        private readonly CustomClientRpc _rpcUseMedicalItem = new(nameof(RpcUseMedicalItem));

        [Header("Prefabs(MedicalItem)")]
        [SerializeField] private CountdownTimer _countdownTimerPrefab;

        [Header("Settings(MedicalItem)")]
        [SerializeField] private int _healthValue;
        [SerializeField] private int _healthLimit;
        [SerializeField] private float _usageTime;

        private CountdownTimer _countdownTimer;
    
        private BaseEntity Entity { get; set; }
        private Player Player { get; set; }

        protected override void OnInitialize(Systems.Inventory.Inventory owner)
        {
            base.OnInitialize(owner);
        
            Entity = owner.GetComponent<BaseEntity>();
            Player = owner.GetComponent<Player>();
        }

        protected override void OnDeinitialize()
        {
            Entity = null;
            Player = null;

            base.OnDeinitialize();
        }

        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (!isLocalPlayer)
            {
                return;
            }
            
            if (Player != null)
            {
                Player.Hud.Mode = BaseHud.HudMode.Provision;
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }

            _countdownTimer = Instantiate(_countdownTimerPrefab, Loader.Instance.Canvas.transform);
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Hud.ResetSettings();
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            
                if (_countdownTimer != null)
                {
                    Destroy(_countdownTimer.gameObject);
                }
            }

            base.OnItemInactive();
        }

        public override void Use()
        {
            base.Use();

            if (!isLocalPlayer)
            {
                return;
            }

            if (IsCast)
            {
                return;
            }
            
            StartCast(_usageTime);

            if (isLocalPlayer && !isServer)
            {
                _cmdUseMedicalItem.Send();
            }
        }

        private void CmdUseMedicalItem()
        {
            if (!isServer)
            {
                return;
            }

            if (!IsCast)
            {
                StartCast(_usageTime);
            }
        }

        protected override void OnStartCast()
        {
            base.OnStartCast();

            if (Model != null)
            {
                Model.Use();
            }
            
            if (isLocalPlayer)
            {
                _countdownTimer.StartTimer("Usage", _usageTime);
            }
            
            if (isServer)
            {
                _rpcUseMedicalItem.Send();
            }
        }
        
        private void RpcUseMedicalItem()
        {
            if (isLocalPlayer || isServer)
            {
                return;
            }

            if (!IsCast)
            {
                StartCast(_usageTime);
            }
        }

        protected override void OnCastCompleted()
        {
            base.OnCastCompleted();

            if (!isServer)
            {
                return;
            }
        
            if (Entity != null)
            {
                Entity.TakeHeal(
                    new BaseEntity.DamageSenderInfo(Entity),
                    Mathf.Clamp(
                        _healthValue, 
                        0, 
                        Mathf.Clamp(_healthLimit - Entity.Health, 0, Entity.MaxHealth)));
            
                Quantity--;
            }
            else
            {
                Debug.LogWarning("Entity is null!");
            }
        }
    }
}
