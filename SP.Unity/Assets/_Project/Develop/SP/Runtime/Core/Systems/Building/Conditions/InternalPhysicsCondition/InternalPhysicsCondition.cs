using System.Collections.Generic;
using System.Linq;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition.Enums;
using EasyBuildSystem.Features.Scripts.Core.Base.Manager;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece.Enums;
using SP.Runtime.Core.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalPhysicsCondition
{
    [Condition(
        "Internal Physics Condition",
        "Check and denies the actions, if this piece is not stable.",
        ConditionTarget.PieceBehaviour)]
    public class InternalPhysicsCondition : ConditionBehaviour
    {
        public static event UnityAction<InternalPhysicsCondition> DestroyAction;

        [Header("Settings")]
        [SerializeField] private InternalPhysicsConditionSettings _conditionSettings;

        private float _stability;
        
        private readonly List<InternalPhysicsCondition> _attachedPieces = new();
        
        #region Cached

        private Bounds _cachedBounds;
        
        private readonly Queue<InternalPhysicsCondition> _piecesQueue = new();
        private readonly HashSet<InternalPhysicsCondition> _visitedPieces = new();

        private UnityEngine.Camera _mainCamera;
        
        #endregion

        private void Awake()
        {
            _cachedBounds = ObjectUtils.GetBounds(gameObject);
            
            _mainCamera = UnityEngine.Camera.main;
        }

        private void Start()
        {
            if (Piece.CurrentState != StateType.Placed)
            {
                return;
            }
            
            UpdateAttachedPieces();

            foreach (var p in _attachedPieces)
            {
                p.UpdateAttachedPieces();
            }

            PropagateStabilityFrom(this);
        }

        private void OnDestroy()
        {
            if (Piece.CurrentState != StateType.Placed)
            {
                return;
            }
            
            UpdateAttachedPieces();
            
            foreach (var p in _attachedPieces)
            {
                p.UpdateAttachedPieces();
            }
            
            ResetStabilityFrom(this);
            
            foreach (var p in _attachedPieces)
            {
                PropagateStabilityFrom(p);
            }

            DestroyAction?.Invoke(this);
        }

        // For debug stability
        // private void OnGUI()
        // {
        //     var screenPos = _mainCamera.WorldToScreenPoint(transform.position);
        //     
        //     if (screenPos.z < 0)
        //     {
        //         return;
        //     }
        //     
        //     screenPos.y = Screen.height - screenPos.y;
        //     
        //     var rect = new Rect(screenPos.x, screenPos.y, 100, 20);
        //     
        //     GUI.Label(rect, _stability.ToString());
        // }

        private void UpdateAttachedPieces()
        {
            _attachedPieces.Clear();
            
            foreach (var d in _conditionSettings.Detections)
            {
                var physicsConditions = GetTypesByBox<InternalPhysicsCondition>(d);
                
                foreach (var c in physicsConditions)
                {
                    if (c != this && !_attachedPieces.Contains(c))
                    {
                        _attachedPieces.Add(c);
                    } 
                }
            }
        }
        
        private void ResetStabilityFrom(InternalPhysicsCondition root)
        {
            _piecesQueue.Clear();
            _visitedPieces.Clear();
            
            _piecesQueue.Enqueue(root);

            while (_piecesQueue.Count > 0)
            {
                var current = _piecesQueue.Dequeue();
                
                current._stability = current._conditionSettings.IsSupportPiece ? 100f : 0f;

                _visitedPieces.Add(current);

                foreach (var p in current._attachedPieces)
                {
                    if (!_visitedPieces.Contains(p))
                    {
                        _piecesQueue.Enqueue(p);
                    }
                }
            }
        }

        private void PropagateStabilityFrom(InternalPhysicsCondition root)
        {
            _piecesQueue.Clear();
            _visitedPieces.Clear();
            
            _piecesQueue.Enqueue(root);

            while (_piecesQueue.Count > 0)
            {
                var current = _piecesQueue.Dequeue();

                var oldStability = current._stability;
                
                if (current._conditionSettings.IsSupportPiece)
                {
                    current._stability = 100f;
                }
                else
                {
                    var maxFromNeighbors = 0f;

                    foreach (var p in current._attachedPieces)
                    {
                        var potential = p._stability - current._conditionSettings.StabilityFalloff;

                        if (potential > maxFromNeighbors)
                        {
                            maxFromNeighbors = potential;
                        }
                    }

                    current._stability = Mathf.Max(0, maxFromNeighbors);
                }
                
                _visitedPieces.Add(current);

                if (oldStability != current._stability)
                {
                    foreach (var p in current._attachedPieces)
                    {
                        _visitedPieces.Remove(p);
                    }
                }

                foreach (var p in current._attachedPieces)
                {
                    if (!_visitedPieces.Contains(p))
                    {
                        _piecesQueue.Enqueue(p);
                    }
                }
            }
        }
        
        public override bool CheckForPlacement()
        {
            return CheckStability();
        }
        
        private bool CheckStability()
        {
            if (!_conditionSettings.RequireStableSupport)
            {
                return true;
            }
        
            if (_conditionSettings.Detections.Count == 0)
            {
                return false;
            }
            
            UpdateAttachedPieces();
        
            if (CheckByStability())
            {
                return true;
            }
        
            if (CheckByTerrainSupport())
            {
                return true;
            }
            
            if (CheckByRequiredTypes())
            {
                return true;
            }
        
            return false;
        }
        
        private bool CheckByStability()
        {
            foreach (var p in _attachedPieces)
            {
                if (p._stability >= _conditionSettings.MinRequiredStability)
                {
                    return true;
                }
            }

            return false;
        }
        
        private bool CheckByTerrainSupport()
        {
            if (_conditionSettings.RequireTerrainSupport)
            {
                foreach (var d in _conditionSettings.Detections)
                {
                    var colliders = GetTypesByBox<Collider>(d);

                    foreach (var c in colliders)
                    {
                        if (BuildManager.Instance.IsSupport(c))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        
        private bool CheckByRequiredTypes()
        {
            if (_conditionSettings.RequiredTypes.Count > 0)
            {
                foreach (var c in _attachedPieces)
                {
                    if (_conditionSettings.ContainsType(c.Piece.Type))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        #region Utilities
        
        public void GetDependents(ref List<InternalPhysicsCondition> outputs)
        {
            outputs.Clear();
            
            _piecesQueue.Clear();
            _visitedPieces.Clear();
            
            _piecesQueue.Enqueue(this);

            while (_piecesQueue.Count > 0)
            {
                var current = _piecesQueue.Dequeue();

                if (!outputs.Contains(current))
                {
                    outputs.Add(current);
                }
                
                // if (current != this && current._attachedPieces.All(
                //         p => p._stability < current._conditionSettings.MinRequiredStability))
                // {
                //     goto ret;
                // }

                _visitedPieces.Add(current);

                foreach (var p in current._attachedPieces)
                {
                    if (!_visitedPieces.Contains(p))
                    {
                        _piecesQueue.Enqueue(p);
                    }
                }
            }

            //ret:
            
            outputs.Remove(this);
        }
        
        public void GetNearest(ref List<InternalPhysicsCondition> outputs)
        {
            outputs.Clear();

            outputs.AddRange(PhysicUtils.GetTypesByBox<InternalPhysicsCondition>(
                _cachedBounds.center,
                IncreaseVector(_cachedBounds.extents, 0.1f),
                transform.rotation,
                -1,
                QueryTriggerInteraction.Ignore));
        }
        
        private Vector3 IncreaseVector(Vector3 vector, float percent)
        {
            vector.x *= 1 + percent;
            vector.y *= 1 + percent;
            vector.z *= 1 + percent;

            return vector;
        }
        
        private IReadOnlyList<T> GetTypesByBox<T>(InternalPhysicsConditionSettings.Detection detection)
        {
            var temp = transform;
            
            return PhysicUtils.GetTypesByBox<T>(
                temp.TransformPoint(detection.DetectionBounds.center),
                detection.DetectionBounds.extents,
                temp.rotation,
                detection.LayerMask,
                QueryTriggerInteraction.Ignore);
        }
        
        #endregion
    }
}