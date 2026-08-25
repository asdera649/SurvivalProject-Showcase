using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Objects;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Utilities;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace SP.Runtime.Core.Systems.Inventory
{
    #region Structs

    [Serializable]
    public struct CustomVector3
    {
        public CustomVector3(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public CustomVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class CustomSyncVarAttribute : Attribute { public string hook; }

    public class CustomSyncVarContainer
    {
        public CustomSyncVarContainer(BaseItem item, FieldInfo field)
        {
            Item = item;
            Field = field;
        }

        public BaseItem Item { get; }
        public FieldInfo Field { get; }
    }

    [Serializable]
    public struct CustomSyncVarValue
    {
        public CustomSyncVarValue(string itemUniqueId, string fieldName, object fieldValue)
        {
            ItemUniqueId = itemUniqueId;
            FieldName = fieldName;
            FieldValue = fieldValue;
        }

        public readonly string ItemUniqueId;
        public readonly string FieldName;
        public readonly object FieldValue;
    }
    
    public class CustomCommand
    {
        public CustomCommand(string methodName, bool ignoreAuthority = false)
        {
            MethodName = methodName;
            IgnoreAuthority = ignoreAuthority;
        }

        public event UnityAction<CustomCommand, object[]> InvokeAction;

        public string MethodName { get; }
        public bool IgnoreAuthority { get; }

        public void Send()
        {
            InvokeAction?.Invoke(this, null);
        }

        public void Send(object arg0)
        {
            InvokeAction?.Invoke(this, new [] { arg0 });
        }

        public void Send(object arg0, object arg1)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1 });
        }

        public void Send(object arg0, object arg1, object arg2)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1, arg2 });
        }

        public void Send(object arg0, object arg1, object arg2, object arg3)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1, arg2, arg3 });
        }

        public void Send(object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1, arg2, arg3, arg4 });
        }
    }

    public class CustomCommandContainer
    {
        public CustomCommandContainer(CustomCommand customCommand, BaseItem item, FieldInfo fieldInfo)
        {
            CustomCommand = customCommand;
            
            Item = item;
            FieldInfo = fieldInfo;
        }
    
        public CustomCommand CustomCommand { get; }
        
        public BaseItem Item { get; }
        public FieldInfo FieldInfo { get; }
    }
    
    public class CustomClientRpc
    {
        public CustomClientRpc(string methodName)
        {
            MethodName = methodName;
        }

        public event UnityAction<CustomClientRpc, object[]> InvokeAction;

        public string MethodName { get; }

        public void Send()
        {
            InvokeAction?.Invoke(this, null);
        }

        public void Send(object arg0)
        {
            InvokeAction?.Invoke(this, new [] { arg0 });
        }

        public void Send(object arg0, object arg1)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1 });
        }

        public void Send(object arg0, object arg1, object arg2)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1, arg2 });
        }

        public void Send(object arg0, object arg1, object arg2, object arg3)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1, arg2, arg3 });
        }

        public void Send(object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            InvokeAction?.Invoke(this, new [] { arg0, arg1, arg2, arg3, arg4 });
        }
    }

    public class CustomClientRpcContainer
    {
        public CustomClientRpcContainer(CustomClientRpc customClientRpc, BaseItem item, FieldInfo fieldInfo)
        {
            CustomClientRpc = customClientRpc;
            
            Item = item;
            FieldInfo = fieldInfo;
        }
        
        public CustomClientRpc CustomClientRpc { get; }
    
        public BaseItem Item { get; }
        public FieldInfo FieldInfo { get; }
    }

    #endregion

    public class Inventory : NetworkBehaviour
    {
        public event UnityAction<SyncList<BaseItem>.Operation, int, BaseItem, BaseItem> InventoryUpdated;
        public event UnityAction<BaseItem, int> ItemAdded;
        public event UnityAction<BaseItem, int> ItemRemoved;

        [Header("Settings")] 
        [SerializeField] private bool _randomDropDirection;
        [SerializeField] private Vector3 _itemDropOffset;

        private readonly SyncList<BaseItem> _inventory = new();
        public IReadOnlyList<BaseItem> GetInventory
        {
            get
            {
                AwakenInventory();
                
                return _inventory;
            }
        }

        // Wrapper нужен для системы сохранения(SaveHandler)
        [SaveHandler.Saved]
        private SyncList<BaseItem> InventoryWrapper
        {
            get => _inventory;
            set
            {
                _inventory.Reset();
                _inventory.AddRange(value);
            }
        }

        private IReadOnlyList<BaseItem> _oldItemsCache;
        
        private bool _isInventoryReady;

        private readonly Dictionary<string, Dictionary<string, CustomSyncVarContainer>> _customSyncVarFields = new();
        private readonly SyncList<CustomSyncVarValue> _customSyncVarValues = new();

        private readonly Dictionary<string, Dictionary<string, CustomCommandContainer>> _customCommandFields = new();
        private readonly Dictionary<string, Dictionary<string, CustomClientRpcContainer>> _customClientRpcFields = new();
        
        public override void OnStartClient()
        {
            base.OnStartClient();
            
            for (var i = 0; i < _customSyncVarValues.Count; i++)
            {
                OnCustomSyncVarValuesUpdate(
                    SyncList<CustomSyncVarValue>.Operation.OP_ADD,
                    i,
                    new CustomSyncVarValue(),
                    _customSyncVarValues[i]);
            }

            _customSyncVarValues.Callback += OnCustomSyncVarValuesUpdate;
        }
        
        public override void OnStopClient()
        {
            // TODO: Надо сделать очистку BaseItem's (Object.Destroy()) после уничтожение объекта на клиенте.
            // TODO: Не уверен, нужно ли делать тоже самое после уничтожения объекта на стороне сервера,
            // TODO: т.к. на стороне сервера нету утечек (после уничтожения объекта на сервере,
            // TODO: все содержимое инвентаря перемещается в "Остаточный контейнер", откуда его лутают игроки)
            
            _customSyncVarValues.Callback -= OnCustomSyncVarValuesUpdate;
            
            base.OnStopClient();
        }

        private void Start()
        {
            ReAwakenInventory();
        }

        private void AwakenInventory()
        {
            if (_isInventoryReady)
            {
                return;
            }
            
            for (var i = 0; i < _inventory.Count; i++)
            {
                OnInventoryUpdated(SyncList<BaseItem>.Operation.OP_ADD, i, null, _inventory[i]);
            }

            _oldItemsCache = _inventory.ToList();
        }
        
        // Проблема: у Mirror существует проблема, из-за которой при входе на сервер, для объекта локального игрока,
        // который считается заспавненным на стороне сервера (netId != 0), обновление всех SyncObject's
        // происходит два раза, первый раз - в OnStartClient(), второй - в Start(), это двойное обновление
        // ломает нормальную логику инициализации BaseItem's.
        // Решение: Добавить еще одну инициализацию инвентаря в Start(), причем сделать так,
        // что бы в случаях когда кто то обращается к инвентарю, когда он еще не готов,
        // проинициализировать его принудительно, запомнив состояние инвентаря в _oldItemsCache,
        // а далее при повторной инициализации в Start(), использовать BaseItem's из _oldItemsCache 
        // в качестве oldValue(тоесть получится что в случаях когда двойного обновления не произошло,
        // в Start() мы инициализируем инвентаря используя в качестве oldValue и newValue один и то же BaseItem)
        
        private void ReAwakenInventory()
        {
            for (var i = 0; i < _inventory.Count; i++)
            {
                OnInventoryUpdated(
                    SyncList<BaseItem>.Operation.OP_ADD,
                    i,
                    _oldItemsCache != null && _oldItemsCache.Count > i ? _oldItemsCache[i] : null,
                    _inventory[i]);
            }
            
            _oldItemsCache = null;
            
            _inventory.Callback += OnInventoryUpdated;
        }

        private void OnDestroy()
        {
            _inventory.Callback -= OnInventoryUpdated;
            
            CleanUp();
        }

        private void Update()
        {
            UpdateItems();

            if (isServer)
            {
                UpdateCustomSyncVarValues();
            }
        }

        private void LateUpdate()
        {
            LateUpdateItems();
        }

        private void UpdateItems()
        {
            foreach (var item in _inventory)
            {
                if (item != null)
                {
                    item.OnUpdate();
                }
            }
        }

        private void LateUpdateItems()
        {
            foreach (var item in _inventory)
            {
                if (item != null)
                {
                    item.OnLateUpdate();
                }
            }
        }

        private void CleanUp()
        {
            foreach (var i in _inventory)
            {
                if (i == null)
                {
                    continue;
                }
                
                RemoveCustomCommandFields(i);
                RemoveCustomClientRpcFields(i);
            }
            
            _customSyncVarFields.Clear();
            _customCommandFields.Clear();
            _customClientRpcFields.Clear();
        }

        #region InventoryActions

        // В будущем ко всем значимым методам из этого региона, в аргументах нужно добавить 
        // еще один параметр - отправитель, и с помощью него организовать защиту от читерства.
        // Например: при вызове какого-нибудь метода, проверять, есть ли между отправителем 
        // и этим объектом препятствие, а так же проверять дистанцию между ними, дабы исключить управление
        // инвентарем, допустим сундука, с другого конца карты.
        // А так же добавить в настройки инвентаря в инспекторе, галочку, которая бы блокировала управление
        // инвентарем для объекта который не является текущим, для того что бы один игрок не мог управлять 
        // инвентарем другого игрока(так как в целом по умолчанию любой объект может управлять инвентарем
        // другого объекта, если выполнились условия изложенные сверху).
        
        [ServerCallback]
        public void Initialize(int size)
        {
            if (_inventory.Count != 0)
            {
                return;
            }

            for (var i = 0; i < size; i++)
            {
                _inventory.Add(null);
            }
        }

        public bool IsEmpty()
        {
            var result = true;
            
            foreach (var i in _inventory)
            {
                if (i != null)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public int GetTotal(BaseItem item)
        {
            var total = 0;

            foreach (var i in _inventory)
            {
                if (i != null && item.Equals(i))
                {
                    total += i.Quantity;
                }
            }

            return total;
        }

        public bool Contains(BaseItem item, int quantity)
        {
            return GetTotal(item) >= quantity;
        }

        public bool CanAdd(BaseItem item)
        {
            foreach (var i in _inventory)
            {
                if (i == null || (item.Equals(i) && i.MaxQuantity > i.Quantity))
                {
                    return true;
                }
            }

            return false;
        }

        [ServerCallback]
        public void Add(BaseItem item, bool unite = true, bool drop = true)
        {
            // Костыльное решение для добавленного количества, пока так.
            var quantity = GetTotal(item);
            
            SilentAdd(item, unite, drop);

            // В будущем здесь потенциально может появится ошибка, из за того что мы в ItemAdded
            // передаем экземпляр item, который может быть уничтожен в следствий добаления в инвентарь.
            // Возможное решение: Возвращать итоговый item из SilentAdd и использовать его для 
            // передачи в ItemAdded.
            
            ItemAdded?.Invoke(item, GetTotal(item) - quantity);
        }

        [ServerCallback]
        private void SilentAdd(BaseItem item, bool unite = true, bool drop = true)
        {
            if (unite)
            {
                foreach (var i in _inventory)
                {
                    if (item.Quantity == 0)
                    {
                        return;
                    }
                    
                    if (i != null && item.Equals(i))
                    {
                        OverflowQuantity(item, i);
                    }
                }
            }

            if (item.Quantity == 0)
            {
                return;
            }

            for (var i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i] == null)
                {
                    _inventory[i] = item;
                    return;
                }
            }

            if (drop)
            {
                LocalDrop(item);
            }
        }

        [ServerCallback]
        public void Move(int fromSlotIndex, int toSlotIndex, Inventory toInventory = null)
        {
            if (toInventory == null)
            {
                toInventory = this;
            }
            
            if (_inventory[fromSlotIndex] != null &&
                toInventory._inventory[toSlotIndex] != null &&
                _inventory[fromSlotIndex].Equals(toInventory._inventory[toSlotIndex]))
            {
                OverflowQuantity(_inventory[fromSlotIndex], toInventory._inventory[toSlotIndex]);
            }
            else
            {
                var fromTemp = _inventory[fromSlotIndex];
                var toTemp = toInventory._inventory[toSlotIndex];
                
                _inventory[fromSlotIndex] = null;
                toInventory._inventory[toSlotIndex] = null;
                _inventory[fromSlotIndex] = toTemp;
                toInventory._inventory[toSlotIndex] = fromTemp;
            }
        }

        [ServerCallback]
        public void Move(int fromSlotIndex, Inventory toInventory)
        {
            var item = _inventory[fromSlotIndex];
            
            if (item != null)
            {
                var oldQuantity = item.Quantity;
                var oldTotal = toInventory.GetTotal(item);
                
                _inventory[fromSlotIndex] = null;
                
                toInventory.Add(item, true, false);

                // Если предмет не переместился полностью, а только его часть.
                if (!toInventory.Contains(item, oldTotal + oldQuantity))
                {
                    _inventory[fromSlotIndex] = item;
                }
            }
        }
        
        [ServerCallback]
        public void Split(BaseItem item)
        {
            if (item.Quantity < 2)
            {
                return;
            }

            foreach (var i in _inventory)
            {
                if (i == item)
                {
                    var beforeQuantity = item.Quantity;
                    item.Quantity /= 2;

                    var half = BaseItem.Instantiate(item);
                    half.Quantity = beforeQuantity - item.Quantity;

                    SilentAdd(half, false);
                    break;
                }
            }
        }
        
        [ServerCallback]
        public void Drop(int slotIndex, bool randomDropDirection = false)
        {
            if (_inventory.Count <= slotIndex || slotIndex < 0)
            {
                return;
            }

            if (_inventory[slotIndex] == null)
            {
                return;
            }

            Drop(_inventory[slotIndex], randomDropDirection);
        }

        [ServerCallback]
        public void Drop(BaseItem item, bool randomDropDirection = false)
        {
            for (var i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i] == item)
                {
                    _inventory[i] = null;
                    LocalDrop(item, randomDropDirection);
                    break;
                }
            }
        }

        [ServerCallback]
        public void Remove(BaseItem item, int quantity)
        {
            SilentRemove(item, quantity);

            ItemRemoved?.Invoke(item, quantity);
        }

        [ServerCallback]
        public void SilentRemove(BaseItem item, int quantity)
        {
            var currentQuantity = quantity;

            foreach (var i in _inventory)
            {
                if (i != null && item.Equals(i))
                {
                    if (currentQuantity > 0)
                    {
                        var beforeQuantity = i.Quantity;
                        i.Quantity -= currentQuantity;
                        currentQuantity -= beforeQuantity - i.Quantity;
                    }
                }
            }
        }

        [ServerCallback]
        public void Destroy(BaseItem item)
        {
            for (var i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i] == item)
                {
                    _inventory[i] = null;
                    Object.Destroy(item);
                    break;
                }
            }
        }

        public void Clear()
        {
            for (var i = _inventory.Count - 1; i >= 0; i--)
            {
                if (_inventory[i] != null)
                {
                    Destroy(_inventory[i]);
                }
            }
        }

        [ServerCallback]
        private void OverflowQuantity(BaseItem from, BaseItem to)
        {
            if (from.Quantity > 0)
            {
                if (to.Quantity < to.MaxQuantity)
                {
                    var beforeQuantity = to.Quantity;
                    to.Quantity = Mathf.Clamp(to.Quantity + from.Quantity, 0, to.MaxQuantity);
                    from.Quantity -= to.Quantity - beforeQuantity;
                }
            }
        }

        [ServerCallback]
        private void LocalDrop(BaseItem item, bool randomDropDirection = false)
        {
            if (!randomDropDirection)
            {
                randomDropDirection = _randomDropDirection;
            }

            var pickupItem = Instantiate(
                    item.PickupItemPrefab.gameObject,
                    transform.position + _itemDropOffset,
                    randomDropDirection ?
                        Quaternion.Euler(0, UnityEngine.Random.Range(-180, 180), 0) : 
                        transform.rotation).GetComponent<PickupItem>();

            pickupItem.Initialize(item);

            pickupItem.Rigidbody.AddForce(
                pickupItem.transform.forward * UnityEngine.Random.Range(8, 13),
                ForceMode.Impulse);

            NetworkServer.Spawn(pickupItem.gameObject);
        }

        #endregion

        #region CustomSyncVar
        
        private void AddCustomSyncVarFields(BaseItem item)
        {
            if (_customSyncVarFields.ContainsKey(item.UniqueId))
            {
                return;
            }
        
            var fields = GetCustomSyncVarFields(item);
             
            Dictionary<string, CustomSyncVarContainer> syncVarContainers = new();
        
            foreach (var f in fields)
            {
                syncVarContainers.Add(f.Name, new CustomSyncVarContainer(item, f));
            }
             
            _customSyncVarFields.Add(item.UniqueId, syncVarContainers);
        
            if (isServer)
            {
                foreach (var f in fields)
                {
                    _customSyncVarValues.Add(new CustomSyncVarValue(item.UniqueId, f.Name, f.GetValue(item)));
                }
            }
        }
        
        private void RemoveCustomSyncVarFields(BaseItem item)
        {
            if (!_customSyncVarFields.ContainsKey(item.UniqueId))
            {
                return;
            }
        
            _customSyncVarFields.Remove(item.UniqueId);
             
            if (isServer)
            {
                _customSyncVarValues.RemoveAll(i => i.ItemUniqueId == item.UniqueId);
            }
        }
        
        [ServerCallback]
        private void UpdateCustomSyncVarValues()
        {
            for (var i = 0; i < _customSyncVarValues.Count; i++)
            {
                var itemUniqueId = _customSyncVarValues[i].ItemUniqueId;
                 
                if (_customSyncVarFields.TryGetValue(itemUniqueId, out var syncVarField))
                {
                    if (syncVarField.TryGetValue(_customSyncVarValues[i].FieldName, out var field))
                    {
                        var value = field.Field.GetValue(field.Item);
                         
                        if (_customSyncVarValues[i].FieldValue != value)
                        {
                            _customSyncVarValues[i] = new CustomSyncVarValue(
                                _customSyncVarValues[i].ItemUniqueId,
                                _customSyncVarValues[i].FieldName,
                                value);
                        }
                    }
                }
            }
        }
        
        [ClientCallback]
        private void OnCustomSyncVarValuesUpdate(
            SyncList<CustomSyncVarValue>.Operation operation,
            int index,
            CustomSyncVarValue oldValue,
            CustomSyncVarValue newValue)
        {
            if (string.IsNullOrEmpty(newValue.ItemUniqueId))
            {
                return;
            }
             
            if (_customSyncVarFields.TryGetValue(newValue.ItemUniqueId, out var syncVarField) &&
                syncVarField.TryGetValue(newValue.FieldName, out var field))
            {
                if (!field.Field.GetValue(field.Item).Equals(newValue.FieldValue))
                {
                    field.Field.SetValue(field.Item, newValue.FieldValue);
                    InvokeHook(field.Item, field.Field);
                }
            }
        }
        
        private void InvokeAllHooks(BaseItem item)
        {
            foreach (var f in GetCustomSyncVarFields(item))
            {
                InvokeHook(item, f);
            }
        }
        
        private void InvokeHook(BaseItem item, FieldInfo field)
        {
            var attribute = (CustomSyncVarAttribute)field.GetCustomAttribute(typeof(CustomSyncVarAttribute));
        
            if (attribute is { hook: not null })
            {
                ReflectionUtils.InvokeMethod(item, attribute.hook, null);
            }
        }
        
        private IReadOnlyList<FieldInfo> GetCustomSyncVarFields(BaseItem item)
        {
            return ReflectionUtils.GetFields(item.GetType(), typeof(CustomSyncVarAttribute), true);
        }

        #endregion

        #region CustomCommand

        private void AddCustomCommandFields(BaseItem item)
        {
            if (_customCommandFields.ContainsKey(item.UniqueId))
            {
                return;
            }

            var fields = ReflectionUtils.GetFields(item.GetType(), typeof(CustomCommand), false);
            
            Dictionary<string, CustomCommandContainer> customCommandContainers = new();

            foreach (var f in fields)
            {
                var customCommand = (CustomCommand)f.GetValue(item);
                customCommand.InvokeAction += OnCustomCommandInvoke;
                
                customCommandContainers.Add(f.Name, new CustomCommandContainer(customCommand, item, f));
            }
            
            _customCommandFields.Add(item.UniqueId, customCommandContainers);
        }
        
        private void RemoveCustomCommandFields(BaseItem item)
        {
            if (_customCommandFields.TryGetValue(item.UniqueId, out var commandContainers))
            {
                foreach (var c in commandContainers)
                {
                    c.Value.CustomCommand.InvokeAction -= OnCustomCommandInvoke;
                }

                _customCommandFields.Remove(item.UniqueId);
            }
        }

        private void OnCustomCommandInvoke(CustomCommand customCommand, object[] arguments)
        {
            foreach (var f in _customCommandFields)
            {
                foreach (var c in f.Value)
                {
                    if (c.Value.CustomCommand == customCommand)
                    {
                        if (!customCommand.IgnoreAuthority)
                        {
                            CmdCustomCommandInvoke(f.Key, c.Key, arguments);
                        }
                        else
                        {
                            CmdCustomCommandInvokeIgnore(f.Key, c.Key, arguments);
                        }
                        
                        break;
                    }
                }
            }
        }

        [Command]
        private void CmdCustomCommandInvoke(string itemUniqueId, string fieldName, object[] arguments)
        {
            CustomCommandInvoke(itemUniqueId, fieldName, arguments, false);
        }

        [Command(requiresAuthority = false)]
        private void CmdCustomCommandInvokeIgnore(string itemUniqueId, string fieldName, object[] arguments)
        {
            CustomCommandInvoke(itemUniqueId, fieldName, arguments, true);
        }

        [ServerCallback]
        private void CustomCommandInvoke(
            string itemUniqueId,
            string fieldName,
            object[] arguments,
            bool ignoreAuthority)
        {
            if (_customCommandFields.TryGetValue(itemUniqueId, out var fields))
            {
                if (fields.TryGetValue(fieldName, out var container))
                {
                    if (container.CustomCommand.IgnoreAuthority != ignoreAuthority)
                    {
                        return;
                    }
                    
                    ReflectionUtils.InvokeMethod(container.Item, container.CustomCommand.MethodName, arguments);
                }
            }
        }

        #endregion

        #region CustomClientRpc

        private void AddCustomClientRpcFields(BaseItem item)
        {
            if (_customClientRpcFields.ContainsKey(item.UniqueId))
            {
                return;
            }

            var fields = ReflectionUtils.GetFields(item.GetType(), typeof(CustomClientRpc), false);
            
            Dictionary<string, CustomClientRpcContainer> customClientRpcContainers = new();

            foreach (var f in fields)
            {
                var customClientRpc = (CustomClientRpc)f.GetValue(item);
                customClientRpc.InvokeAction += OnCustomClientRpcInvoke;
                
                customClientRpcContainers.Add(f.Name, new CustomClientRpcContainer(customClientRpc, item, f));
            }
            
            _customClientRpcFields.Add(item.UniqueId, customClientRpcContainers);
        }
        
        private void RemoveCustomClientRpcFields(BaseItem item)
        {
            if (_customClientRpcFields.TryGetValue(item.UniqueId, out var clientRpcContainers))
            {
                foreach (var c in clientRpcContainers)
                {
                    c.Value.CustomClientRpc.InvokeAction -= OnCustomClientRpcInvoke;
                }

                _customClientRpcFields.Remove(item.UniqueId);
            }
        }

        private void OnCustomClientRpcInvoke(CustomClientRpc customClientRpc, object[] arguments)
        {
            foreach (var f in _customClientRpcFields)
            {
                foreach (var c in f.Value)
                {
                    if (c.Value.CustomClientRpc == customClientRpc)
                    {
                        RpcCustomClientRpcInvoke(f.Key, c.Key, arguments);
                        
                        break;
                    }
                }
            }
        }

        [ClientRpc]
        private void RpcCustomClientRpcInvoke(string itemUniqueId, string fieldName, object[] arguments)
        {
            if (_customClientRpcFields.TryGetValue(itemUniqueId, out var fields))
            {
                if (fields.TryGetValue(fieldName, out var container))
                {
                    ReflectionUtils.InvokeMethod(container.Item, container.CustomClientRpc.MethodName, arguments);
                }
            }
        }

        #endregion

        #region Callbacks

        private void OnInventoryUpdated(
            SyncList<BaseItem>.Operation operation,
            int index,
            BaseItem oldValue,
            BaseItem newValue)
        {
            if (oldValue != null)
            {
                RemoveCustomSyncVarFields(oldValue);
                RemoveCustomCommandFields(oldValue);
                RemoveCustomClientRpcFields(oldValue);
                
                oldValue.QuantityUpdated -= OnItemQuantityUpdated;
                oldValue.StockStrengthUpdated -= OnItemStockStrengthUpdated;
                oldValue.Deinitialize();
            }
            
            if (newValue != null)
            {
                AddCustomSyncVarFields(newValue);
                AddCustomCommandFields(newValue);
                AddCustomClientRpcFields(newValue);
                
                if (newValue.Initialize(this))
                {
                    if (isClient)
                    {
                        InvokeAllHooks(newValue);
                    }

                    newValue.QuantityUpdated += OnItemQuantityUpdated;
                    newValue.StockStrengthUpdated += OnItemStockStrengthUpdated;
                }
            }

            _isInventoryReady = true;
            
            InventoryUpdated?.Invoke(operation, index, oldValue, newValue);

            // Очистка.
            
            if (!isClientOnly)
            {
                return;
            }
            
            if (oldValue != null && oldValue != newValue)
            {
                Object.Destroy(oldValue);
            }
        }

        private void OnItemQuantityUpdated(BaseItem item, int value)
        {
            OnItemDataUpdated(item);
        }

        private void OnItemStockStrengthUpdated(BaseItem item, float value)
        {
            OnItemDataUpdated(item);
        }

        private void OnItemDataUpdated(BaseItem item)
        {
            InventoryUpdated?.Invoke(
                SyncList<BaseItem>.Operation.OP_ADD, 
                _inventory.FindIndex(i => i == item),
                null,
                item);
        }
        
        #endregion 
    }
}