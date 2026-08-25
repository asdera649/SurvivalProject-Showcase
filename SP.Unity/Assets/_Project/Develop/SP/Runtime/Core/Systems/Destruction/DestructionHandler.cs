using System.Collections.Generic;
using SP.Runtime.Core.Services;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Destruction
{
    public class DestructionHandler : MonoBehaviour
    {
        #region Structs
    
        private enum DestructionPoint
        {
            Center,
            Pivot
        }

        [System.Serializable]
        private class DebrisPieceContainer
        {
            [SerializeField] private DebrisPiece _debrisPiecePrefab;
            public DebrisPiece DebrisPiecePrefab => _debrisPiecePrefab;
            
            [SerializeField] private Transform _spawnPoint;
            public Transform SpawnPoint => _spawnPoint;
        }
    
        #endregion

        [Header("Prefabs")]
        [SerializeField] private DebrisPieceContainer[] _debrisPieces;

        [Header("Settings")]
        [SerializeField] private DestructionPoint _destructionPoint = DestructionPoint.Center;
        [SerializeField] private Transform _customDestructionPoint;
        [SerializeField] private bool _addExplosionForce = true;
        [SerializeField] private bool _addTorque = true;
        [SerializeField] private float _explosionForce = 15;
        [SerializeField] private float _explosionRadius;
        [SerializeField] private float _upwardsModifier = 3;
        
        public DebrisPiece[] Destruct(Vector3 customTorqueDirection = default)
        {
            List<DebrisPiece> output = new();
            
            var explosionPosition = transform.position;

            if (_destructionPoint == DestructionPoint.Center)
            {
                if (TryGetComponent(out Renderer r))
                {
                    explosionPosition = r.bounds.center;
                }
            }

            if (_customDestructionPoint != null)
            {
                explosionPosition = _customDestructionPoint.position;
            }
        
            foreach (var p in _debrisPieces)
            {
                if (Loader.Instance.PoolService.TrySpawnObject(
                        p.DebrisPiecePrefab,
                        p.SpawnPoint.position,
                        p.SpawnPoint.rotation,
                        out var debrisPiece))
                {
                    output.Add(debrisPiece);
                    
                    debrisPiece.transform.localScale = p.SpawnPoint.lossyScale;
                    
                    if (_addExplosionForce)
                    {
                        debrisPiece.Rigidbody.AddExplosionForce(
                            _explosionForce,
                            explosionPosition,
                            _explosionRadius,
                            _upwardsModifier,
                            ForceMode.Impulse);
                    }

                    if (_addTorque)
                    {
                        var torque = GetRandomTorque();

                        if (customTorqueDirection != Vector3.zero)
                        {
                            torque = customTorqueDirection.normalized;

                            var rigidbodyForward = debrisPiece.transform.up;
                            torque = Vector3.Cross(torque, rigidbodyForward);
                        }

                        debrisPiece.Rigidbody.AddTorque(torque, ForceMode.Impulse);
                    }
                }
            }

            return output.ToArray();
        }

        #region Utilities

        private Vector3 GetRandomTorque()
        { 
            return new Vector3(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1)) * 0.5f;
        }

        #endregion
    }
}
