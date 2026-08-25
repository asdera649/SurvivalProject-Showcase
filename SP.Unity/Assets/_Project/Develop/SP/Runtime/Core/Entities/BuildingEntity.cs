using System;
using System.Collections.Generic;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece.Enums;
using FMODUnity;
using Mirror;
using SP.Runtime.Core.Entities.Buildings;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Core.Systems.Destruction;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Entities
{
    [RequireComponent(typeof(PieceBehaviour), typeof(SaveHandler))]
    public class BuildingEntity : BaseEntity
    {
        #region Structs
        
        [Serializable]
        public class UpgradeCostForUpkeepContainer
        {
            [SerializeField] private RequiredItem[] _costForUpkeep;
            public IReadOnlyList<RequiredItem> CostForUpkeep => _costForUpkeep;
        }
        
        #endregion

        public event UnityAction UpgradeAction;

        [Header("References")] 
        [SerializeField] private StudioEventEmitter _placementEventEmitter;
        [SerializeField] private StudioEventEmitter[] _upgradeEventEmitters;
        [SerializeField] private StudioEventEmitter[] _destructionEventEmitters;
        
        [Header("Settings")]
        [SerializeField] private EntityStats[] _upgradeStats;

        [SerializeField] private UpgradeCostForUpkeepContainer[] _upgradeCostForUpkeep;
        public IReadOnlyList<RequiredItem> UpgradeCostForUpkeep => _upgradeCostForUpkeep[_upgradeIndex].CostForUpkeep;
        
        [SerializeField] private bool _isRotting;
        [SerializeField] private int _rottingDamage = 11;
        [SerializeField] private float _rottingUpdateRateInSeconds = 60;
        
        [SyncVar(hook = nameof(OnUpgradeIndexUpdate)), SaveHandler.Saved] 
        private int _upgradeIndex;
        public int UpgradeIndex => _upgradeIndex;

        private bool IsRot => _protectiveCupboards.Count == 0;
        
        private readonly List<CupboardEntity> _protectiveCupboards = new();

        private bool _isDestructed;
        
        private PieceBehaviour _pieceBehaviour;
        public PieceBehaviour PieceBehaviour
        {
            get
            {
                if (_pieceBehaviour == null)
                {
                    _pieceBehaviour = GetComponent<PieceBehaviour>();
                }

                return _pieceBehaviour;
            }
        }
        
        private SaveHandler _saveHandler;
        public SaveHandler SaveHandler
        {
            get
            {
                if (_saveHandler == null)
                {
                    _saveHandler = GetComponent<SaveHandler>();
                }

                return _saveHandler;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            if (PieceBehaviour.CurrentState == StateType.Preview)
            {
                return;
            }
            
            if (_isRotting)
            {
                InvokeRepeating(nameof(UpdateBuildingRotting), _rottingUpdateRateInSeconds, _rottingUpdateRateInSeconds);
            }
        }

        public override void OnStopServer()
        {
            if (PieceBehaviour.CurrentState == StateType.Preview)
            {
                return;
            }
            
            CancelInvoke(nameof(UpdateBuildingRotting));

            ClearProtectiveCupboards();
            
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            OnUpgradeIndexUpdate(UpgradeIndex, UpgradeIndex);

            _isDestructed = false;
        }

        public override void OnStopClient()
        {
            if (PieceBehaviour.CurrentState == StateType.Preview)
            {
                return;
            }

            // Проблема(только для хоста): у Mirror есть пока не решенный нюанс(я думаю в будущем они это исправят),
            // OnStopClient вызывается каждый раз, когда мы вызываем NetworkServer.Destroy(), даже
            // если NetworkServer.Destroy() уже вызывался для этого объекта. Из за того что у нас
            // существует система стабильности построек которая каждый раз, когда уничтожается строение,
            // проверят связанные с ним строения на стабильность, и уничтожает если они нестабильны.
            // И таким образом OnStopClient для этого объекта вызовится столько, сколько у него существует
            // связанных с ним нестабильных строении(наверное).
            // Решение: думаю тут все понятно по коду.
            
            if (!_isDestructed)
            {
                Destruct();

                _isDestructed = true;
            }

            base.OnStopClient();
        }

        public virtual void Start()
        {
            if (PieceBehaviour.CurrentState == StateType.Preview)
            {
                return;
            }
            
            if (isServer)
            {
                RpcInvokePlacementImpacts();
            }
        }
        
        [ServerCallback]
        protected override void OnDeath()
        {
            base.OnDeath();

            // Если это хост то просто вызываем Destruct().
            // Все потому, что если у хоста перед уничтожением отправить Rpc,то он, не отправится.
            
            if (isServer && isClient)
            {
                InvokeDestructionImpacts(UpgradeIndex);
            }
            
            RpcInvokeDestructionImpacts(UpgradeIndex);
        }

        private void Destruct()
        {
            if (PieceBehaviour.Appearances.Count <= _upgradeIndex ||
                !PieceBehaviour.Appearances[_upgradeIndex].TryGetComponent(out DestructionHandler dh))
            {
                return;
            }
            
            dh.Destruct();
        }

        [ServerCallback]
        private void UpdateBuildingRotting()
        {
            if (!_isRotting)
            {
                return;
            }
            
            if (IsRot)
            {
                TakeDamage(new DamageSenderInfo(this), _rottingDamage, DeathMethods.Null);
            }
            else
            {
                TakeHeal(new DamageSenderInfo(this), _rottingDamage);
            }
        }
        
        [ClientRpc]
        private void RpcInvokePlacementImpacts()
        {
            _placementEventEmitter.Play();
        }
        
        [ClientRpc]
        private void RpcInvokeDestructionImpacts(int upgradeIndex)
        {
            InvokeDestructionImpacts(upgradeIndex);
        }

        private void InvokeDestructionImpacts(int upgradeIndex)
        {
            _destructionEventEmitters[upgradeIndex].Play();
        }
        
        public bool CanUpgrade(int upgradeIndex)
        {
            return _upgradeIndex < upgradeIndex && _upgradeStats.Length > upgradeIndex;
        }
        
        [ServerCallback]
        public void Upgrade(int upgradeIndex)
        {
            if (CanUpgrade(upgradeIndex))
            {
                _upgradeIndex = upgradeIndex;
                _upgradeStats[upgradeIndex].SetStats(this);

                RpcInvokeUpgradeImpacts(_upgradeIndex);
            }
        }

        [ClientRpc]
        private void RpcInvokeUpgradeImpacts(int upgradeIndex)
        {
            _upgradeEventEmitters[upgradeIndex].Play();
        }
        
        [ServerCallback]
        public void AddProtectiveCupboard(CupboardEntity cupboard)
        {
            if (!_protectiveCupboards.Contains(cupboard))
            {
                _protectiveCupboards.Add(cupboard);
            }
        }

        [ServerCallback]
        public void RemoveProtectiveCupboard(CupboardEntity cupboard)
        {
            if (_protectiveCupboards.Contains(cupboard))
            {
                _protectiveCupboards.Remove(cupboard);
            }
        }

        [ServerCallback]
        private void ClearProtectiveCupboards()
        {
            for (var i = _protectiveCupboards.Count - 1; i >= 0; i--)
            {
                RemoveProtectiveCupboard(_protectiveCupboards[i]);
            }
        }

        #region Callbacks
        
        private void OnUpgradeIndexUpdate(int oldValue, int newValue)
        {
            PieceBehaviour.AppearanceIndex = newValue;
            
            UpgradeAction?.Invoke();
        }
        
        #endregion
    }
}
