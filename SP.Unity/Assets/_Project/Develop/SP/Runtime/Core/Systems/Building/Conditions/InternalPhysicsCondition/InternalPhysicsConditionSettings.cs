using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SP.Runtime.Core.Systems.Building.Conditions.InternalPhysicsCondition
{
    [Serializable]
    [CreateAssetMenu(menuName = "ConditionSettings/InternalPhysicsConditionSettings")]
    public class InternalPhysicsConditionSettings : ScriptableObject
    {
        #region Structs
        
        [Serializable]
        public class Detection
        {
            [SerializeField] private Bounds _detectionBounds;
            public Bounds DetectionBounds => _detectionBounds;
            
            [FormerlySerializedAs("_requireLayer")] 
            [SerializeField] private LayerMask _layerMask;
            public LayerMask LayerMask => _layerMask;
        }
        
        #endregion
        
        [Header("General Settings")]
        [SerializeField] private bool _requireStableSupport = true;
        public bool RequireStableSupport => _requireStableSupport;
        
        [SerializeField] private List<Detection> _detections = new();
        public IReadOnlyList<Detection> Detections => _detections;
        
        [Header("Terrain Support Settings")]
        
        [SerializeField] private bool _requireTerrainSupport;
        public bool RequireTerrainSupport => _requireTerrainSupport;

        [Header("Stability Settings")]
        
        [SerializeField] private bool _isSupportPiece;
        public bool IsSupportPiece => _isSupportPiece;
        
        [SerializeField] private float _minRequiredStability = 50f;
        public float MinRequiredStability => _minRequiredStability;
        
        [SerializeField] private float _stabilityFalloff = 25f;
        public float StabilityFalloff => _stabilityFalloff;
        
        [Header("Required Types Settings")]
        
        [SerializeField] private List<string> _requiredTypes = new();
        public IReadOnlyList<string> RequiredTypes => _requiredTypes;
        
        public bool ContainsType(string type)
        {
            return RequiredTypes.Contains(type);
        }
    }
}