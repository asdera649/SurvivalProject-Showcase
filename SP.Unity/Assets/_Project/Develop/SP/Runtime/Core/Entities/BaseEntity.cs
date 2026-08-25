using System;
using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Entities
{
    public class BaseEntity : NetworkBehaviour
    {
        #region Structs
        
        public class DamageSenderInfo
        {
            public DamageSenderInfo(BaseEntity sender)
            {
                Player.Player player = null;

                if (sender is Player.Player temp)
                {
                    player = temp;
                }
                
                if (player == null)
                {
                    IsPlayer = false;
                    PlayerUniqueId = "";
                }
                else
                {
                    IsPlayer = true;
                    PlayerUniqueId = player.UniqueId;
                }

                Entity = sender;
            }

            public bool IsPlayer { get; }
            public string PlayerUniqueId { get; }
            public BaseEntity Entity { get; }
        }
    
        private struct DamageHistory
        {
            public DamageHistory(DamageSenderInfo damageSender, int damageAmount)
            {
                DamageSender = damageSender;
                DamageAmount = damageAmount;
            }
            
            public DamageSenderInfo DamageSender { get; }
            public int DamageAmount { get; }
        }

        [Serializable]
        public class EntityStats
        {
            [FormerlySerializedAs("MaxHealth")] 
            [SerializeField] private int _maxHealth = 100;
            
            [FormerlySerializedAs("Health")] 
            [SerializeField] private int _health = 100;

            public void SetStats(BaseEntity entity)
            {
                entity.MaxHealth = _maxHealth;
                entity.Health = _health;
            }
        }
        
        #endregion

        public event UnityAction<int, int> HealthUpdated;
        public event UnityAction<DamageSenderInfo, int, DeathMethods> DamageTook;
        public event UnityAction<BaseEntity, DamageSenderInfo> Dead;
        public event UnityAction<BaseEntity> Destroyed;

        [Header("Settings")]
        [SerializeField] private EntityStats _entityStats;
        
        [FormerlySerializedAs("_invulnerable")] 
        [SerializeField] protected bool invulnerable;

        [SyncVar(hook = nameof(OnHealthUpdate)), SaveHandler.Saved] 
        private int _maxHealth;
        public int MaxHealth 
        { 
            get => _maxHealth;
            private set => _maxHealth = Mathf.Clamp(value, 0, int.MaxValue);
        }

        [SyncVar(hook = nameof(OnHealthUpdate)), SaveHandler.Saved] 
        private int _health;
        public int Health
        {
            get => _health;
            private set => _health = Mathf.Clamp(value, 0, MaxHealth);
        }

        public bool IsFullHealth => Health >= MaxHealth;

        private bool _isDead;
        
        private readonly List<DamageHistory> _damageHistory = new();
        
        protected virtual void Awake() 
        {
            _entityStats.SetStats(this);
        }

        public override void OnStopServer()
        {
            if (!MirrorUtils.IsShutdownProcessRunning)
            {
                Death();
            }
            
            base.OnStopServer();
        }

        protected virtual void OnDestroy()
        {
            Destroyed?.Invoke(this);
        }

        [ServerCallback]
        public void TakeDamage(DamageSenderInfo sender, int damage, DeathMethods deathMethod)
        {
            if (!CanTakeDamage())
            {
                return;
            }

            Health -= damage;
            
            _damageHistory.Add(new DamageHistory(sender, damage));

            DamageTook?.Invoke(sender, damage, deathMethod);

            if (Health == 0)
            {
                Death();
            }
        }

        [ServerCallback]
        private void Death()
        {
            if (_isDead)
            {
                return;
            }
            
            _isDead = true;

            Dead?.Invoke(
                this,
                _damageHistory.Count > 0 ? _damageHistory[^1].DamageSender : new DamageSenderInfo(null));
            
            _damageHistory.Clear();

            OnDeath();
            
            NetworkServer.Destroy(gameObject);
        }
        
        [ServerCallback]
        protected virtual void OnDeath()
        {
            
        }

        [ServerCallback]
        public void TakeHeal(DamageSenderInfo sender, int heal)
        {
            if (!CanTakeHeal())
            {
                return;
            }

            Health += heal;
        }

        #region Utilities

        private bool CanTakeDamage()
        {
            return !(invulnerable || _isDead);
        }
        
        private bool CanTakeHeal()
        {
            return !_isDead;
        }

        #endregion

        #region Callbacks

        private void OnHealthUpdate(int oldValue, int newValue)
        {
            HealthUpdated?.Invoke(oldValue, newValue);
        }
        
        #endregion
    }
}
