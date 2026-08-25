using System.Collections.Generic;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.Items.Settings;
using SP.Runtime.Core.Movement;
using SP.Runtime.Core.Systems.NetworkAim;
using SP.Runtime.Core.Systems.ValueContainer;
using UnityEngine;

namespace SP.Runtime.Core.Items
{
    public class BaseCombatItem : BaseDamagingItem
    {
        [Header("Settings(BaseCombatItem)")]
        [SerializeField] private Vector3 _aimOriginOffset = new (0, 1.4f, 0);
        [Range(0, 1)] 
        [SerializeField] private float _aimingRange = 1;
        [SerializeField] private float _selectionTime = 0.65f;
        [SerializeField] private BonesWeightSettings _bonesWeightSettings;

        protected Vector3 AimOriginPosition => Transform.position + _aimOriginOffset;

        // В будущем выделить бы в отдельный класс
        private readonly List<float> _queue = new();

        private Container<float>.Element _slowDownElement;
        
        private bool _isActive;
        private float _currentSelectionTime;
        
        protected CharacterMotor CharacterMotor { private set; get; }
        protected Player Player { private set; get; }
        protected NetworkAim NetworkAim { private set; get; }

        protected override void OnInitialize(Systems.Inventory.Inventory owner)
        {
            base.OnInitialize(owner);
        
            CharacterMotor = owner.GetComponent<CharacterMotor>();
            Player = owner.GetComponent<Player>();
            NetworkAim = owner.GetComponent<NetworkAim>();
        }

        protected override void OnDeinitialize()
        {
            CharacterMotor = null;
            Player = null;
            NetworkAim = null;

            base.OnDeinitialize();
        }
        
        protected override void OnItemActive()
        {
            base.OnItemActive();

            _currentSelectionTime = _selectionTime;
            
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Hud.AimJoystick.Interactable = true;
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }
        }

        protected override void OnItemInactive()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Hud.ResetSettings();
                    Player.Hud.AimJoystick.Interactable = false;
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }

            _isActive = false;

            base.OnItemInactive();
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();

            if (IsActive)
            {
                _currentSelectionTime -= Time.deltaTime;

                if (!_isActive)
                {
                    if (_currentSelectionTime <= 0)
                    {
                        ActiveItem();
                    }
                }
            }

            UpdateQueue();
        }
        
        private void ActiveItem()
        {
            if (isLocalPlayer)
            {
                if (Player != null)
                {
                    Player.Hud.Target = Transform;
                    Player.Hud.AimOriginOffset = _aimOriginOffset;
                    Player.Hud.AimingRange = _aimingRange;
                }
                else
                {
                    Debug.LogWarning("Player is null!");
                }
            }

            if (NetworkAim != null)
            {
                _bonesWeightSettings.SetupBones(NetworkAim.AimIK.solver);
            }
            else
            {
                Debug.LogWarning("Aim controller is null!");
            }

            _isActive = true;
        }
        
        #region QueueLogic

        private void UpdateQueue()
        {
            for (var i = _queue.Count - 1; i >= 0; i--)
            {
                _queue[i] -= Time.deltaTime;

                if (_queue[i] <= 0)
                {
                    _queue.RemoveAt(i);
                }
            }
        }

        protected void AddToQueue()
        {
            _queue.Add(1);
        }

        protected bool GetFromQueue()
        {
            if (_queue.Count > 0)
            {
                _queue.RemoveAt(0);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        protected bool CheckSelectionTime()
        {
            return _currentSelectionTime <= 0;
        }

        protected void SlowDown(bool value, float percent)
        {
            if (CharacterMotor != null)
            {
                if (value && _slowDownElement == null)
                {
                    _slowDownElement = CharacterMotor.SlowDownsContainer.Add(Mathf.Clamp01(percent));
                }
                else if (!value && _slowDownElement != null)
                {
                    CharacterMotor.SlowDownsContainer.Remove(_slowDownElement);
                    _slowDownElement = null;
                }
            }
            else
            {
                Debug.LogWarning("Character motor is null!");
            }
        }
    }
}
