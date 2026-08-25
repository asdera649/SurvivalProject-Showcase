using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using QuickOutline.Scripts;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Core.Systems.Hiding;
using SP.Runtime.Core.Systems.Interaction;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.Systems.ValueContainer;
using SP.Runtime.Core.UI.Building;
using SP.Runtime.Core.UI.Notification;
using SP.Runtime.Core.UI.UIControls.RadialMenu;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Items.BuildingRepairItem
{
    [CreateAssetMenu(menuName = "Items/BuildingRepairItem")]
    public class BuildingRepairItem : BaseItem
    {
        #region Structs
        
        [Serializable]
        public class RequiredItemsContainer
        {
            [SerializeField] private RequiredItem[] _requiredItems;
            public RequiredItem[] RequiredItems => _requiredItems;
        }
        
        [Serializable]
        public class BuildingCostContainer
        {
            [SerializeField] private BuildingEntity _building;
            public BuildingEntity Building => _building;
            
            [SerializeField] private RequiredItemsContainer[] _requiredItems;
            public RequiredItemsContainer[] RequiredItems => _requiredItems;
        }

        [Serializable]
        public class BuildingsCostsContainer
        {
            [SerializeField] private BuildingCostContainer[] _buildings;

            public bool TryGetHealthDependentBuildingCost(BuildingEntity building, out IReadOnlyList<RequiredItem> output)
            {
                List<RequiredItem> temp = null;

                if (TryGetBuildingCost(building, building.UpgradeIndex, out var requiredItems))
                {
                    temp = new List<RequiredItem>();

                    foreach (var i in requiredItems)
                    {
                        var value = (int)Mathf.Lerp(1, i.Quantity, 1 - (float)building.Health / building.MaxHealth);
                        
                        temp.Add(new RequiredItem(i.Item, Mathf.Clamp(value, 1, i.Quantity)));
                    }
                }

                output = temp;
                
                return output != null;
            }
            
            public bool TryGetBuildingCost(BuildingEntity building, out IReadOnlyList<RequiredItem> output)
            {
                output = null;

                TryGetBuildingCost(building, building.UpgradeIndex, out output);
                
                return output != null;
            }
            
            public bool TryGetBuildingCost(BuildingEntity building, int upgradeIndex, out IReadOnlyList<RequiredItem> output)
            {
                List<RequiredItem> temp = null;

                foreach (var b in _buildings)
                {
                    if (building.GetType() == b.Building.GetType())
                    {
                        if (b.RequiredItems.Length > upgradeIndex)
                        {
                            temp = new List<RequiredItem>(b.RequiredItems[upgradeIndex].RequiredItems);
                            break;
                        }
                    }
                }

                output = temp;
                
                return output != null;
            }
        }

        [Serializable]
        public class BuildingsContainer
        {
            [SerializeField] private BuildingEntity[] _buildings;

            public bool ContainsBuilding(BuildingEntity building)
            {
                foreach (var b in _buildings)
                {
                    if (building.GetType() == b.GetType())
                    {
                        return true;
                    }
                }
                
                return false;
            }
        }


        #endregion
        
        private readonly CustomCommand _cmdDestroyBuilding = new(nameof(CmdDestroyBuilding));
        private readonly CustomCommand _cmdRepairBuilding = new(nameof(CmdRepairBuilding));
        private readonly CustomCommand _cmdUpgradeBuildingToStone = new(nameof(CmdUpgradeBuildingToStone));
        private readonly CustomCommand _cmdUpgradeBuildingToMetal = new(nameof(CmdUpgradeBuildingToMetal));
    
        [Header("Prefabs(BuildingRepairItem)")]
        [SerializeField] private BuildingStats _buildingStatsPrefab;
        
        [Header("References(BuildingRepairItem)")]
        [SerializeField] private Sprite _destructionIcon;
        [SerializeField] private Sprite _repairIcon;
        [SerializeField] private Sprite _upgradeToStoneIcon;
        [SerializeField] private Sprite _upgradeToMetalIcon;
        
        [Header("Settings(BuildingRepairItem)")]
        [SerializeField] private BuildingsContainer _informableBuildings;
        [SerializeField] private BuildingsContainer _destructibleBuildings;
        [SerializeField] private BuildingsCostsContainer _repairableBuildings;
        [SerializeField] private BuildingsCostsContainer _upgradebleBuildings;

        [SerializeField] private float _sphereCastRadius = 0.1f;
        [SerializeField] private float _searchRadius = 3;
        [SerializeField] private Vector3 _offset = Vector3.up;
        [SerializeField] private float _updateRate = 0.1f;

        private Vector3 Position => Transform.position + _offset;
        
        private BuildingEntity _focusBuilding;
        private BuildingEntity FocusBuilding
        {
            get => _focusBuilding;
            set
            {
                var oldValue = _focusBuilding;
                _focusBuilding = value;
                
                OnFocusBuildingUpdate(oldValue, _focusBuilding);
            }
        }

        private bool _isFocusBuildingProtected;

        private const string _destroyNameEntry = "{Destroy}";
        private const string _destroyDescriptionEntry = "{DestroyTheObject}";
        private const string _repairNameEntry = "{Repair}";
        private const string _upgradeToStoneNameEntry = "{UpgradeToStone}";
        private const string _upgradeToMetalNameEntry = "{UpgradeToMetal}";
        
        private const int _stoneUpgradeIndex = 1;
        private const int _metalUpgradeIndex = 2;
        
        private float _currentTime;
        
        private readonly Dictionary<RadialMenuButton, BuildingEntity> _createdButtons = new ();
        
        private Container<InteractionSeeker.InteractionLayer>.Element _layerElement;

        private BuildingStats _buildingStats;
    
        private Player Player { set; get; }
        private InteractionSeeker InteractionSeeker { set; get; }

        protected override void OnInitialize(Inventory owner)
        {
            base.OnInitialize(owner);
        
            Player = owner.GetComponent<Player>();
            InteractionSeeker = owner.GetComponent<InteractionSeeker>();
        }

        protected override void OnDeinitialize()
        {
            Player = null;
            InteractionSeeker = null;

            base.OnDeinitialize();
        }
        
        protected override void OnItemActive()
        {
            base.OnItemActive();

            if (!isLocalPlayer)
            {
                return;
            }
        
            if (InteractionSeeker != null)
            {
                _layerElement = InteractionSeeker.AddLayer(InteractionSeeker.InteractionLayer.Building);

                foreach (var h in InteractionSeeker.InvokedHandlers)
                {
                    OnInvokeInteraction(h);
                }

                InteractionSeeker.InvokeAction += OnInvokeInteraction;
                InteractionSeeker.DismissAction += OnDismissInteraction;
            }
            else
            {
                Debug.LogWarning("Interaction seeker is null!");
            }

            if (Player != null)
            {
                if (Player.TryGetComponent<Hider>(out var hider))
                {
                    hider.HidingMode = Hider.HandlersHidingMode.NotFull;
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }

            Inventory.InventoryUpdated += OnInventoryUpdate;
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (InteractionSeeker != null)
                {
                    InteractionSeeker.InvokeAction -= OnInvokeInteraction;
                    InteractionSeeker.DismissAction -= OnDismissInteraction;
                    
                    foreach (var h in InteractionSeeker.InvokedHandlers)
                    {
                        OnDismissInteraction(h);
                    }

                    if (_layerElement != null)
                    {
                        InteractionSeeker.RemoveLayer(_layerElement);
                        _layerElement = null;
                    }
                }
                else
                {
                    Debug.LogWarning("Interaction seeker is null!");
                }

                if (Player != null)
                {
                    if (Player.TryGetComponent<Hider>(out var hider))
                    {
                        hider.HidingMode = Hider.HandlersHidingMode.Full;
                    }
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
                
                Inventory.InventoryUpdated -= OnInventoryUpdate;

                FocusBuilding = null;
                
                _createdButtons.Clear();
                
                UpdateBuildingStats();
            }

            base.OnItemInactive();
        }

        #region FocusBuildingUpdate
    
        public override void OnUpdate()
        {
            base.OnUpdate();

            UpdateFocusBuildingInformation();
        }

        private void UpdateFocusBuildingInformation()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (!IsActive)
            {
                return;
            }
        
            _currentTime += Time.deltaTime;

            if (_currentTime > _updateRate)
            {
                UpdateFocusBuilding();
                UpdateFocusBuildingProtectionStatus();
                _currentTime = 0;
            }

            UpdateBuildingStats();
        }
        
        private void UpdateFocusBuilding()
        {
            var results = PhysicUtils.GetBySphere<BuildingEntity>(
                Position,
                _searchRadius,
                -1,
                QueryTriggerInteraction.Ignore);

            BuildingEntity focusBuilding = null;
            
            var currentDistance = float.MaxValue;
            
            foreach (var r in results)
            {
                if (r.Object.TryGetComponent<BuildingFocusOverride>(out var focusOverride))
                {
                    foreach (var a in focusOverride.Anchors)
                    {
                        var distance = Vector3.Distance(Position, a.transform.position);
                
                        if (distance < currentDistance)
                        {
                            focusBuilding = r.Object;
                            currentDistance = distance;
                        }
                    }
                }
                else
                {
                    var distance = GetDistance(r);
                
                    if (distance < currentDistance)
                    {
                        focusBuilding = r.Object;
                        currentDistance = distance;
                    }   
                }
            }

            if (FocusBuilding != focusBuilding)
            {
                if (focusBuilding != null && !_informableBuildings.ContainsBuilding(focusBuilding))
                {
                    focusBuilding = null;
                }

                FocusBuilding = focusBuilding;
            }
        }

        private float GetDistance(PhysicUtils.Result<BuildingEntity> result)
        {
            var output = float.MaxValue;

            foreach (var c in result.Colliders)
            {
                var distance = float.MaxValue;

                if (c is MeshCollider { convex: false })
                {
                    var castResults = PhysicUtils.GetSphereCastAll<BuildingEntity>(
                        Position,
                        _sphereCastRadius,
                        result.GetClosestPoint(Position) - Position,
                        _searchRadius,
                        -1,
                        QueryTriggerInteraction.Ignore);

                    if (castResults.Count > 0 && castResults.First().Object == result.Object)
                    {
                        distance = castResults.First().Hits.First().distance;
                    }
                }
                else
                {
                    distance = Vector3.Distance(Position, PhysicUtils.GetClosestPoint(c, Position));
                }

                if (distance < output)
                {
                    output = distance;
                }
            }
            
            return output;
        }

        private void UpdateFocusBuildingProtectionStatus()
        {
            _isFocusBuildingProtected = 
                FocusBuilding != null &&
                CupboardEntity.GetCupboards(FocusBuilding.transform.position).Count > 0;
        }

        private void UpdateBuildingStats()
        {
            if (FocusBuilding != null)
            {
                Vector3 position = RectTransformUtility.WorldToScreenPoint(
                    Loader.Instance.MainCamera.Camera,
                    ObjectUtils.CalculateCenter(FocusBuilding.transform));

                if (_buildingStats == null)
                {
                    _buildingStats = Instantiate(
                            _buildingStatsPrefab,
                            position, 
                            Quaternion.identity,
                            Loader.Instance.Canvas.transform);
                    
                    _buildingStats.transform.SetAsFirstSibling();
                }
                
                _buildingStats.transform.position = position;
                _buildingStats.Initialize(FocusBuilding.Health, FocusBuilding.MaxHealth, _isFocusBuildingProtected);
            }
            else if (_buildingStats != null)
            {
                Destroy(_buildingStats.gameObject);
                _buildingStats = null;
            }
        }

        #endregion

        private void OnInvokeInteraction(InteractionHandler handler)
        {
            if (!handler.TryGetComponent<BuildingEntity>(out var building))
            {
                return;
            }

            RadialMenuButton radialMenuButton;
        
            if (_destructibleBuildings.ContainsBuilding(building))
            {
                radialMenuButton = handler.InteractionButton.RadialMenu.InstantiateButton(
                    _destroyNameEntry,
                    _destroyDescriptionEntry,
                    _destructionIcon);
            
                radialMenuButton.ExecuteAction += OnDestroyButtonExecute;
                
                _createdButtons.Add(radialMenuButton, building);
            }
        
            if (_repairableBuildings.TryGetBuildingCost(building, out _))
            {
                radialMenuButton = handler.InteractionButton.RadialMenu.InstantiateButton(
                    _repairNameEntry, 
                    null, 
                    _repairIcon);
                
                radialMenuButton.ExecuteAction += OnRepairButtonExecute;
                
                _createdButtons.Add(radialMenuButton, building);
            }
        
            if (_upgradebleBuildings.TryGetBuildingCost(building, _stoneUpgradeIndex, out _))
            {
                radialMenuButton = handler.InteractionButton.RadialMenu.InstantiateButton(
                    _upgradeToStoneNameEntry,
                    null, 
                    _upgradeToStoneIcon);
                
                radialMenuButton.ExecuteAction += OnUpgradeToStoneButtonExecute;
                
                _createdButtons.Add(radialMenuButton, building);
            }
        
            if (_upgradebleBuildings.TryGetBuildingCost(building, _metalUpgradeIndex, out _))
            {
                radialMenuButton = handler.InteractionButton.RadialMenu.InstantiateButton(
                    _upgradeToMetalNameEntry,
                    null,
                    _upgradeToMetalIcon);
                
                radialMenuButton.ExecuteAction += OnUpgradeToMetalButtonExecute;
                
                _createdButtons.Add(radialMenuButton, building);
            }
        
            building.HealthUpdated += OnBuildingHealthUpdate;
            building.UpgradeAction += OnBuildingUpgrade;
            
            UpdateRadialMenuButtons();
        }

        private void OnDismissInteraction(InteractionHandler handler)
        {
            if (!handler.TryGetComponent<BuildingEntity>(out var building))
            {
                return;
            }

            for (var i = _createdButtons.Count - 1; i >= 0; i--)
            {
                if (_createdButtons.ElementAt(i).Value == building)
                {
                    handler.InteractionButton.RadialMenu.DestroyButton(_createdButtons.ElementAt(i).Key);
                    _createdButtons.Remove(_createdButtons.ElementAt(i).Key);
                }
            }

            building.HealthUpdated -= OnBuildingHealthUpdate;
            building.UpgradeAction -= OnBuildingUpgrade;
        }
        
        private void UpdateRadialMenuButtons()
        {
            foreach (var b in _createdButtons)
            {
                IReadOnlyList<RequiredItem> requiredItems = Array.Empty<RequiredItem>();
            
                switch (b.Key.NameEntry)
                {
                    case _destroyNameEntry:
                    {
                        continue;
                    }
                    case _repairNameEntry:
                    {
                        if (_repairableBuildings.TryGetHealthDependentBuildingCost(b.Value, out var output))
                        {
                            requiredItems = output;
                        }
                        
                        b.Key.Interactable = !b.Value.IsFullHealth;
                        
                        break;
                    }
                    case _upgradeToStoneNameEntry:
                    {
                        if (_upgradebleBuildings.TryGetBuildingCost(b.Value, _stoneUpgradeIndex, out var output))
                        {
                            requiredItems = output;
                        }
                        
                        b.Key.Interactable = b.Value.CanUpgrade(1);
                        
                        break;
                    }
                    case _upgradeToMetalNameEntry:
                    {
                        if (_upgradebleBuildings.TryGetBuildingCost(b.Value, _metalUpgradeIndex, out var output))
                        {
                            requiredItems = output;
                        }
                        
                        b.Key.Interactable = b.Value.CanUpgrade(2);
                        
                        break;
                    }
                }
        
                b.Key.DescriptionEntry = GetCostText(requiredItems);
            }
        }

        private string GetCostText(IReadOnlyList<RequiredItem> requiredItems)
        {
            var output = "";
    
            foreach (var i in requiredItems)
            {
                output += "{" + i.Item.Name + "} -" + i.Quantity;

                if (Inventory.Contains(i.Item, i.Quantity))
                {
                    output += " (x" + Inventory.GetTotal(i.Item) + ") </color>\n";
                }
                else
                {
                    output += "<color=red> (x" + Inventory.GetTotal(i.Item) + ") </color>\n";
                }
            }
    
            return output;
        }
        
        private bool ContainsItems(IReadOnlyList<RequiredItem> requiredItems)
        {
            foreach (var i in requiredItems)
            {
                if (!Inventory.Contains(i.Item, i.Quantity))
                {
                    return false;
                }
            }

            return true;
        }
        
        #region Destroy
        
        private void OnDestroyButtonExecute(RadialMenuButton button)
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (_createdButtons.TryGetValue(button, out var building))
            {
                if (building.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    _cmdDestroyBuilding.Send(identity.netId);
                }
            }
        }

        private bool CanDestroy(BuildingEntity building)
        {
            if (Player != null)
            {
                if (!CupboardEntity.ComprehensiveCheck(Player, building.transform.position))
                {
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
                return false;
            }

            if (!_destructibleBuildings.ContainsBuilding(building))
            {
                return false;
            }

            return true;
        }
        
        private void CmdDestroyBuilding(uint netId)
        {
            if (!isServer)
            {
                return;
            }

            if (Player != null)
            {
                if (NetworkServer.spawned.TryGetValue(netId, out var identity))
                {
                    if (identity.TryGetComponent<BuildingEntity>(out var building) && CanDestroy(building))
                    {
                        building.TakeDamage(
                            new BaseEntity.DamageSenderInfo(Player),
                            building.MaxHealth,
                            DeathMethods.Null);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }
        }

        #endregion

        #region Repair

        private void OnRepairButtonExecute(RadialMenuButton button)
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (_createdButtons.TryGetValue(button, out var building))
            {
                if (building.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    _cmdRepairBuilding.Send(identity.netId);
                }
            }
        }

        private bool CanRepair(BuildingEntity building)
        {
            if (Player != null)
            {
                if (!CupboardEntity.ComprehensiveCheck(Player, building.transform.position))
                {
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
                return false;
            }

            if (_repairableBuildings.TryGetHealthDependentBuildingCost(building, out var requiredItems))
            {
                if (!ContainsItems(requiredItems))
                {
                    Player.SendNotification("{NotEnoughResources}", NotificationType.Warning);
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (building.IsFullHealth)
            {
                return false;
            }

            return true;
        }

        private void CmdRepairBuilding(uint netId)
        {
            if (!isServer)
            {
                return;
            }

            if (Player != null)
            {
                if (NetworkServer.spawned.TryGetValue(netId, out var identity))
                {
                    if (identity.TryGetComponent<BuildingEntity>(out var building) && CanRepair(building))
                    {
                        if (_repairableBuildings.TryGetHealthDependentBuildingCost(building, out var requiredItems))
                        {
                            foreach (var i in requiredItems)
                            {
                                Inventory.Remove(i.Item, i.Quantity);
                            }
                        }
                        
                        building.TakeHeal(new BaseEntity.DamageSenderInfo(Player), building.MaxHealth);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }
        }

        #endregion
        
        #region Upgrade
        
        private bool CanUpgrade(BuildingEntity building, int upgradeIndex)
        {
            if (Player != null)
            {
                if (!CupboardEntity.ComprehensiveCheck(Player, building.transform.position))
                {
                    return false;
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
                return false;
            }

            if (_upgradebleBuildings.TryGetBuildingCost(building, upgradeIndex, out var requiredItems))
            {
                if (!ContainsItems(requiredItems))
                {
                    Player.SendNotification("{NotEnoughResources}", NotificationType.Warning);
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (!building.CanUpgrade(upgradeIndex))
            {
                return false;
            }

            return true;
        }

        private void UpgradeBuilding(BuildingEntity building, int upgradeIndex)
        {
            if (Player != null)
            {
                if (CanUpgrade(building, upgradeIndex))
                {
                    building.Upgrade(upgradeIndex);
                    
                    if (_upgradebleBuildings.TryGetBuildingCost(building, upgradeIndex, out var requiredItems))
                    {
                        foreach (var i in requiredItems)
                        {
                            Inventory.Remove(i.Item, i.Quantity);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player is null!");
            }
        }

        #region UpgradeToStone

        private void OnUpgradeToStoneButtonExecute(RadialMenuButton button)
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (_createdButtons.TryGetValue(button, out var building))
            {
                if (building.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    _cmdUpgradeBuildingToStone.Send(identity.netId);
                }
            }
        }

        private void CmdUpgradeBuildingToStone(uint netId)
        {
            if (!isServer)
            {
                return;
            }

            if (NetworkServer.spawned.TryGetValue(netId, out var identity))
            {
                if (identity.TryGetComponent<BuildingEntity>(out var building))
                {
                    UpgradeBuilding(building, _stoneUpgradeIndex);
                }
            }
        }

        #endregion

        #region UpgradeToMetal

        private void OnUpgradeToMetalButtonExecute(RadialMenuButton button)
        {
            if (!isLocalPlayer)
            {
                return;
            }
        
            if (_createdButtons.TryGetValue(button, out var building))
            {
                if (building.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    _cmdUpgradeBuildingToMetal.Send(identity.netId);
                }
            }
        }

        private void CmdUpgradeBuildingToMetal(uint netId)
        {
            if (!isServer)
            {
                return;
            }

            if (NetworkServer.spawned.TryGetValue(netId, out var identity))
            {
                if (identity.TryGetComponent<BuildingEntity>(out var building))
                {
                    UpgradeBuilding(building, _metalUpgradeIndex);
                }
            }
        }

        #endregion
        
        #endregion

        #region Callbacks
        
        private void OnInventoryUpdate(SyncList<BaseItem>.Operation operation, int index, BaseItem oldValue, BaseItem newValue)
        {
            UpdateRadialMenuButtons();
        }
        
        private void OnBuildingHealthUpdate(int oldValue, int newValue)
        {
            UpdateRadialMenuButtons();
        }

        private void OnBuildingUpgrade()
        {
            UpdateRadialMenuButtons();
        }

        private void OnFocusBuildingUpdate(BuildingEntity oldValue, BuildingEntity newValue)
        {
            if (oldValue != null)
            {
                var outlines = oldValue.GetComponentsInChildren<Outline>(true);

                foreach (var o in outlines)
                {
                    o.enabled = false;
                }
            }

            if (newValue != null)
            {
                var renderers = newValue.GetComponentsInChildren<Renderer>(true);

                foreach (var r in renderers)
                {
                    if (r.TryGetComponent<Outline>(out var outline))
                    {
                        outline.enabled = true;
                    }
                    else
                    {
                        r.gameObject.AddComponent<Outline>().OutlineWidth = 2.1f;
                    }
                }
            }
        }
        
        #endregion
    }
}