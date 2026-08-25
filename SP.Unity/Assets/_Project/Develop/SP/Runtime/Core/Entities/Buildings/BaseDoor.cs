using FMODUnity;
using Mirror;
using SP.Runtime.Core.Systems.Interaction;
using UnityEngine;

namespace SP.Runtime.Core.Entities.Buildings
{
    [RequireComponent(typeof(InteractionHandler))]
    public class BaseDoor : BuildingEntity
    {
        [Header("References")]
        [SerializeField] private Transform _door;
        
        [SerializeField] private StudioEventEmitter _openEventEmitter;
        [SerializeField] private StudioEventEmitter _closeEventEmitter;
        
        [Header("Settings")]
        [SerializeField] private float _doorOpeningSpeed = 8;
        [SerializeField] private float _doorClosingSpeed = 12;
        [SerializeField] private float _doorOpeningCooldown = 0.77f;
        
        private float CurrentSpeed => IsDoorOpen ? _doorOpeningSpeed : _doorClosingSpeed;
        
        [SyncVar] private float _openingAngle;
        private bool IsDoorOpen => _openingAngle != 0;
        
        private float _cooldown;
        
        private const float _openDoorAngle = 90;
        private const float _closeDoorAngel = 0;
        
        private InteractionHandler _interactionHandler;
        private InteractionHandler InteractionHandler
        {
            get
            {
                if (_interactionHandler == null)
                {
                    _interactionHandler = GetComponent<InteractionHandler>();
                }

                return _interactionHandler;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            InteractionHandler.ExecutionAction += OnExecution;
        }

        public override void OnStopClient()
        {
            InteractionHandler.ExecutionAction -= OnExecution;
            
            base.OnStopClient();
        }
        
        private void Update()
        {
            UpdateDoor();
            
            UpdateCooldown();
        }

        private void UpdateDoor()
        {
            _door.localRotation = Quaternion.Lerp(
                _door.localRotation,
                Quaternion.Euler(0, _openingAngle, 0),
                CurrentSpeed * Time.deltaTime);
        }

        private void UpdateCooldown()
        {
            _cooldown -= Time.deltaTime;
        }

        [ClientCallback]
        private void OnExecution()
        {
            CmdOpen();
        }

        [Command(requiresAuthority = false)]
        private void CmdOpen(NetworkConnectionToClient sender = null)
        {
            if (sender == null || !sender.identity.TryGetComponent(out Player.Player player))
            {
                return;
            }
            
            OpenDoor(player);
        }
        
        private bool CanOpen(Player.Player player)
        {
            return !(_cooldown > 0) &&
                   CupboardEntity.CupboardEntity.CheckAuthorization(player, transform.position);
        }

        [ServerCallback]
        private void OpenDoor(Player.Player player)
        {
            if (!CanOpen(player))
            {
                return;
            }
            
            if (!IsDoorOpen)
            {
                if (transform.InverseTransformPoint(player.transform.position).z < 0)
                {
                    _openingAngle = _openDoorAngle;
                }
                else
                {
                    _openingAngle = -_openDoorAngle;
                }
            }
            else
            {
                _openingAngle = _closeDoorAngel;
            }

            RpcInvokeOpenOrCloseImpacts(IsDoorOpen);

            _cooldown = _doorOpeningCooldown;
        }
        
        [ClientRpc]
        private void RpcInvokeOpenOrCloseImpacts(bool isOpen)
        {
            if (isOpen)
            {
                _openEventEmitter.Play();
            }
            else
            {
                _closeEventEmitter.Play();
            }
        }
    }
}