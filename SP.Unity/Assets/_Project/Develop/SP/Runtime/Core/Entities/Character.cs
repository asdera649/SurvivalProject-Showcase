using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Services.AuthorizationService;
using SP.Runtime.Core.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Entities
{
    [RequireComponent(typeof(Systems.Inventory.Inventory))]
    public class Character : BaseEntity, IAuthorizationCallbacks
    {
        [Header("Prefabs")]
        [SerializeField] private ResidualContainerEntity _residualContainerPrefab;
        
        [Header("References")]
        [SerializeField] private Transform _headTransform;
        public Vector3 HeadPosition => _headTransform.position;
        
        [SerializeField] private Transform _rightHandTransform;
        public Transform RightHandTransform => _rightHandTransform;

        [SerializeField] private Transform _leftHandTransform;
        public Transform LeftHandTransform => _leftHandTransform;

        [Header("Settings")]
        [SerializeField] private int[] _inventorySize = new int[] { 5, 18 };
        protected IReadOnlyList<int> InventorySize => _inventorySize;

        [SyncVar(hook = nameof(OnActiveSlotUpdate))]
        private int _activeSlot = _defaultActiveSlot;
        protected int ActiveSlot
        {
            get => _activeSlot;
            set
            {
                var oldValue = _activeSlot;
                _activeSlot = value;

                OnActiveSlotUpdate(oldValue, _activeSlot);
            }
        }

        protected const int _defaultActiveSlot = -1;

        private Systems.Inventory.Inventory _inventory;
        public Systems.Inventory.Inventory Inventory
        {
            get
            {
                if (_inventory == null)
                {
                    _inventory = GetComponent<Systems.Inventory.Inventory>();
                }

                return _inventory;
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            InitializeInventory();
        }

        private void Start()
        {
            var items = Inventory.GetInventory;

            for (var i = 0; i < items.Count; i++)
            {
                OnInventoryUpdated(SyncList<BaseItem>.Operation.OP_ADD, i, null, items[i]);
            }
            
            Inventory.InventoryUpdated += OnInventoryUpdated;
        }

        protected override void OnDestroy()
        {
            Inventory.InventoryUpdated -= OnInventoryUpdated;
            
            base.OnDestroy();
        }
        
        [ServerCallback]
        protected override void OnDeath()
        {
            SpawnResidualContainer();
            
            base.OnDeath();
        }
        
        [ServerCallback]
        private void InitializeInventory()
        {
            var overallSize = 0;

            foreach (var s in _inventorySize)
            {
                overallSize += s;
            }

            Inventory.Initialize(overallSize);
        }

        [ServerCallback]
        private void SpawnResidualContainer()
        {
            ResidualContainerEntity.SpawnResidualContainer(
                _residualContainerPrefab,
                ObjectUtils.CalculateCenter(transform),
                Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, 0)),
                Inventory);
        }
        
        #region Callbacks
        
        [ServerCallback]
        public virtual void OnPlayerAuthorized()
        { 
        
        }

        [ServerCallback]
        public virtual void OnPlayerUnauthorized()
        {
            ActiveSlot = _defaultActiveSlot;
        }
        
        private void OnActiveSlotUpdate(int oldValue, int newValue)
        {
            if (isLocalPlayer)
            {
                return;
            }

            var items = Inventory.GetInventory;

            if (items.Count > oldValue && oldValue > _defaultActiveSlot && items[oldValue] != null)
            {
                items[oldValue].SetActive(false);
            }

            if (items.Count > newValue && newValue > _defaultActiveSlot && items[newValue] != null)
            {
                items[newValue].SetActive(true);
            }
        }

        protected virtual void OnInventoryUpdated(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            if (isLocalPlayer)
            {
                return;
            }

            if (ActiveSlot == index)
            {
                if (oldValue != null)
                {
                    oldValue.SetActive(false);
                }

                if (newValue != null)
                {
                    newValue.SetActive(true);
                }
            }
        }
        
        #endregion
    }
}
