using System.Collections.Generic;
using System.Linq;
using EasyBuildSystem.Features.Scripts.Core.Base.Group;
using Mirror;
using SP.Runtime.Core.Utilities;
using SP.Runtime.Utilities;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Hiding
{
    public class Hider : NetworkBehaviour
    {
        #region Structs
        
        private enum CastDirection
        { 
            GroupDirection,
            Up
        }

        public enum HandlersHidingMode
        {
            Full,
            NotFull
        }
        
        #endregion

        [Header("Settings")] 
        [SerializeField] private float _handlersHidingTransparency = 0.15f;
        
        [Space(10)]
        
        [SerializeField] private Vector3 _sphereCastOffset = Vector3.up;
        [SerializeField] private float _sphereCastLenght = 10;
        [SerializeField] private float _sphereCastRadius = 0.15f;
        
        [Space(10)] 
        
        [SerializeField] private Vector3 _boxCastOffset = new(0, 0.2f, 0);
        
        [Space(10)] 
        
        [SerializeField] private LayerMask _layerMask;
        
        [Space(10)] 
        
        [SerializeField] private float _updateRate = 0.5f;

        public HandlersHidingMode HidingMode { get; set; } = HandlersHidingMode.Full;

        public GroupBehaviour CurrentHiddenGroup { get; private set; }

        private readonly List<HidingHandler> _hiddenHandlers = new();
        private readonly List<ObjectHider> _hiddenObjects = new();
        
        private GameObject _hiderBox;
        private GameObject _botPoint;
        private GameObject _hitPoint;

        private readonly Vector3 _botPointPosition = new(0, -0.5f, 0);

        private const string _hiderBoxName = "HiderBox";
        private const string _botPointName = "BotPoint";
        private const string _hitPointName = "HitPoint";
        
        #region Cache
        
        private List<HidingHandler> _hiddenHandlersCache = new();
        private List<ObjectHider> _hiddenObjectsCache = new();
        private readonly List<ObjectHider> _ignoredObjectsCache = new();
        
        #endregion
        
        public override void OnStartLocalPlayer()
        {
            InvokeRepeating(nameof(UpdateHiding), _updateRate, _updateRate);
            
            InstantiateHiderBox();
        }

        public override void OnStopLocalPlayer()
        {
            CancelInvoke(nameof(UpdateHiding));
            
            ResetHidden();
            
            DestroyHiderBox();
        }
        
        private void InstantiateHiderBox()
        {
            if (_hiderBox != null)
            {
                return;
            }
            
            _hiderBox = new GameObject(_hiderBoxName);
            
            _botPoint = new GameObject(_botPointName);
            _botPoint.transform.SetParent(_hiderBox.transform);
            _botPoint.transform.localPosition = _botPointPosition;
            
            _hitPoint = new GameObject(_hitPointName);
            _hitPoint.transform.SetParent(_hiderBox.transform);
        }

        private void DestroyHiderBox()
        {
            if (_hiderBox == null)
            {
                return;
            }
            
            Destroy(_hiderBox);
        }

        private void UpdateHiding()
        {
            CurrentHiddenGroup = null;
            
            ClearCache();

            RaycastHit? currentHit = null;

            var hits = Raycast(CastDirection.GroupDirection);
            
            foreach (var h in hits)
            {
                if (!ComponentUtils.TryGetComponentInParent<HidingHandler>(h.transform, out var hidingHandler))
                {
                    continue;
                }

                if (hidingHandler.GetPieceType is HidingHandler.PieceType.Foundation)
                {
                    break;
                }
                
                if (hidingHandler.GetPieceType == HidingHandler.PieceType.Floor)
                {
                    currentHit = h;

                    UpdateIgnoredObjects(hidingHandler);
                    
                    HideHidingHandlers(hidingHandler);
                    
                    break;
                }
            }
            
            ResetHiddenHandlers();

            if (currentHit != null &&
                ComponentUtils.TryGetComponentInParent(currentHit.Value.transform, out GroupBehaviour group))
            {
                HideObjects(currentHit.Value.point, group);

                CurrentHiddenGroup = group;
            }
            
            ResetHiddenObjects();

            UpdateHiddenHandlers();
        }

        private void UpdateIgnoredObjects(HidingHandler current)
        {
            if (_ignoredObjectsCache.Contains(current.ObjectHider))
            {
                return;
            }
            
            _ignoredObjectsCache.Add(current.ObjectHider);

            foreach (var s in current.Sockets)
            {
                foreach (var h in s.Handlers)
                {
                    if (h.GetPieceType == HidingHandler.PieceType.Floor && h != current)
                    {
                        UpdateIgnoredObjects(h);
                        break;
                    }
                }
            }
        }

        private void HideHidingHandlers(HidingHandler current)
        {
            GetTargetHidingHandlers(current, ref _hiddenHandlersCache);

            foreach (var h in _hiddenHandlersCache.Where(h => !_hiddenHandlers.Contains(h)))
            {
                h.ObjectHider.ReduceTransparency(_handlersHidingTransparency);

                _hiddenHandlers.Add(h);
            }
        }
        
        private void GetTargetHidingHandlers(HidingHandler current, ref List<HidingHandler> targets)
        {
            if (current.GetPieceType != HidingHandler.PieceType.Floor)
            {
                return;
            }
            
            if (targets.Contains(current))
            {
                return;
            }
            
            targets.Add(current);
                
            foreach (var s in current.Sockets)
            {
                if (s.Handlers.Any(h => h.GetPieceType == HidingHandler.PieceType.Wall))
                {
                    continue;
                }
                
                foreach (var h in s.Handlers)
                {
                    if (h != current && h.GetPieceType == HidingHandler.PieceType.Floor)
                    {
                        GetTargetHidingHandlers(h, ref targets);
                        break;
                    }
                }
            }
        }
        
        private void HideObjects(Vector3 position, GroupBehaviour group)
        {
            _hiddenObjectsCache = GetTargetObjectHiders(position, group);
            
            foreach (var h in _hiddenObjectsCache)
            {
                if (!_hiddenObjects.Contains(h))
                {
                    h.Hide(true);

                    _hiddenObjects.Add(h);
                }
            }
        }
        
        private List<ObjectHider> GetTargetObjectHiders(Vector3 position, GroupBehaviour group)
        {
            if (group.TryGetComponent<HidingHandlerGroup>(out var hidingHandlerGroup) &&
                hidingHandlerGroup.UseCustomHiderBoxes)
            {
                var output = new List<ObjectHider>();
                
                foreach (var b in hidingHandlerGroup.CustomHiderBoxes)
                {
                    if (b.transform.position.y < position.y)
                    {
                        continue;
                    }
                    
                    output.AddRange(PhysicUtils.GetTypesByBox<ObjectHider>(
                        b.transform.position,
                        b.transform.localScale / 2,
                        b.transform.rotation,
                        _layerMask,
                        QueryTriggerInteraction.Ignore).Where(h => !_ignoredObjectsCache.Contains(h)).ToList());
                }
                
                return output;
            }
            
            var groupBounds = ObjectUtils.GetBounds(group.gameObject);

            _hiderBox.transform.position = groupBounds.center;
            _hiderBox.transform.up = group.transform.up;
            _hiderBox.transform.localScale = groupBounds.size;
            
            _hitPoint.transform.position = position;
            
            _botPoint.transform.localPosition = new Vector3(
                _hitPoint.transform.localPosition.x,
                _botPoint.transform.localPosition.y,
                _hitPoint.transform.localPosition.z);
                
            var hiderBoxPosition = _hiderBox.transform.position += _hiderBox.transform.up * 
                Vector3.Distance(
                    _botPoint.transform.position,
                    _hitPoint.transform.position) + _boxCastOffset;
            
            return PhysicUtils.GetTypesByBox<ObjectHider>(
                hiderBoxPosition,
                _hiderBox.transform.localScale / 2,
                _hiderBox.transform.rotation,
                _layerMask,
                QueryTriggerInteraction.Ignore).Where(h => !_ignoredObjectsCache.Contains(h)).ToList();
        }

        private void ClearCache()
        {
            _hiddenHandlersCache.Clear();
            _hiddenObjectsCache.Clear();
            _ignoredObjectsCache.Clear();
        }
        
        private void ResetHidden()
        {
            _botPoint.transform.localPosition = _botPointPosition;
            _hitPoint.transform.localPosition = Vector3.zero;

            ClearCache();
            
            ResetHiddenHandlers();
            ResetHiddenObjects();
        }

        private void ResetHiddenHandlers()
        {
            for (var i = _hiddenHandlers.Count - 1; i >= 0; i--)
            {
                if (_hiddenHandlers[i] == null)
                {
                    _hiddenHandlers.RemoveAt(i);
                    continue;
                }

                if (!_hiddenHandlersCache.Contains(_hiddenHandlers[i]))
                {
                    _hiddenHandlers[i].ObjectHider.ResetTransparency();
                    _hiddenHandlers.RemoveAt(i);
                }
            }
        }

        private void ResetHiddenObjects()
        {
            for (var i = _hiddenObjects.Count - 1; i >= 0; i--)
            {
                if (_hiddenObjects[i] == null)
                {
                    _hiddenObjects.RemoveAt(i);
                    continue;
                }

                if (!_hiddenObjectsCache.Contains(_hiddenObjects[i]))
                {
                    _hiddenObjects[i].Hide(false);
                    _hiddenObjects.RemoveAt(i);
                }
            }
        }

        private void UpdateHiddenHandlers()
        {
            foreach (var h in _hiddenHandlers)
            {
                h.ObjectHider.ReduceTransparency(HidingMode == HandlersHidingMode.Full ? 0 : _handlersHidingTransparency);
            }
        }
        
        #region Utilities

        private IReadOnlyList<RaycastHit> Raycast(CastDirection castDirection)
        {
            var direction = Vector3.up;

            switch (castDirection)
            {
                case CastDirection.Up:
                {
                    direction = Vector3.up;
                    break;
                }
                case CastDirection.GroupDirection:
                { 
                    direction = GetGroupDirection();
                    break;
                }
            }
     
            return Physics.SphereCastAll(
                transform.position + _sphereCastOffset,
                _sphereCastRadius,
                direction,
                _sphereCastLenght,
                _layerMask,
                QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToList();
        }
        
        private Vector3 GetGroupDirection()
        {
            var hits = Raycast(CastDirection.Up);

            foreach (var h in hits)
            {
                if (ComponentUtils.TryGetComponentInParent<GroupBehaviour>(h.transform, out var group))
                {
                    return group.transform.up;
                }
            }

            return Vector3.up;
        }
        
        #endregion
    }
}
