using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Core.Services.TimeService;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using SP.Runtime.Core.UI.Notification;
using SP.Runtime.Core.Utilities;
using UnityEngine;
using UnityEngine.Events;
using VContainer;

namespace SP.Runtime.Core.Entities.Buildings.CupboardEntity
{
    public class CupboardEntity : LootableEntity<CupboardInventoryBlock>
    {
        #region Structs
        
        public enum Reason
        {
            None,
            TerritoryIsClaimed,
            UnderAttack
        }
    
        #endregion
        
        public event UnityAction AuthorizedEntitiesUpdated;

        [Header("References")]
        [SerializeField] private CupboardArea _cupboardArea;
        public CupboardArea CupboardArea => _cupboardArea;
        
        [Header("Settings")]
        [SerializeField] private float _raidBlockTime = 300;
        [SerializeField] [Range(0, 1)] private float _percentageCostForUpkeep = 0.14f;
        
        [SerializeField] private float _cupboardUpdateRateInSeconds = 600;
        public float CupboardUpdateRateInSeconds => _cupboardUpdateRateInSeconds;

        [SyncVar] private float _raidBlock;
        public float RaidBlockTime => _raidBlock;

        public bool IsRaidBlock => _raidBlock > 0;

        [SyncVar] private bool _isRottingProtection;
        private bool IsRottingProtection 
        { 
            set
            {
                var temp = _isRottingProtection;
                _isRottingProtection = value;

                OnRottingProtectionUpdate(temp, _isRottingProtection);
            }
        }

        private readonly SyncList<string> _authorizedEntities = new();
        
        // Wrapper нужен для системы сохранения(SaveHandler)
        [SaveHandler.Saved]
        private IReadOnlyList<string> AuthorizedEntitiesWrapper 
        { 
            get => _authorizedEntities;
            set
            {
                _authorizedEntities.Reset(); 
                _authorizedEntities.AddRange(value);
            } 
        }

        [SaveHandler.Saved]
        [SyncVar] private float _placementTime;
        private float PlacementTime => _placementTime;

        private TimeService _timeService;

        [Inject]
        private void Inject(TimeService timeService)
        {
            _timeService = timeService;
        }
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            
            _cupboardArea.TargetBuildingDestroyed += OnBuildingDestroy;

            // Поставил время первого вызова OnCupboardRottingUpdate - 1 секунда
            // специально что бы в CupboardArea успел вызватся OnTriggerEnter
            InvokeRepeating(nameof(UpdateCupboardRotting), 1, _cupboardUpdateRateInSeconds);

            // Проверяем на нулевое значение, что бы не перезаписать _placementTime,
            // после загрузки сохранения.
            if (_placementTime == 0)
            {
                _placementTime = _timeService.Time;
            }
        }

        public override void OnStopServer()
        {
            _cupboardArea.TargetBuildingDestroyed -= OnBuildingDestroy;
            
            CancelInvoke(nameof(UpdateCupboardRotting));

            // Убираем защиту шкафа со всех строений
            IsRottingProtection = false;
            
            base.OnStopServer();
        }
        
        protected virtual void OnEnable()
        {
            _authorizedEntities.Callback += OnAuthorizedEntitiesUpdate;
        }

        public override void Start()
        {
            base.Start();
            
            for (var i = 0; i < _authorizedEntities.Count; i++)
            {
                OnAuthorizedEntitiesUpdate(SyncList<string>.Operation.OP_ADD, i, null, _authorizedEntities[i]);
            }
        }

        private void OnDisable()
        {
            _authorizedEntities.Callback -= OnAuthorizedEntitiesUpdate;
        }
        
        private void Update()
        {
            UpdateRaidBlock();
        }

        [ServerCallback]
        private void UpdateRaidBlock()
        {
            _raidBlock -= Time.deltaTime;
        }

        [ServerCallback]
        private void UpdateCupboardRotting()
        {
            var isContains = false;
            var requiredItems = GetCostForUpkeep();
            
            Foo:

            foreach (var i in requiredItems)
            {
                if (!isContains)
                {
                    if (!Inventory.Contains(i.Item, i.Quantity))
                    {
                        IsRottingProtection = false;
                        return;
                    }
                }
                else
                {
                    Inventory.SilentRemove(i.Item, i.Quantity);
                }
            }

            if (!isContains)
            {
                isContains = true;
                
                goto Foo;
            }

            IsRottingProtection = true;
        }
        
        [ClientCallback]
        public void LogIn()
        {
            CmdLogIn();
        }

        [ClientCallback]
        public void ClearLoggedIn()
        {
            CmdClearLoggedIn();
        }

        [Command(requiresAuthority = false)]
        private void CmdLogIn(NetworkConnectionToClient sender = null)
        {
            if (sender == null || !sender.identity.TryGetComponent(out Player.Player player))
            {
                return;
            }

            AddUniqueId(player.UniqueId);
        }

        [Command(requiresAuthority = false)]
        private void CmdClearLoggedIn(NetworkConnectionToClient sender = null)
        {
            if (sender == null || !sender.identity.TryGetComponent(out Player.Player player))
            {
                return;
            }

            ClearUniqueIds(player.UniqueId);
        }
        
        public bool ContainsUniqueId(string uniqueId)
        {
            return _authorizedEntities.Contains(uniqueId);
        }

        [ServerCallback]
        private void AddUniqueId(string uniqueId)
        {
            if (ContainsUniqueId(uniqueId))
            {
                return;
            }
            
            _authorizedEntities.Add(uniqueId);
        }

