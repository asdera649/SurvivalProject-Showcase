using System;
using Mirror;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.UI.Craft;
using SP.Runtime.Localization;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

namespace SP.Runtime.Core.UI.Inventory.AdditionalBlocks
{
    public class CupboardInventoryBlock : BaseAdditionalBlock
    {
        [Header("Prefabs")]
        [SerializeField] private RequiredItem _requiredItemPrefab;

        [Header("Refrences")]
        [SerializeField] private Button _logInButton;
        [SerializeField] private Button _clearLoggedInButton;
        [SerializeField] private LocalizeStringHelper _tipTextLocalize;
        [SerializeField] private Transform _requiredItemsBlock;

        [Header("Settings")]
        [SerializeField] private float _updateRateInSeconds = 1;

        private const int _secondsInHour = 3600;
        
        private CupboardEntity _cupboardEntity;

        #region Logics
        
        private void Start()
        {
            _logInButton.onClick.AddListener(OnLogInButtonClick);
            _clearLoggedInButton.onClick.AddListener(OnClearLoggedInButtonClick);
        }

        public override void Initialize<T>(T cupboardEntity, string nameEntry, Systems.Inventory.Inventory inventory, int[] inventorySize)
        {
            base.Initialize(cupboardEntity, nameEntry, inventory, inventorySize);
            
            _cupboardEntity = cupboardEntity as CupboardEntity;
            
            OnAuthorizedEntitiesUpdate();
            
            // ReSharper disable once PossibleNullReferenceException
            _cupboardEntity.AuthorizedEntitiesUpdated += OnAuthorizedEntitiesUpdate;
        
            InvokeRepeating(nameof(OnUpdate), 0, _updateRateInSeconds);
        }

        protected override void OnDestroy()
        {
            _logInButton.onClick.RemoveListener(OnLogInButtonClick);
            _clearLoggedInButton.onClick.RemoveListener(OnClearLoggedInButtonClick);
            
            _cupboardEntity.AuthorizedEntitiesUpdated -= OnAuthorizedEntitiesUpdate;
            
            CancelInvoke(nameof(OnUpdate));
            
            base.OnDestroy();
        }
        
        #endregion

        #region Callbacks
        
        private void OnLogInButtonClick()
        {
            _cupboardEntity.LogIn();
        }

        private void OnClearLoggedInButtonClick()
        {
            _cupboardEntity.ClearLoggedIn();
        }

        private void OnAuthorizedEntitiesUpdate()
        {
            if (NetworkClient.localPlayer == null) return;
            if (!NetworkClient.localPlayer.TryGetComponent(out Player player)) return;
            
            var isLocalPlayerAuthorized = _cupboardEntity.ContainsUniqueId(player.UniqueId);

            _logInButton.interactable = !isLocalPlayerAuthorized;
            _clearLoggedInButton.interactable = isLocalPlayerAuthorized;
        }

        private void OnUpdate()
        {
            foreach (Transform child in _requiredItemsBlock.transform)
                Destroy(child.gameObject);

            var requiredItems = _cupboardEntity.GetCostForUpkeep();

            foreach (var r in requiredItems)
            {
                var item = Instantiate(
                    _requiredItemPrefab.gameObject,
                    _requiredItemsBlock).GetComponent<RequiredItem>();

                item.Initialize(
                    new Systems.Craft.RequiredItem(
                        r.Item,
                        (int)(_secondsInHour / _cupboardEntity.CupboardUpdateRateInSeconds * r.Quantity)),
                        Inventory.GetTotal(r.Item));
            }

            var time = _cupboardEntity.GetProtectionTime();

            if (time > 0)
            {
                var timeSpan = TimeSpan.FromSeconds(time);

                _tipTextLocalize.SetEntry(
                    new EntryContainer(
                        "StructureDecayProtected",
                        new LocalVariable[]
                        {
                            new("days", new IntVariable() { Value = timeSpan.Days }),
                            new("hours", new IntVariable() { Value = timeSpan.Hours }),
                            new("minutes", new IntVariable() { Value = timeSpan.Minutes })
                        }));
            }
            else
            {
                _tipTextLocalize.SetEntry(
                    new EntryContainer("StructureNotDecayProtected", Array.Empty<LocalVariable>()));
            }
        }
        
        #endregion
    }
}
