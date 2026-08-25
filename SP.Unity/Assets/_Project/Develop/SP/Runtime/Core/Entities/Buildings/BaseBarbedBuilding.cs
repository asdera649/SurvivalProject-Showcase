using System;
using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using SP.Runtime.Core.Movement;
using UnityEngine;
using Mirror;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Systems.ValueContainer;


namespace SP.Runtime.Core.Entities.Buildings
{
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public class BaseBarbedBuilding : BuildingEntity
    {
        #region Structs
        
        private class Containers
        {
            public Container<float>.Element FloatContainer;
            public Container<bool>.Element BoolContainer;
        }
        
        #endregion

        [Header("References")] 
        [SerializeField] private StudioEventEmitter _takingDamageSource;
        
        [Header("Settings")] 
        [SerializeField, Range(0, 1)] private float _slowDown = 0.8f;

        [SerializeField] private int _damagePerHit = 5;
        [SerializeField] private float _damageInterval = 0.5f;
        
        private readonly List<CharacterMotor> _localTrappedMotorsCount = new();
        
        private static readonly Dictionary<CharacterMotor, uint> _trappedMotorsCount = new();
        
        private static readonly Dictionary<CharacterMotor, Containers> _trappedMotors = new();
        
        private static readonly Dictionary<Character, (Vector3, float)> _motorsLastPosition = new();
        
        public override void OnStopClient()
        {
            foreach (var m in _localTrappedMotorsCount.Where(
                         m => _trappedMotorsCount.ContainsKey(m)))
            {
                _trappedMotorsCount[m] = Math.Max(0, _trappedMotorsCount[m] - 1);
            }
            
            UpdateTrappedMotors();

            base.OnStopClient();
        }
        
        [ClientCallback]
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out CharacterMotor motor))
            {
                _localTrappedMotorsCount.Add(motor);
                
                if (!_trappedMotorsCount.TryAdd(motor, 1))
                {
                    _trappedMotorsCount[motor]++;
                }
                
                UpdateTrappedMotors();
            }
        }

        [ClientCallback]
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out CharacterMotor motor))
            {
                _localTrappedMotorsCount.Remove(motor);
                
                if (_trappedMotorsCount.ContainsKey(motor))
                {
                    _trappedMotorsCount[motor] = Math.Max(0, _trappedMotorsCount[motor] - 1);
                }

                UpdateTrappedMotors();
            }
        }

        [ClientCallback]
        private void UpdateTrappedMotors()
        {
            // Удаление
            
            var motorsToRemove = _trappedMotorsCount
                .Where(kvp => kvp.Value == 0)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var m in motorsToRemove)
            {
                _trappedMotorsCount.Remove(m);
                RemoveMotorFromTrapped(m);
            }
            
            // Добавление
            
            foreach (var m in _trappedMotorsCount)
            {
                AddMotorToTrapped(m.Key);
            }
        }

        [ClientCallback]
        private void AddMotorToTrapped(CharacterMotor motor)
        {
            if (_trappedMotors.ContainsKey(motor))
            {
                return;
            }
            
            var floatContainer = motor.SlowDownsContainer.Add(_slowDown);
            var boolContainer = motor.JumpLockContainer.Add(true);
            
            _trappedMotors[motor] = new Containers { FloatContainer = floatContainer, BoolContainer = boolContainer };
        }
        
        [ClientCallback]
        private void RemoveMotorFromTrapped(CharacterMotor motor)
        {
            if (_trappedMotors.TryGetValue(motor, out var container))
            {
                motor.SlowDownsContainer.Remove(container.FloatContainer);
                motor.JumpLockContainer.Remove(container.BoolContainer);
                
                _trappedMotors.Remove(motor);
            }
        }

        [ServerCallback]
        private void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent(out Character character))
            {
                if (_motorsLastPosition.TryGetValue(character, out var data))
                {
                    _motorsLastPosition[character] = (character.transform.position, data.Item2);
                    
                    if (character.transform.position == data.Item1)
                    {
                        return;
                    }

                    if (Time.time - data.Item2 < _damageInterval)
                    {
                        return;
                    }
                }
                
                character.TakeDamage(new DamageSenderInfo(this), _damagePerHit, DeathMethods.Null);

                RpcInvokeCutImpacts();
                
                if (!_motorsLastPosition.TryAdd(character, (character.transform.position, Time.time)))
                {
                    _motorsLastPosition[character] = (character.transform.position, Time.time);
                }
            }
        }

        [ClientRpc]
        private void RpcInvokeCutImpacts()
        {
            _takingDamageSource.Play();
        }
    }
}