        [ServerCallback]
        private void ClearUniqueIds(string uniqueId)
        {
            if (!ContainsUniqueId(uniqueId))
            {
                return;
            }

            // Можно было бы использовать Clear, но в Mirror с этим беда пока что(
            for (var i = _authorizedEntities.Count - 1; i >= 0; i--)
            {
                _authorizedEntities.RemoveAt(i);
            }
        }
        
        #region Utilities

        public IReadOnlyList<RequiredItem> GetCostForUpkeep()
        {
            List<RequiredItem> outputs = new();
            
            var buildings = _cupboardArea.TargetBuildings;

            foreach (var b in buildings)
            {
                var cost = b.UpgradeCostForUpkeep;

                foreach (var c in cost)
                {
                    var index = outputs.FindIndex((x) => x.Item.Equals(c.Item));

                    if (index != -1)
                    {
                        outputs[index] = new RequiredItem(outputs[index].Item, outputs[index].Quantity + c.Quantity);
                    }
                    else
                    {
                        outputs.Add(new RequiredItem(c.Item, c.Quantity));
                    }
                }
            }

            for (var i = 0; i < outputs.Count; i++)
            {
                var value = (int)Mathf.Round(outputs[i].Quantity * _percentageCostForUpkeep);
                
                outputs[i] = new RequiredItem(outputs[i].Item, Mathf.Clamp(value, 1, int.MaxValue));
            }

            return outputs;
        }

        public float GetProtectionTime()
        {
            var output = 0f;
            
            var time = float.MaxValue;
            
            var requiredItems = GetCostForUpkeep();

            foreach (var i in requiredItems)
            {
                var value = (float)Inventory.GetTotal(i.Item) / i.Quantity * _cupboardUpdateRateInSeconds;

                if (value < time)
                {
                    output = value;
                    time = value;
                }
            }

            return output;
        }

        #endregion

        #region Callbacks

        [ServerCallback]
        private void OnRottingProtectionUpdate(bool oldValue, bool newValue)
        {
            var buildings = _cupboardArea.TargetBuildings;

            foreach (var b in buildings)
            {
                if (newValue)
                {
                    b.AddProtectiveCupboard(this);
                }
                else
                {
                    b.RemoveProtectiveCupboard(this);
                }
            }
        }
        
        [ServerCallback]
        private void OnBuildingDestroy(BaseEntity entity, DamageSenderInfo guilty)
        {
            // if (guilty.IsPlayer && !ContainsUniqueId(guilty.PlayerUniqueId))
            // {
            //     _raidBlock = _raidBlockTime;
            // }
            
            // Новое условие для установки рейд-блока. Более правильный вариант.
            
            if (guilty.IsPlayer &&
                !CheckAuthorization(guilty.PlayerUniqueId, entity.transform.position))
            {
                _raidBlock = _raidBlockTime;
            }
        }

        private void OnAuthorizedEntitiesUpdate(SyncList<string>.Operation op, int index, string oldValue, string newValue)
        {
            AuthorizedEntitiesUpdated?.Invoke();
        }
        
        #endregion
        
        #region Statics

        public static bool ComprehensiveCheck(Player.Player player, Vector3 position)
        {
            var result = ComprehensiveCheck(player.UniqueId, position, out var reason);

            switch (reason)
            {
                case Reason.UnderAttack:
                {
                    player.SendNotification("{UnderAttack}", NotificationType.Warning);
                    break;
                }
                case Reason.TerritoryIsClaimed:
                {
                    player.SendNotification("{TerritoryIsClaimed}", NotificationType.Warning);
                    break;
                }
            }
            
            return result;
        }
    
        public static bool ComprehensiveCheck(string playerUniqueId, Vector3 position, out Reason reason)
        {
            reason = Reason.None;
            
            if (!CheckRaidBlock(position))
            {
                reason = Reason.UnderAttack;
                return false;
            }
            
            if (!CheckAuthorization(playerUniqueId, position))
            {
                reason = Reason.TerritoryIsClaimed;
                return false;
            }
        
            return true;
        }
        
        public static bool CheckRaidBlock(Player.Player player, Vector3 position)
        {
            if (!CheckRaidBlock(position))
            {
                player.SendNotification("{UnderAttack}", NotificationType.Warning);
                return false;
            }
        
            return true;
        }

        public static bool CheckRaidBlock(Vector3 position)
        {
            var cupboardEntities = GetCupboards(position);

            foreach (var c in cupboardEntities)
            {
                if (c.IsRaidBlock)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckAuthorization(Player.Player player, Vector3 position)
        {
            if (!CheckAuthorization(player.UniqueId, position))
            {
                player.SendNotification("{TerritoryIsClaimed}", NotificationType.Warning);
                return false;
            }
        
            return true;
        }
        
        public static bool CheckAuthorization(string playerUniqueId, Vector3 position)
        {
            var cupboardEntities = GetCupboards(position);

            CupboardEntity targetCupboard = null;

            var currentPlacementTime = float.MaxValue;
            
            foreach (var c in cupboardEntities)
            {
                if (c.PlacementTime < currentPlacementTime)
                {
                    targetCupboard = c;
                    currentPlacementTime = c.PlacementTime;
                }
            }
            
            if (targetCupboard != null && !targetCupboard.ContainsUniqueId(playerUniqueId))
            {
                return false;
            }

            return true;
        }

        public static IReadOnlyList<CupboardEntity> GetCupboards(Vector3 position)
        {
            List<CupboardEntity> outputs = new();
            
            var areas = PhysicUtils.GetTypesBySphere<CupboardArea>(
                position,
                0.01f,
                -1,
                QueryTriggerInteraction.Collide);

            foreach (var a in areas)
            {
                outputs.Add(a.CupboardEntity);
            }
            
            return outputs;
        }
    
        #endregion
    }
}
