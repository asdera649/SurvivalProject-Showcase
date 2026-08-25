using Mirror;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Water.BuoyancyObject
{
    [RequireComponent(typeof(Rigidbody))]
    public class BuoyancyObject : NetworkBehaviour
    {
        [Header("Settings")] 
        [SerializeField] private Transform[] _floaters;
        
        [Space(10)]
        
        [SerializeField] private float _underwaterDrag = 3;
        [SerializeField] private float _underwaterAngularDrag = 1;
        [SerializeField] private float _airDrag;
        [SerializeField] private float _airAngularDrag = 0.05f;
        [SerializeField] private float _floatingPower = 150;

        private bool _isUnderwater;

        private int _floatersUnderwater;

        private Rigidbody _rigidbody;
        private Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = GetComponent<Rigidbody>();
                }

                return _rigidbody;
            }
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (!isServer)
            {
                return;
            }
            
            _floatersUnderwater = 0;
            
            foreach (var f in _floaters)
            {
                if (Water.TryGetDistanceToWaterLevel(f.position, out var output))
                {
                    if (output > 0)
                    {
                        Rigidbody.AddForceAtPosition(
                            Vector3.up * _floatingPower * Mathf.Abs(output),
                            f.position,
                            ForceMode.Force);

                        _floatersUnderwater++;
                    
                        if (!_isUnderwater)
                        {
                            _isUnderwater = true;   
                            SwitchState(true);
                        }
                    }
                }
            }
            
            if (_isUnderwater && _floatersUnderwater == 0)
            {
                _isUnderwater = false;
                SwitchState(false);
            }
        }

        [ServerCallback]
        private void SwitchState(bool isUnderwater)
        {
            Rigidbody.drag = isUnderwater ? _underwaterDrag : _airDrag;
            Rigidbody.angularDrag = isUnderwater ? _underwaterAngularDrag : _airAngularDrag;
        }
    }
}