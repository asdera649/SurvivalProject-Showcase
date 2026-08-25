using System;
using Mirror;
using SP.Runtime.Core.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace SP.Runtime.Core.Entities.LootContainers
{
    public class DestructibleContainerEntity : ContainerEntity
    {
        private class HealthBar
        {
            public HealthBar(TextMeshPro healthBar)
            {
                _healthBar = healthBar;
                _healthBar.richText = true; 
                _startCharsCount = _healthBar.text.Length;
            }
            
            private readonly TextMeshPro _healthBar;
            private readonly int _startCharsCount;

            public void UpdateHealthBar(float percent01)
            {
                percent01 = Mathf.Clamp01(percent01);
                
                var whiteCharsCount = Mathf.RoundToInt(_startCharsCount * percent01);
                var blackCharsCount = _startCharsCount - whiteCharsCount;
                
                var whitePart = whiteCharsCount > 0 ? $"<color=white>{new string('|', whiteCharsCount)}</color>" : "";
                var blackPart = blackCharsCount > 0 ? $"<color=#0d0d0d>{new string('|', blackCharsCount)}</color>" : "";
                
                _healthBar.text = whitePart + blackPart;
            }
        }
        
        [Header("References")] 
        [SerializeField] private LocalizeStringEvent _unlockedTextMeshLocalize;
        [SerializeField] private TextMeshPro _healthBarTextMesh;

        [Header("Settings")] 
        [SerializeField] private float _healthBarHideDelay = 3;

        [SyncVar(hook = nameof(OnLockUpdated))] private bool _isLock = true;

        private float _time;

        private HealthBar _healthBar;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            DamageTook += OnDamageTook;
        }

        public override void OnStopServer()
        {
            DamageTook -= OnDamageTook;
            
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _healthBar = new  HealthBar(_healthBarTextMesh);
            
            HealthUpdated += OnHealthUpdated;
            
            OnLockUpdated(_isLock, _isLock);
        }

        public override void OnStopClient()
        {
            HealthUpdated -= OnHealthUpdated;
            
            base.OnStopClient();
        }

        private void Update()
        {
            if (_time > 0)
            {
                _time -= Time.deltaTime;
                
                return;
            }
            
            _healthBarTextMesh.gameObject.SetActive(false);
        }

        [ServerCallback]
        protected override void OnFill()
        {
            Inventory.Clear();
            
            SetLock(true);
        }
        
        [ClientCallback]
        protected override void OnExecution()
        {
            if (_isLock)
            {
                Debug.LogWarning("Показать подсказку!");
            }
            else
            {
                base.OnExecution();
            }
        }

        [ServerCallback]
        private void SetLock(bool value)
        {
            _isLock = value;
            invulnerable = !value;
        }
        
        #region Callbacks

        [ClientCallback]
        private void OnLockUpdated(bool _, bool newValue)
        {
            _unlockedTextMeshLocalize.gameObject.SetActive(!newValue);
            _healthBarTextMesh.gameObject.SetActive(newValue);
            InteractionHandler.HoldMode = !newValue;
        }

        [ClientCallback]
        private void OnHealthUpdated(int oldValue, int newValue)
        {
            if (_healthBar != null)
            {
                _time = _healthBarHideDelay;
                
                _healthBar.UpdateHealthBar((float)newValue / MaxHealth);

                _healthBarTextMesh.gameObject.SetActive(true);
            }
        }

        [ServerCallback]
        private void OnDamageTook(DamageSenderInfo damageSenderInfo, int damage, DeathMethods deathMethods)
        {
            if (Health <= 0)
            {
                TakeHeal(new DamageSenderInfo(this), MaxHealth);
                Fill();
                SetLock(false);
            }
        }
        
        [ClientCallback]
        protected override void OnRemainingTimeToRestoreUpdated(float oldValue, float newValue)
        {
            base.OnRemainingTimeToRestoreUpdated(oldValue, newValue);
            
            var timeSpan = TimeSpan.FromSeconds(newValue);
            
            _unlockedTextMeshLocalize.StringReference["minutes"] = new StringVariable
            {
                Value = timeSpan.Minutes.ToString()
            };
            
            _unlockedTextMeshLocalize.StringReference["seconds"] = new StringVariable
            {
                Value = timeSpan.Seconds.ToString("0#")
            };
            
            _unlockedTextMeshLocalize.RefreshString();
        }
        
        #endregion
    }
}
