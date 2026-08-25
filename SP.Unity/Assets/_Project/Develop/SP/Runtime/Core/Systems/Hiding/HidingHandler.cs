using System.Collections.Generic;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Hiding
{
    [RequireComponent(typeof(ObjectHider))]
    public class HidingHandler : MonoBehaviour
    {
        #region Structs
        
        public enum PieceType
        { 
            Wall,
            Floor,
            Foundation
        }
        
        #endregion

        [Header("Settings")]
        [SerializeField] private PieceType _pieceType;
        public PieceType GetPieceType => _pieceType;

        private readonly List<Socket> _sockets = new();
        public IReadOnlyList<Socket> Sockets => _sockets;

        private ObjectHider _objectHider;
        public ObjectHider ObjectHider
        {
            get
            {
                if (_objectHider == null)
                {
                    _objectHider = GetComponent<ObjectHider>();
                }

                return _objectHider;
            }
        }

        private void Awake()
        {
            _sockets.AddRange(GetComponentsInChildren<Socket>());
        }
    }
}
