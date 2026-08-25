using SP.Runtime.Core.Entities.Player;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Services.SaveService.CustomSaveHandlers
{
    [RequireComponent(typeof(Player))]
    public class PlayerSaveHandler : MonoBehaviour, ISaveHandler
    {
        private Player _player;
        private Player Player
        {
            get
            {
                if (_player == null)
                {
                    _player = GetComponent<Player>();
                }

                return _player;
            }
        }

        private AuthorizationService.AuthorizationService _authorizationService;

        [Inject]
        private void Inject(AuthorizationService.AuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }
        
        public void OnSetSavedElements()
        {
            Player.CharacterMotor.Motor.SetPosition(Player.transform.position);
            _authorizationService.RestorePlayerReference(Player);
        }
    }
}