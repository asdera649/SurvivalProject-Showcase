using System.Collections.Generic;
using EasyBuildSystem.Features.Scripts.Core.Base.Event;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using EasyBuildSystem.Features.Scripts.Core.Base.Socket;
using EasyBuildSystem.Features.Scripts.Extensions;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Hiding
{
    public class Socket : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _radius = 0.5f;

        private readonly List<HidingHandler> _handlers = new();
        public IReadOnlyList<HidingHandler> Handlers => _handlers;
        
        private void OnEnable()
        {
            BuildEvent.Instance.OnPieceInstantiated.AddListener(OnPieceInstantiated);
            BuildEvent.Instance.OnPieceDestroyed.AddListener(OnPieceDestroyed);
        }

        private void Start()
        {
            UpdateSocket();
        }

        private void OnDisable()
        {
            BuildEvent.Instance.OnPieceInstantiated.RemoveListener(OnPieceInstantiated);
            BuildEvent.Instance.OnPieceDestroyed.RemoveListener(OnPieceDestroyed);
        }
        
        private void UpdateSocket()
        {
            _handlers.Clear();

            var handlers = PhysicExtension.GetNeighborsTypeBySphere<HidingHandler>(
                transform.position,
                _radius,
                -1,
                QueryTriggerInteraction.Ignore);

            _handlers.AddRange(handlers);
        }

        #region Callbacks
        
        private void OnPieceInstantiated(PieceBehaviour piece, SocketBehaviour socket)
        {
            UpdateSocket();
        }

        private void OnPieceDestroyed(PieceBehaviour piece)
        {
            UpdateSocket();
        }
        
        #endregion
    }
}
