using System;
using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Camera;
using SP.Runtime.Core.Entities.Buildings.CupboardEntity;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Movement;
using SP.Runtime.Core.Services;
using SP.Runtime.Core.Services.PlayerDeathPlacemarkHandler;
using SP.Runtime.Core.Services.SaveService;
using SP.Runtime.Core.Systems.Craft;
using SP.Runtime.Core.Systems.Input;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.Systems.NetworkAim;
using SP.Runtime.Core.UI;
using SP.Runtime.Core.UI.Craft;
using SP.Runtime.Core.UI.Hud;
using SP.Runtime.Core.UI.Inventory;
using SP.Runtime.Core.UI.Inventory.AdditionalBlocks;
using SP.Runtime.Core.UI.MiniMap;
using SP.Runtime.Core.UI.Notification;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Entities.Player
{
    [RequireComponent(typeof(Craft), typeof(CharacterMotor), typeof(NetworkAim))]
    [RequireComponent(typeof(SaveHandler), typeof(NetworkAnimator), typeof(NetworkTransformReliable))]
    public class Player : Character, ISaveHandler
    {
        [field: SyncVar, SaveHandler.Saved] public string UniqueId { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private PlayerStats _playerStatsPrefab;
        [SerializeField] private MobileHud _mobileHudPrefab;
        [SerializeField] private InventoryMenu _inventoryMenuPrefab;
        [SerializeField] private CraftMenu _craftMenuPrefab;
        
        [SerializeField] private NotificationBlock notificationBlockPrefab;

        [Header("References")] 
        [SerializeField] private NickNameHandler _nickNameHandler;

        [Header("Settings")] 
        [SerializeField] private Sprite _jumpButtonSprite;
        [SerializeField] private Sprite _inventoryButtonSprite;
        [SerializeField] private List<BaseItem> _startItems = new();
        
        [Space(10)]
        
        [SerializeField] private float _fallDamageThreshold = 25;
        [SerializeField] private float _minFallDamage = 5;
        [SerializeField] private float _maxFallDamage = 300;
        [SerializeField] private float _minFallForce;
        [SerializeField] private float _maxFallForce = 100;
        [SerializeField] private AnimationCurve _fallDamage;

        [Space(10)]
        
        [SerializeField] private AimingHelperConfiguration _aimingHelperConfiguration;
        
        [SyncVar(hook = nameof(OnNickNameUpdated)), SaveHandler.Saved] private string _nickName = "NickName";
        
        private const int _oneSecond = 1;

        private const float _inputMagnitudeSmoothTime = 0.125f;
        private const float _inputHorizontalSmoothTime = 0.1f;
        private const float _inputVerticalSmoothTime = 0.1f;
        
        private readonly int _inputMagnitude = Animator.StringToHash("InputMagnitude");
        private readonly int _inputHorizontal = Animator.StringToHash("InputHorizontal");
        private readonly int _inputVertical = Animator.StringToHash("InputVertical");
        private readonly int _isGrounded = Animator.StringToHash("IsGrounded");
        
        private PlayerStats _playerStats;

        private MobileHud _hud;
        public MobileHud Hud => _hud;

        private InventoryMenu _inventoryMenu;
        private CraftMenu _craftMenu;

        private NotificationBlock _notificationBlock;

        private BaseInput _input;
        public BaseInput Input => _input;

        private InputAction _jumpInputAction;
        private InputAction _inventoryInputAction;
        
        private NotificationElement _raidBlockNotification;
        private NotificationElement _workbenchLevelNotification;
        private NotificationElement _privilegeNotification;
        
        private CameraFollow _camera;
        
        private Vector2 _moveDirection;
        
        #region Cached

        private float _cachedInputMagnitudeVelocity;
        private float _cachedInputHorizontalVelocity;
        private float _cachedInputVerticalVelocity;
        
        #endregion
        
        private Craft _craft;
        private Craft Craft
        {
            get
            {
                if (_craft == null)
                {
                    _craft = GetComponent<Craft>();
                }

                return _craft;
            }
        }
        
        private CharacterMotor _characterMotor;
        public CharacterMotor CharacterMotor
        {
            get
            {
                if (_characterMotor == null)
                {
                    _characterMotor = GetComponent<CharacterMotor>();
                }

                return _characterMotor;
            }
        }
        
        private NetworkAim _networkAim;
        private NetworkAim NetworkAim
        {
            get
            {
                if (_networkAim == null)
                {
                    _networkAim = GetComponent<NetworkAim>();
                }

                return _networkAim;
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
        
        private NetworkAnimator _networkAnimator;
        private NetworkAnimator NetworkAnimator
        {
            get
            {
                if (_networkAnimator == null)
                {
                    _networkAnimator = GetComponent<NetworkAnimator>();
                }

                return _networkAnimator;
            }
        }

        private PlayerDeathPlacemarkHandler _playerDeathPlacemarkHandler;
        private MiniMap _miniMap;
        private PauseMenu _pauseMenu;

        [Inject]
        private void Inject(PlayerDeathPlacemarkHandler playerDeathPlacemarkHandler, MiniMap miniMap, PauseMenu pauseMenu)
        {
            _playerDeathPlacemarkHandler = playerDeathPlacemarkHandler;
            _miniMap = miniMap;
            _pauseMenu = pauseMenu;
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Отключаем у всех кроме локального игрока, иначе синхронизация позиций от NetworkTransform
            // будут перезаписыватся CharacterMotor'ом
            // Далее в OnStartLocalPlayer мы включим CharacterMotor
            CharacterMotor.enabled = false;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            AddStartItems();
            
            Inventory.ItemAdded += OnInventoryItemAdded;
            Inventory.ItemRemoved += OnInventoryItemRemoved;
        }

        [ServerCallback]
        private void AddStartItems()
        {
            if (SaveHandler.IsSavingSet)
            {
                return;
            }
            
            foreach (var i in _startItems)
            {
                Inventory.Add(BaseItem.Instantiate(i));
            }
        }

        public override void OnStopServer()
        {
            Inventory.ItemAdded -= OnInventoryItemAdded;
            Inventory.ItemRemoved -= OnInventoryItemRemoved;
            
            base.OnStopServer();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            _nickNameHandler.gameObject.SetActive(false);
            
            CharacterMotor.enabled = true;
            
            _playerStats = Instantiate(_playerStatsPrefab, Loader.Instance.Canvas.transform);

            _hud = Instantiate(_mobileHudPrefab);

            _input = new MobileInput(_hud);

            _input.MoveAction += OnMove;
            _input.AttackAction += OnAttack;
            
            _jumpInputAction = _input.AddInputAction<InputAction>(
                "Jump",
                _jumpButtonSprite,
                InputAction.ClickType.Down);
            
            _jumpInputAction.Executed += OnJumpExecuted;
            
            _inventoryInputAction = _input.AddInputAction<InputAction>(
                "Open/close inventory",
                _inventoryButtonSprite,
                InputAction.ClickType.Up);
            
            _inventoryInputAction.Executed += OnInventoryExecuted;

            _inventoryMenu = Instantiate(_inventoryMenuPrefab);
            _inventoryMenu.ExitButton.onClick.AddListener(OnInventoryExecuted);// здесь сделать по другому
            _inventoryMenu.CraftButton.onClick.AddListener(OnCraftMenuViewExecuted);
            _inventoryMenu.ItemMoved += OnItemMoved;
            _inventoryMenu.ItemQuickMoved += OnItemQuickMoved;
            _inventoryMenu.ItemQuickDropped += OnItemQuickDropped;
            _inventoryMenu.ItemActionExecuted += OnItemActionExecuted;
            _inventoryMenu.ActiveSlotUpdated += OnActiveSlotUpdated;
            _inventoryMenu.SetOwnerNickName(_nickName);
            _inventoryMenu.Initialize(Inventory, InventorySize);

            _craftMenu = Instantiate(_craftMenuPrefab);
            _craftMenu.CraftAction += OnCraftItem;
            _craftMenu.ExitButton.onClick.AddListener(OnCraftMenuViewExecuted);// тоже сделать по другому
            _craftMenu.Initialize(Craft);

            _notificationBlock = Instantiate(notificationBlockPrefab, Loader.Instance.Canvas.transform);

            _camera = Loader.Instance.MainCamera.CameraFollow;
            
            _camera.Target = transform;
            Loader.Instance.MainCamera.StudioListener.AttenuationObject = gameObject;
            
            _miniMap.Target = transform;

            _pauseMenu.StartedOver += Suicide;
            
            CharacterMotor.LandedAction += OnCharacterMotorLanded;
            CharacterMotor.LeaveStableGroundAction += OnCharacterMotorLeaveStableGround;
            
            HealthUpdated += OnHealthUpdated;
            
            OnHealthUpdated(Health, Health);
            
            InvokeRepeating(nameof(OnLocalClientUpdate), _oneSecond, _oneSecond);
        }
        
        public override void OnStopLocalPlayer()
        {
            if (_playerStats != null)
            {
                Destroy(_playerStats.gameObject);
            }

            if (_input != null)
            {
                _input.Cleanup();
                _input.MoveAction -= OnMove;
                _input.AttackAction -= OnAttack;

                if (_jumpInputAction != null)
                {
                    _jumpInputAction.Executed -= OnJumpExecuted;
                    _input.RemoveInputAction(_jumpInputAction);
                    _jumpInputAction = null;
                }

                if (_inventoryInputAction != null)
                {
                    _inventoryInputAction.Executed -= OnInventoryExecuted;
                    _input.RemoveInputAction(_inventoryInputAction);
                    _inventoryInputAction = null;
                }

                _input = null;
            }

            if (_hud != null)
            {
                Destroy(_hud.gameObject);
            }
            
            if (_inventoryMenu != null)
            {
                _inventoryMenu.ExitButton.onClick.RemoveListener(OnInventoryExecuted);
                _inventoryMenu.CraftButton.onClick.RemoveListener(OnCraftMenuViewExecuted);
                _inventoryMenu.ItemMoved -= OnItemMoved;
                _inventoryMenu.ItemQuickMoved -= OnItemQuickMoved;
                _inventoryMenu.ItemQuickDropped -= OnItemQuickDropped;
                _inventoryMenu.ItemActionExecuted -= OnItemActionExecuted;
                _inventoryMenu.ActiveSlotUpdated -= OnActiveSlotUpdated;
                Destroy(_inventoryMenu.gameObject);
            }

            if (_craftMenu != null)
            {
                _craftMenu.ExitButton.onClick.RemoveListener(OnCraftMenuViewExecuted);
                _craftMenu.CraftAction -= OnCraftItem;
                Destroy(_craftMenu.gameObject);
            }

            if (_notificationBlock != null)
            {
                Destroy(_notificationBlock.gameObject);
            }
            
            _pauseMenu.StartedOver -= Suicide;
            
            CharacterMotor.LandedAction -= OnCharacterMotorLanded;
            CharacterMotor.LeaveStableGroundAction -= OnCharacterMotorLeaveStableGround;
            
            HealthUpdated -= OnHealthUpdated;

            CancelInvoke(nameof(OnLocalClientUpdate));
            
            base.OnStopLocalPlayer();
        }
        
        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            
            _input.Update();

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            
            _moveDirection = new Vector2(
                UnityEngine.Input.GetAxis("Horizontal"),
                UnityEngine.Input.GetAxis("Vertical"));

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                Jump();
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftShift))
            {
                OnAttack();
            }
            
#endif
            
            Move(_moveDirection);
            
            UpdateAim();
        }

        private void OnLocalClientUpdate()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            UpdateRaidBlockNotification();

            UpdateWorkbenchLevelNotification();

            UpdatePrivilegeNotification();
        }
        
        #region Notifications

        private void UpdateRaidBlockNotification()
        {
            var cupboards = CupboardEntity.GetCupboards(transform.position);
            
            CupboardEntity targetCupboard = null;

            var currentRaidBlockTime = -float.MaxValue;
            
            foreach (var c in cupboards)
            {
                if (!c.IsRaidBlock)
                {
                    continue;
                }
                
                if (c.RaidBlockTime > currentRaidBlockTime)
                {
                    targetCupboard = c;
                    currentRaidBlockTime = c.RaidBlockTime;
                }
            }

            if (targetCupboard != null)
            {
                var text = "{RaidBlockedTerritory}: " + TimeSpan.FromSeconds(currentRaidBlockTime).ToString("mm':'ss");

                if (_raidBlockNotification == null)
                {
                    _raidBlockNotification = _notificationBlock.AddNotification(text, NotificationType.Warning, true);
                }
                else
                {
                    _raidBlockNotification.Text = text;
                }
            }
            else
            {
                if (_raidBlockNotification != null)
                {
                    Destroy(_raidBlockNotification.gameObject);
                    _raidBlockNotification = null;
                }
            }
        }

        private void UpdateWorkbenchLevelNotification()
        {
            var currentWorkbenchLevel = _craft.WorkbenchLevelHandler.CurrentWorkbenchLevel;

            if (currentWorkbenchLevel > 0)
            {
                var text = "{WorkbenchLevel}: " + currentWorkbenchLevel;
                
                if (_workbenchLevelNotification == null)
                {
                    _workbenchLevelNotification = _notificationBlock.AddNotification(
                        text,
                        NotificationType.Info,
                        true);
                }
                else
                {
                    _workbenchLevelNotification.Text = text;
                }
            }
            else
            {
                if (_workbenchLevelNotification != null)
                {
                    Destroy(_workbenchLevelNotification.gameObject);
                    _workbenchLevelNotification = null;
                }
            }
        }

        private void UpdatePrivilegeNotification()
        {
            var cupboards = CupboardEntity.GetCupboards(transform.position);

            CupboardEntity targetCupboard = null;

            var currentProtectionTime = -float.MaxValue;
            
            foreach (var c in cupboards)
            {
                var protectionTime = c.GetProtectionTime();

                if (protectionTime > currentProtectionTime)
                {
                    targetCupboard = c;
                    currentProtectionTime = protectionTime;
                }
            }

            if (targetCupboard != null && CupboardEntity.CheckAuthorization(UniqueId, transform.position))
            {
                string text;
                NotificationType notificationType;
                
                if (currentProtectionTime > 0)
                {
                    var timeSpan = TimeSpan.FromSeconds(currentProtectionTime);
                    
                    text = "{PrivilegeOverTerritoryUpkeep} " +
                           timeSpan.Days + "{d.} " +
                           timeSpan.Hours + "{h.} " +
                           timeSpan.Minutes + "{m.}";
                    
                    notificationType = NotificationType.Info;
                }
                else
                {
                    text = "{PrivilegeOverTerritoryStructureIsRotting}";

                    notificationType = NotificationType.Warning;
                }
                
                if (_privilegeNotification == null)
                {
                    _privilegeNotification = _notificationBlock.AddNotification(text, notificationType, true);
                }
                else
                {
                    _privilegeNotification.Text = text;
                    _privilegeNotification.NotificationType = notificationType;
                }
            }
            else
            {
                if (_privilegeNotification != null)
                {
                    Destroy(_privilegeNotification.gameObject);
                    _privilegeNotification = null;
                }
            }
        }
        
        #endregion
        
        private void UpdateAim()
        {
            if (!isLocalPlayer)
            {
                return;
            }
            
            NetworkAim.AimDirection = _hud.AimDirection;
            Loader.Instance.MainCamera.Builder.BuildPoint = _hud.BuildPoint;
        }
        
        [ServerCallback]
        protected override void OnDeath()
        {
            base.OnDeath();
            
            _playerDeathPlacemarkHandler.AddDeathPlacemark(this);
        }
        
        [ClientCallback]
        private void Suicide()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdSuicide();
        }

        [Command]
        private void CmdSuicide()
        {
            TakeDamage(new DamageSenderInfo(this), MaxHealth, DeathMethods.Null);
        }

        #region Motor
        
        private void OnCharacterMotorLeaveStableGround()
        {
            NetworkAnimator.animator.SetBool(_isGrounded, false);
        }
        
        private void OnCharacterMotorLanded(float landingForce)
        {
            NetworkAnimator.animator.SetBool(_isGrounded, true);

            if (!isLocalPlayer)
            {
                return;
            }
            
            CmdDealDamagePerFall(landingForce * -1);
        }

        [Command]
        private void CmdDealDamagePerFall(float fallingForce)
        {
            if (fallingForce > _fallDamageThreshold)
            {
                var coefficient = _fallDamage.Evaluate(Mathf.InverseLerp(_minFallForce, _maxFallForce, fallingForce));
                
                TakeDamage(
                    new DamageSenderInfo(this), 
                    (int)Mathf.Lerp(_minFallDamage, _maxFallDamage, coefficient), 
                    DeathMethods.DeathByFalling);
            }
        }
        
        #endregion
        
        #region Input

        private void Move(Vector2 direction)
        {
            var cameraPlanarRotation = GetCameraPlanarRotation();

            var inputDirection = new Vector3(direction.x, 0, direction.y);
            
            var movementDirection = cameraPlanarRotation * inputDirection;
            
            CharacterMotor.Move(movementDirection);
            
            var angle = Vector3.Angle(transform.rotation * Vector3.forward, cameraPlanarRotation * Vector3.forward);
            var cross = Vector3.Cross(transform.rotation * Vector3.forward, cameraPlanarRotation * Vector3.forward);

            if (cross.y < 0)
            {
                angle = -angle;
            }

            var inputRotation = (Quaternion.AngleAxis(angle, Vector3.up) * inputDirection).normalized;
            
            // Animations
            
            var inputMagnitude = Mathf.SmoothDamp(
                NetworkAnimator.animator.GetFloat(_inputMagnitude),
                inputDirection != Vector3.zero ? CharacterMotor.CurrentMoveSpeed : 0,
                ref _cachedInputMagnitudeVelocity,
                _inputMagnitudeSmoothTime);

            if (inputMagnitude >= CharacterMotor.CurrentMoveSpeed)
            {
                inputMagnitude = CharacterMotor.CurrentMoveSpeed;
            }
            
            if (inputMagnitude <= 0.001f)
            {
                inputMagnitude = 0;
            }

            NetworkAnimator.animator.SetFloat(_inputMagnitude, inputMagnitude);
            
            var inputHorizontal = Mathf.SmoothDamp(
                NetworkAnimator.animator.GetFloat(_inputHorizontal),
                inputRotation.x,
                ref _cachedInputHorizontalVelocity,
                _inputHorizontalSmoothTime);

            if (inputHorizontal >= 0.999f)
            {
                inputHorizontal = 1;
            }
            
            if (inputHorizontal is >= -0.001f and <= 0.001f)
            {
                inputHorizontal = 0;
            }
            
            if (inputHorizontal <= -0.999f)
            {
                inputHorizontal = -1;
            }
            
            NetworkAnimator.animator.SetFloat(_inputHorizontal, inputHorizontal);
            
            var inputVertical = Mathf.SmoothDamp(
                NetworkAnimator.animator.GetFloat(_inputVertical),
                inputRotation.z,
                ref _cachedInputVerticalVelocity,
                _inputVerticalSmoothTime);

            if (inputVertical >= 0.999f)
            {
                inputVertical = 1;
            }
            
            if (inputVertical is >= -0.001f and <= 0.001f)
            {
                inputVertical = 0;
            }
            
            if (inputVertical <= -0.999f)
            {
                inputVertical = -1;
            }
            
            NetworkAnimator.animator.SetFloat(_inputVertical, inputVertical);
        }
        
        private Quaternion GetCameraPlanarRotation()
        {
            var cameraPlanarDirection = Vector3.ProjectOnPlane(
                _camera.transform.rotation * Vector3.forward,
                CharacterMotor.Motor.CharacterUp).normalized;
            
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(
                    _camera.transform.rotation * Vector3.up,
                    CharacterMotor.Motor.CharacterUp).normalized;
            }
            
            return Quaternion.LookRotation(cameraPlanarDirection, CharacterMotor.Motor.CharacterUp);
        }
        
        private void Jump()
        {
            CharacterMotor.Jump();
        }
        
        private void OnMove(Vector2 direction)
        {
            _moveDirection = direction;
        }
        
        private void OnJumpExecuted(InputAction inputAction)
        {
            Jump();
        }

        private void OnAttack()
        {
            if (_inventoryMenu.ActiveSlot == null || _inventoryMenu.ActiveSlot.Item == null)
            {
                return;
            }
            
            _inventoryMenu.ActiveSlot.Item.Use();
        }
        
        #endregion

        #region Inventory

        private void OnInventoryExecuted()
        {
            SetInventoryMenuView(!_inventoryMenu.IsInventoryMenuActive);
        }
        
        private void OnInventoryExecuted(InputAction inputAction)
        {
            OnInventoryExecuted();
        }
        
        public void SetInventoryMenuView(bool value, BaseAdditionalBlock additionalBlock = null, bool dontAnimate = false, bool muteSound = false)
        {
            _inventoryMenu.SetView(value, additionalBlock, dontAnimate, muteSound);
            
            _hud.SetView(!value);
            _notificationBlock.SetView(!value);

            if (value)
            {
                _miniMap.SetView(false);
            }
            else
            {
                _miniMap.SetView(true, _inventoryMenu.DisappearanceAnimationLength);
            }
        }
        
        private void OnItemMoved(Inventory fromInventory, int fromSlotIndex, Inventory toInventory, int toSlotIndex)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdMoveItem(fromInventory, fromSlotIndex, toInventory, toSlotIndex);
        }
        
        [Command]
        private void CmdMoveItem(Inventory fromInventory, int fromSlotIndex, Inventory toInventory, int toSlotIndex)
        {
            fromInventory.Move(fromSlotIndex, toSlotIndex, toInventory);
        }
        
        private void OnItemQuickMoved(Inventory fromInventory, int fromSlotIndex, Inventory toInventory)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdQuickMoveItem(fromInventory, fromSlotIndex, toInventory);
        }

        [Command]
        private void CmdQuickMoveItem(Inventory fromInventory, int fromSlotIndex, Inventory toInventory)
        {
            fromInventory.Move(fromSlotIndex, toInventory);
        }
        
        private void OnItemQuickDropped(Inventory fromInventory, int fromSlotIndex)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            CmdQuickDropItem(fromInventory, fromSlotIndex);
        }
        
        [Command]
        private void CmdQuickDropItem(Inventory fromInventory, int fromSlotIndex)
        {
            fromInventory.Drop(fromSlotIndex);
        }

        private void OnItemActionExecuted(BaseItem item, BaseItem.IReadOnlyAction action)
        {
            item.ExecuteAction(action.Name);
        }
        
        private void OnActiveSlotUpdated(Slot oldSlot, Slot newSlot)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (oldSlot != null && oldSlot.Item != null)
            {
                oldSlot.Item.SetActive(false);
            }

            if (newSlot != null && newSlot.Item != null)
            {
                newSlot.Item.SetActive(true);
            }

            UpdateHudAimingHelper(newSlot != null ? newSlot.Item : null);

            CmdSetActiveSlot(newSlot != null ? newSlot.Index : _defaultActiveSlot);
        }

        [Command]
        private void CmdSetActiveSlot(int index)
        {
            ActiveSlot = index;
        }
        
        protected override void OnInventoryUpdated(SyncList<BaseItem>.Operation operation, int index, BaseItem oldValue, BaseItem newValue)
        {
            base.OnInventoryUpdated(operation, index, oldValue, newValue);

            if (!isLocalPlayer)
            {
                return;
            }

            var activeSlotIndex = _defaultActiveSlot;

            if (_inventoryMenu != null && _inventoryMenu.ActiveSlot != null)
            {
                activeSlotIndex = _inventoryMenu.ActiveSlot.Index;
            }

            if (activeSlotIndex == index)
            {
                if (oldValue != null)
                {
                    oldValue.SetActive(false);
                }

                if (newValue != null)
                {
                    newValue.SetActive(true);
                }
                
                UpdateHudAimingHelper(newValue);
            }

            if (_craftMenu != null)
            {
                _craftMenu.UpdateCraftMenu();
            }
        }

        private void UpdateHudAimingHelper(BaseItem targetItem)
        {
            Hud.CurrentAimingHelperConfiguration = null;
            
            if (targetItem == null)
            {
                return;
            }
            
            if (_aimingHelperConfiguration.TryGetAimingHelperConfiguration(
                    targetItem,
                    out var aimingHelperConfiguration))
            {
                Hud.CurrentAimingHelperConfiguration = aimingHelperConfiguration;
            }
        }

        [ServerCallback]
        private void OnInventoryItemAdded(BaseItem item, int quantity)
        {
            var text = "+" + quantity + " {" + item.Name + "} " + "(" + Inventory.GetTotal(item) + "x)";
            
            SendNotification(text, NotificationType.Neutral);
        }

        [ServerCallback]
        private void OnInventoryItemRemoved(BaseItem item, int quantity)
        {
            var text = "-" + quantity + " {" + item.Name + "} " + "(" + Inventory.GetTotal(item) + "x)";
            
            SendNotification(text, NotificationType.Neutral);
        }
        
        #endregion

        #region Craft

        private void OnCraftMenuViewExecuted()
        {
            if (_inventoryMenu.IsInventoryMenuActive)
            {
                SetInventoryMenuView(!_inventoryMenu.IsInventoryMenuActive, null, true, true);
            }
            
            SetCraftMenuView(!_craftMenu.IsCraftMenuActive);
        }
        
        private void SetCraftMenuView(bool value)
        {
            _craftMenu.SetView(value);
            
            _hud.SetView(!value);
            _notificationBlock.SetView(!value);
            _miniMap.SetView(!value);
        }

        private void OnCraftItem(CraftingRecipe craftingRecipe, int quantity)
        {
            if (Craft.CraftCollection.TryGetCraftRecipeIndex(craftingRecipe, out var index))
            {
                CmdCraftItem(index, quantity);
            }
        }

        [Command]
        private void CmdCraftItem(int craftingRecipeIndex, int quantity)
        {
            Craft.CraftItem(Craft.CraftCollection.CraftingRecipes[craftingRecipeIndex], quantity);
        }
        
        #endregion

        #region Notification

        [ServerCallback]
        public void SendNotification(string text, NotificationType notificationType)
        {
            if (netIdentity.connectionToClient == null)
            {
                return;
            }

            TargetSendNotification(netIdentity.connectionToClient, text, notificationType);
        }

        [TargetRpc]
        private void TargetSendNotification(NetworkConnection target, string text, NotificationType notificationType)
        {
            if (_notificationBlock == null)
            {
                return;
            }
            
            _notificationBlock.AddNotification(text, notificationType);
        }

        #endregion
        
        private void OnHealthUpdated(int oldValue, int newValue)
        {
            if (_playerStats == null)
            {
                return;
            }
            
            _playerStats.SetHealth(Health, MaxHealth);
        }

        private void OnNickNameUpdated(string oldValue, string newValue)
        {
            _nickNameHandler.SetNickName(newValue);

            if (_inventoryMenu != null)
            {
                _inventoryMenu.SetOwnerNickName(newValue);
            }
        }

        public void OnSetSavedElements()
        {
            OnNickNameUpdated(_nickName, _nickName);
        }
        
        #region Statics
        
        public static Player Instantiate(Player prefab, string uniqueId, string nickName, Vector3 position)
        {
            var output = Instantiate(prefab, position, Quaternion.identity);
            output.UniqueId = uniqueId;
            output._nickName = nickName;
            
            return output;
        }

        #endregion
    }
}
