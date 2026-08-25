using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Entities.Player;
using SP.Runtime.Core.Services.SaveService;
using UnityEngine;
using VContainer;

namespace SP.Runtime.Core.Services.PlayerDeathPlacemarkHandler
{
    [RequireComponent(typeof(SaveHandler))]
    public class PlayerDeathPlacemarkHandler : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Sprite _deathIcon;
        
        [SaveHandler.Saved]
        private readonly Dictionary<string, string> _placemarks = new();
        
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
        
        private AuthorizationService.AuthorizationService _authorizationService;

        [Inject]
        private void Inject(AuthorizationService.AuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }
        
        [ServerCallback]
        public void AddDeathPlacemark(Player player)
        {
            if (_placemarks.TryGetValue(player.UniqueId, out var output))
            {
                _authorizationService.RemoveMapPlacemark(output);
            }
            
            if (_authorizationService.TryAddMapPlacemark(
                    player.UniqueId,
                    null,
                    null,
                    player.transform.position,
                    _deathIcon.texture.name,
                    null, 
                    out var placemark))
            {
                _placemarks[player.UniqueId] = placemark.UniqueId;
            }
        }
    }
}