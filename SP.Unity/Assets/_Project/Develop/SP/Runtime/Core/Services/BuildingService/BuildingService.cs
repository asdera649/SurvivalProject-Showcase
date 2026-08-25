using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EasyBuildSystem.Features.Scripts.Core.Base.Condition;
using EasyBuildSystem.Features.Scripts.Core.Base.Event;
using EasyBuildSystem.Features.Scripts.Core.Base.Group;
using EasyBuildSystem.Features.Scripts.Core.Base.Manager;
using EasyBuildSystem.Features.Scripts.Core.Base.Piece;
using EasyBuildSystem.Features.Scripts.Core.Base.Socket;
using EasyBuildSystem.Features.Scripts.Core.Base.Socket.Data;
using Mirror;
using SP.Runtime.Core.Systems.Building.Conditions.InternalPhysicsCondition;
using SP.Runtime.Core.Utilities;
using SP.Runtime.LoadingService;
using SP.Runtime.Utilities;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SP.Runtime.Core.Services.BuildingService
{
    [RequireComponent(typeof(BuildManager), typeof(BuildEvent))]
    public class BuildingService : MonoBehaviour, ILoadUnit
    {
        [Header("Settings")] 
        [SerializeField] private float _maxBuildingDistance = 6;
        public float MaxBuildingDistance => _maxBuildingDistance;
        
        [SerializeField] private bool _checkStabilityAfterDestroy = true;
        
        private string GroupName => "Group (" + GetInstanceID() + ")";
        
        private readonly List<PieceBehaviour> _instancedPieces = new();
        
        private readonly Dictionary<uint, PieceBehaviour> _piecesCache = new();

        #region Cache

        private List<InternalPhysicsCondition> cacheDependents = new();

        #endregion
        
        private BuildManager _buildManager;
        public BuildManager BuildManager
        {
            get
            {
                if (_buildManager == null)
                {
                    _buildManager = GetComponent<BuildManager>();
                }

                return _buildManager;
            }
        }
        
        private BuildEvent _buildEvent;
        private BuildEvent BuildEvent
        {
            get
            {
                if (_buildEvent == null)
                {
                    _buildEvent = GetComponent<BuildEvent>();
                }

                return _buildEvent;
            }
        }
        
        private IObjectResolver _container;
        
        [Inject]
        private void Inject(IObjectResolver container)
        {
            _container = container;
        }
        
        public UniTask Load()
        {
            RegisterSpawnHandlers();
            
            return UniTask.CompletedTask;
        }
        
        private void OnEnable()
        {
            InternalPhysicsCondition.DestroyAction += OnDestroyPhysicsCondition;
            BuildEvent.OnPieceDestroyed.AddListener(OnPieceDestroyed);
        }
        
        private void OnDisable()
        {
            InternalPhysicsCondition.DestroyAction -= OnDestroyPhysicsCondition;
            BuildEvent.OnPieceDestroyed.RemoveListener(OnPieceDestroyed);
        }

        #region CustomSpawnHandler

        [ClientCallback]
        private void RegisterSpawnHandlers()
        {
            var pieces = BuildManager.Pieces;

            foreach (var p in pieces)
            {
                if (p.TryGetComponent<NetworkIdentity>(out var identity))
                {
                    _piecesCache.Add(identity.assetId, p);
                    
                    NetworkClient.UnregisterPrefab(p.gameObject);
                    NetworkClient.RegisterSpawnHandler(identity.assetId, SpawnHandler, UnSpawnHandler);
                }
                else
                {
                    Debug.LogWarning("Piece: " + p.gameObject.name + " don't have NetworkIdentity!");
                }
            }
        }

        private GameObject SpawnHandler(SpawnMessage msg)
        {
            GameObject output = null;
            
            if (_piecesCache.TryGetValue(msg.assetId, out var value))
            {
                output = PlacePiece(value, msg.position, msg.rotation.eulerAngles, msg.scale).gameObject;
            }
            else
            {
                Debug.LogWarning("The _piecesCache does not contain such an assetId");
            }
            
            return output;
        }

        private void UnSpawnHandler(GameObject spawned)
        {
            Destroy(spawned);
        }
        
        #endregion
        
        private bool CanPlaceBefore(PieceBehaviour piece, SocketBehaviour socket)
        {
            return !((piece.RequireSocket && socket == null) || (socket != null && socket.CheckOccupancy(piece)));
        }

        private bool CanPlaceAfter(PieceBehaviour piece, out ConditionBehaviour reason)
        {
            reason = null;
            
            foreach (var c in piece.Conditions.Where(c => !c.CheckForPlacement()))
            {
                reason = c;
                return false;
            }
            
            return true;
        }
        
        public bool CanPlace(PieceBehaviour piece, SocketBehaviour socket, out ConditionBehaviour reason)
        {
            reason = null;
            
            return CanPlaceBefore(piece, socket) && CanPlaceAfter(piece, out reason);
        }

        [ServerCallback]
        public bool TryPlacePiece(
            int pieceId,
            Vector3 position,
            Vector3 rotation,
            SocketBehaviour socket,
            out PieceBehaviour output)
        {
            output = null;
            
            return TryGetPieceById(pieceId, out var piece) &&
                   TryPlacePiece(piece, position, rotation, socket, out output);
        }

        [ServerCallback]
        private bool TryPlacePiece(
            PieceBehaviour piece,
            Vector3 position,
            Vector3 rotation,
            SocketBehaviour socket,
            out PieceBehaviour output)
        {
            output = null;
            
            if (CanPlaceBefore(piece, socket))
            {
                var scale = piece.transform.localScale;
                
                if (socket != null)
                {
                    if (TryGetSocketOffset(socket, piece, out var offset))
                    {
                        var eulerAngles = socket.transform.eulerAngles;
                            
                        position = Quaternion.Euler(eulerAngles) * offset.Position + socket.transform.position;
                        rotation = eulerAngles + offset.Rotation;
                        scale = offset.Scale;
                    }
                }
                
                output = PlacePiece(piece, position, rotation, scale);
                
                if (CanPlaceAfter(output, out _))
                {
                    _instancedPieces.Add(output);
                    NetworkServer.Spawn(output.gameObject);
                }
                else
                {
                    Destroy(output.gameObject);
                    output = null;
                }
            }

            return output != null;
        }

        public PieceBehaviour PlacePiece(PieceBehaviour piece, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var output = BuildManager.PlacePrefab(piece, position, rotation, scale, null, null, false);
            
            _container.InjectGameObject(output.gameObject);
            
            UpdateGroup(output);

            return output;
        }

        #region Group
        
        private void UpdateGroup(PieceBehaviour piece)
        {
            var sockets = piece.Sockets;

            List<GroupBehaviour> groups = new();
            
            foreach (var s in sockets)
            {
                var pieces = PhysicUtils.GetTypesBySphere<PieceBehaviour>(
                    s.transform.position,
                    s.Radius, 
                    -1, 
                    QueryTriggerInteraction.Ignore);
                
                foreach (var p in pieces)
                {
                    if (ComponentUtils.TryGetComponentInParent<GroupBehaviour>(p, out var group))
                    {
                        if (!groups.Contains(group))
                        {
                            groups.Add(group);
                        }
                    }
                }
            }

            var targetGroup = groups.OrderByDescending(i => i.transform.childCount).FirstOrDefault();

            if (targetGroup != null)
            {
                AddToGroup(piece.transform, targetGroup);
                
                for (var i = groups.Count - 1; i >= 0; i--)
                {
                    if (groups[i] == targetGroup)
                    {
                        continue;
                    }

                    for (var c = groups[i].transform.childCount - 1; c >= 0; c--)
                    {
                        AddToGroup(groups[i].transform.GetChild(c), targetGroup);
                    }
                    
                    Destroy(groups[i].gameObject);
                    groups.RemoveAt(i);
                }
            }
            else
            {
                AddToGroup(piece.transform, InstantiateGroup());
            }
        }
        
        private GroupBehaviour InstantiateGroup()
        { 
            // Проблема: На сервере все PieceBehaviour являются дочерними объеками GroupBehaviour,
            // из за того, что, почему то Mirror стал отправлять в SpawnMessage локальную позицию, а не мировую,
            // как раньше, PieceBehaviour спавнятся на сервер и на клиенте в разных местах.
            // Решение: что бы решить эту проблему, пришлось и на серверe и на клиенте спавнить
            // GroupBehaviour в нулевую мировую позицию, таким образом и сервер как будто отправляет 
            // позицию PieceBehaviour в мировых координатах(из за того что родитель находится в нулевой позиции),
            // и клиент спавнит PieceBehaviour в мировых координатах(хотя они локальные).
            
            var output = new GameObject(GroupName).AddComponent<GroupBehaviour>();
            
            BuildEvent.OnGroupInstantiated.Invoke(output);

            return output;
        }

        private void AddToGroup(Transform t, GroupBehaviour group)
        {
            t.SetParent(group.transform, true);
             
            BuildEvent.OnGroupUpdated.Invoke(group); 
        }
        
        #endregion

        #region Callbacks

        [ServerCallback]
        private void OnDestroyPhysicsCondition(InternalPhysicsCondition physicsCondition)
        {
            if (_checkStabilityAfterDestroy)
            {
                physicsCondition.GetDependents(ref cacheDependents);

                CheckConditions(cacheDependents);

                physicsCondition.GetNearest(ref cacheDependents);
                
                CheckConditions(cacheDependents);
            }
        }

        [ServerCallback]
        private void CheckConditions(List<InternalPhysicsCondition> physicsConditions)
        {
            foreach (var n in physicsConditions.Where(
                         n => n != null && !n.CheckForPlacement()))
            {
                NetworkServer.Destroy(n.gameObject);
            }
        }

        [ServerCallback]
        private void OnPieceDestroyed(PieceBehaviour piece)
        {
            if (_instancedPieces.Contains(piece))
            {
                _instancedPieces.Remove(piece);
            }
        }

        #endregion
        
        #region Utilities
        
        private bool TryGetPieceById(int id, out PieceBehaviour output)
        {
            output = BuildManager.GetPiece(id);

            return output != null;
        }

        private bool TryGetSocketOffset(SocketBehaviour socket, PieceBehaviour piece, out Offset output)
        {
            output = socket.PartOffsets.FirstOrDefault(o => o.Piece.Id == piece.Id);

            return output != null;
        }

        #endregion
    }
}

