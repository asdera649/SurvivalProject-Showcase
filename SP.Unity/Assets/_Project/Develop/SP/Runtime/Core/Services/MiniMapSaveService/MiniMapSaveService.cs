using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Mirror;
using SP.Runtime.Core.UI.MiniMap;
using SP.Runtime.LoadingService;
using UnityEngine;
using VContainer;
using Authorization = SP.Runtime.Core.Services.AuthorizationService.AuthorizationService;

namespace SP.Runtime.Core.Services.MiniMapSaveService
{
    public class MiniMapSaveService : NetworkBehaviour, ILoadUnit
    {
        private readonly Dictionary<MiniMap.PlacemarkContainer, string> _clientPlacemarksCache = new();

        private bool _isCallbacksDisabled = true;
        
        private Authorization _authorizationService;
        private MiniMap _map;
        
        [Inject]
        public void Inject(
            Authorization authorizationService,
            MiniMap map)
        {
            _authorizationService = authorizationService;
            _map = map;
        }
        
        public UniTask Load()
        {
            InitializeClient();
            
            return UniTask.CompletedTask;
        }

        [ClientCallback]
        private void InitializeClient()
        {
            _map.PlacemarkAdded += OnPlacemarkAdded;
            _map.PlacemarkRemoved += OnPlacemarkRemoved;
            
            _authorizationService.LocalMapPlacemarksUpdated += OnLocalMapPlacemarksUpdated;
            
            _authorizationService.RequestMapPlacemarks();
        }

        private void OnDestroy()
        {
            _map.PlacemarkAdded -= OnPlacemarkAdded;
            _map.PlacemarkRemoved -= OnPlacemarkRemoved;
            
            _authorizationService.LocalMapPlacemarksUpdated -= OnLocalMapPlacemarksUpdated;
            
            _clientPlacemarksCache.Clear();
        }

        [Command(requiresAuthority = false)]
        private void CmdAddPlacemark(
            Vector3 worldPosition,
            string placemarkName,
            string placemarkDescription,
            string icon,
            Color? color,
            NetworkConnectionToClient sender = null)
        {
            if (sender == null)
            {
                return;
            }

            _authorizationService.TryAddMapPlacemark(
                sender,
                placemarkName,
                placemarkDescription,
                worldPosition,
                icon,
                color,
                out _);
        }

        [Command(requiresAuthority = false)]
        private void CmdRemovePlacemark(string placemarkUniqueId)
        {
            _authorizationService.RemoveMapPlacemark(placemarkUniqueId);
        }

        [ClientCallback]
        private void OnPlacemarkAdded(MiniMap.PlacemarkContainer placemark)
        {
            if (_isCallbacksDisabled)
            {
                return;
            }

            if (placemark.DontSave)
            {
                return;
            }
            
            _clientPlacemarksCache.Add(placemark, Guid.NewGuid().ToString());
            
            CmdAddPlacemark(
                placemark.WorldPosition,
                placemark.Name,
                placemark.Description,
            placemark.Icon.texture.name,
                placemark.Color);
        }
        
        [ClientCallback]
        private void OnPlacemarkRemoved(MiniMap.PlacemarkContainer placemark)
        {
            if (_isCallbacksDisabled)
            {
                return;
            }
            
            if (placemark.DontSave)
            {
                return;
            }

            if (_clientPlacemarksCache.TryGetValue(placemark, out var uniqueId))
            {
                CmdRemovePlacemark(uniqueId);
            }
        }
        
        [ClientCallback]
        private void OnLocalMapPlacemarksUpdated(List<Authorization.MapPlacemark> placemarks)
        {
            _isCallbacksDisabled = true;
            
            var removedPlacemarks = _clientPlacemarksCache.Where(
                r => placemarks.All(p => r.Value != p.UniqueId)).ToList();

            foreach (var r in removedPlacemarks)
            {
                _clientPlacemarksCache.Remove(r.Key);
                _map.RemovePlacemark(r.Key);
            }

            foreach (var p in placemarks)
            {
                if (!_clientPlacemarksCache.ContainsValue(p.UniqueId))
                {
                    var placemark = _map.AddPlacemark(
                        p.WorldPosition,
                        p.Name,
                        p.Description,
                        Resources.Load<Sprite>(p.Icon),
                        false,
                        true,
                        false,
                        p.Color);
                    
                    _clientPlacemarksCache.Add(placemark, p.UniqueId);
                }
            }
            
            _isCallbacksDisabled = false;
        }
    }
}