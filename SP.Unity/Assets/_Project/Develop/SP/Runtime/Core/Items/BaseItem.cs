using System;
using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Objects;
using SP.Runtime.Core.Objects.Models;
using SP.Runtime.Core.Services.SaveService.Converters;
using SP.Runtime.Core.Systems.Inventory;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SP.Runtime.Core.Items
{
    public class BaseItem : ScriptableObject
    {
        #region Structs
        
        public enum ItemSections
        {
            None,
            Clothes,
            Constructions,
            Resources,
            Tools,
            Weapons,
            Ammunition,
            Medical,
            Meal
        }
        
        public interface IReadOnlyAction
        {
            public string Name { get; }
        }

        protected class Action : IReadOnlyAction
        {
            public Action(string name, System.Action method)
            {
                Name = name;
                Method = method;
            }
            
            public string Name { get; }
            public System.Action Method { get; }
        }

        private enum ModelLocation
        { 
            RightHand,
            LeftHand
        }
        
        #endregion
        
        public event UnityAction<BaseItem, int> QuantityUpdated;
        public event UnityAction<BaseItem, float> StockStrengthUpdated;

        private readonly CustomCommand _cmdExecuteAction = new (nameof(CmdExecuteAction), true);
        private readonly CustomClientRpc _rpcInvokeDestructionImpacts = new (nameof(RpcInvokeDestructionImpacts));

        // Задается при создании экземпяра BaseItem в BaseItem.Instantiate,
        // автоматический задается при синхронизации BaseItem
        [field: CustomSyncVar] public string UniqueId { get; private set; }
        
        [Header("Settings(BaseItem)")]
        [SerializeField] private string _name;
        public string Name => _name;
        
        [TextArea(5, 10)]
        [SerializeField] private string _description;
        public string Description => _description;

        [SerializeField] private Sprite _icon;
        public Sprite Icon => _icon;

        [SerializeField] private ItemSections _itemSection;
        public ItemSections ItemSection => _itemSection;

        [Space(10)] [SerializeField] private int _maxQuantity = 1;
        public int MaxQuantity => _maxQuantity;
        
        [Space(10)] [SerializeField] private bool _considerStrength;
        public bool ConsiderStrength => _considerStrength;

        [SerializeField] private float _strengthLossDegree = 0.025f;
        
        [Space(10)] [SerializeField] private BaseModel _modelPrefab;
    
        [SerializeField] private ModelLocation _modelLocation = ModelLocation.RightHand;
        
        [Space(10)] [SerializeField] private PickupItem _pickupItemPrefab;
        public PickupItem PickupItemPrefab => _pickupItemPrefab;

        [SerializeField] private float _despawnTime = 300;
        public float DespawnTime => _despawnTime;
        
        [Space(10)] [SerializeField] private bool _hasEquipAnimation;
        
        [SerializeField] private int _equipAnimationId;
        
        [ItemConverter.Saved] 
        [CustomSyncVar(hook = nameof(OnQuantityUpdated))] 
        private int _quantity = 1;
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = Mathf.Clamp(value, 0, int.MaxValue);
                OnQuantityUpdated();
            }
        }
        
        [ItemConverter.Saved] 
        [CustomSyncVar(hook = nameof(OnStockStrengthUpdated))] 
        private float _stockStrength = 1;
        public float StockStrength
        { 
            get => _stockStrength;
            set
            {
                _stockStrength = Mathf.Clamp01(value);
                OnStockStrengthUpdated();
            }
        }

        public virtual string GetQuantityText => Quantity == 1 ? "" : "x" + Quantity;
        
        public bool IsStockStrengthFull => _stockStrength >= _fullStockStrengthValue;
        
        protected BaseModel Model { get; private set; }

        #region Мусорка, скоро уйдут отсюда

        [Space(10)] [SerializeField] private BaseItem _combustionResidue;
        public BaseItem CombustionResidue => _combustionResidue;
        
        [SerializeField] private BaseItem _remeltedItem;
        public BaseItem RemeltedItem => _remeltedItem;
        
        protected bool IsCast { get; private set; }

        private float _currentCastTime;

        #endregion

        protected bool IsActive { get; private set; }
        
        protected bool isLocalPlayer
        {
            get
            {
                if (Inventory != null)
                {
                    return Inventory.netIdentity.isLocalPlayer;
                }

                return false;
            }
        }

        private bool isClient
        {
            get
            {
                if (Inventory != null)
                {
                    return Inventory.netIdentity.isClient;
                }
                
                return false;
            }
        }

        protected bool isServer
        {
            get
            {
                if (Inventory != null)
                {
                    return Inventory.netIdentity.isServer;
                }

                return false;
            }
        }

        private bool IsInitialized { get; set; }

        private const float _fullStockStrengthValue = 1;
        
        private readonly int _equipIDHash = Animator.StringToHash("EquipID");
        private readonly int _equipHash = Animator.StringToHash("Equip");

        protected Transform Transform { get; private set; }
        protected Inventory Inventory { get; private set; }
        
        private Character Character { get; set; }
        protected NetworkAnimator NetworkAnimator { get; private set; }
        
        public bool Initialize(Inventory owner)
        {
            if (IsInitialized)
            {
                Debug.LogError("Attempting to initialize an item that has not been de-initialized!");
                return false;
            }
            
            Transform = owner.transform;
            Inventory = owner;
            
            OnInitialize(owner);
            
            IsInitialized = true;
            
            CheckToDestroy();

            return true;
        }
        
        public void Deinitialize(bool throwError = true)
        {
            if (!IsInitialized)
            {
                if (throwError)
                {
                    Debug.LogError("Attempting to deinitialize an item that has not been initialized!");
                }
                
                return;
            }
            
            SetActive(false);
            
            OnDeinitialize();
            
            Transform = null;
            Inventory = null;

            IsInitialized = false;
        }

        protected virtual void OnInitialize(Inventory owner)
        {
            Character = owner.GetComponent<Character>();
            NetworkAnimator = owner.GetComponent<NetworkAnimator>();
        }

        protected virtual void OnDeinitialize()
        {
            Character = null;
            NetworkAnimator = null;
        }
        
        public virtual void OnDestroy()
        {
            Deinitialize(false);
        }
        
        protected virtual void OnItemActive()
        {
            if (Character != null)
            {
                if (_modelPrefab != null)
                {
                    var parent = _modelLocation == ModelLocation.RightHand ? 
                        Character.RightHandTransform :
                        Character.LeftHandTransform;

                    Model = Instantiate(_modelPrefab, parent, true);

                    var rotation = Quaternion.Euler(
                        Model.RotationOffset.x,
                        Model.RotationOffset.y,
                        Model.RotationOffset.z);
                    
                    Model.transform.SetLocalPositionAndRotation(Model.PositionOffset, rotation);
                    
                    Model.Pick();
                }
            }
            else
            {
                Debug.LogWarning("Character is null!");
            }

            if (_hasEquipAnimation)
            {
                if (NetworkAnimator != null)
                {
                    NetworkAnimator.animator.SetInteger(_equipIDHash, _equipAnimationId);
                    NetworkAnimator.animator.SetTrigger(_equipHash);
                }
                else
                {
                    Debug.LogWarning("Network animator is null!");
                }
            }
        }

        protected virtual void OnItemInactive()
        {
            if (Model != null)
            {
                Destroy(Model.gameObject);
            }
        }

        public virtual void OnUpdate()
        {
            CastUpdate();
        }

        public virtual void OnLateUpdate()
        { 
        
        }
        
        public void SetActive(bool value)
        {
            if (IsActive == value)
            {
                return;
            }
            
            IsActive = value;

            if (IsActive)
            {
                OnItemActive();
            }
            else
            {
                StopCast();
                OnItemInactive();
            }
        }
        
        public void SetFullStrength()
        {
            if (!isServer)
            {
                return;
            }

            if (ConsiderStrength)
            {
                StockStrength = _fullStockStrengthValue;
            }
        }

        protected void ToLoseStrength()
        {
            if (!isServer)
            {
                return;
            }

            if (!ConsiderStrength)
            {
                return;
            }
            
            StockStrength -= _strengthLossDegree;
        }

        #region Actions

        public void ExecuteAction(string actionName)
        {
            if (!isClient)
            {
                return;
            }
            
            _cmdExecuteAction.Send(actionName);
        }
        
        public void CmdExecuteAction(string actionName)
        {
            if (!isServer)
            {
                return;
            }
            
            var actions = GetActions();

            foreach (var a in actions)
            {
                if (a.Name == actionName)
                {
                    a.Method();
                    return;
                }
            }
        }
        
        public IReadOnlyList<IReadOnlyAction> GetReadOnlyActions()
        {
            return new List<IReadOnlyAction>(GetActions());
        }

        protected virtual List<Action> GetActions()
        {
            List<Action> actions = new()
            {
                new Action(nameof(Split), Split),
                new Action(nameof(Drop), Drop)
            };

            return actions;
        }

        private void Split()
        {
            if (!isServer)
            {
                return;
            }
            
            Inventory.Split(this);
        }

        private void Drop()
        {
            if (!isServer)
            {
                return;
            }
        
            Inventory.Drop(this);
        }
        
        #endregion

        #region Cast

        private void CastUpdate()
        {
            if (IsCast)
            {
                if (_currentCastTime > 0)
                {
                    OnDuringCast();
                    _currentCastTime -= Time.deltaTime;
                }
                else
                {
                    IsCast = false;
                    OnCastCompleted();
                }
            }
        }

        protected void StartCast(float time, bool ignoreCastRunning = false)
        {
            if (ignoreCastRunning && IsCast)
            {
                return;
            }

            _currentCastTime = time;
            IsCast = true;
            
            OnStartCast();
        }

        protected void StopCast()
        {
            if (!IsCast)
            {
                return;
            }

            IsCast = false;
            OnStopCast();
        }

        protected virtual void OnStartCast() { }

        protected virtual void OnDuringCast() { }

        protected virtual void OnCastCompleted() { }

        protected virtual void OnStopCast() { }

        #endregion

        public virtual void Use()
        {
            
        }

        private void OnQuantityUpdated()
        {
            QuantityUpdated?.Invoke(this, Quantity);
            
            CheckToDestroy();
        }

        private void OnStockStrengthUpdated()
        {
            StockStrengthUpdated?.Invoke(this, StockStrength);
            
            CheckToDestroy();
        }

        [ServerCallback]
        private void CheckToDestroy()
        {
            var isStrengthReason = ConsiderStrength && StockStrength <= 0;
            
            if (Quantity <= 0 || isStrengthReason)
            {
                if (!IsInitialized)
                {
                    Destroy(this);
                }
                else
                {
                    if (isStrengthReason)
                    {
                        // Если это хост то просто вызываем InvokeDestructionImpacts().
                        // Все потому, что если у хоста перед уничтожением отправить Rpc,то он, не отправится.

                        if (isServer && isClient)
                        {
                            InvokeDestructionImpacts();
                        }

                        _rpcInvokeDestructionImpacts.Send();
                    }

                    Inventory.Destroy(this);   
                }
            }
        }
        
        private void RpcInvokeDestructionImpacts()
        {
            InvokeDestructionImpacts();
        }

        private void InvokeDestructionImpacts()
        {
            if (Model == null)
            {
                return;
            }
 
            Model.Destroy();
        }
        
        public bool Equals(BaseItem value)
        {
            return value != null && Name == value.Name;
        }
        
        #region Statics
        
        public static BaseItem Instantiate(BaseItem value)
        {
            var output = Object.Instantiate(value);
            output.name = value.name;
            output.UniqueId = Guid.NewGuid().ToString("N");
            
            return output;
        }

        #endregion
    }
}