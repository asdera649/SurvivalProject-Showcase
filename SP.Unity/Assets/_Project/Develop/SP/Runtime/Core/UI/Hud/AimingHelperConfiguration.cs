using System;
using System.Collections.Generic;
using System.Linq;
using SP.Runtime.Core.Entities;
using SP.Runtime.Core.Items;
using SP.Runtime.Utilities.SerializableDictionary;
using UnityEngine;

namespace SP.Runtime.Core.UI.Hud
{
    [CreateAssetMenu(menuName = "Hud/AimingHelperConfiguration")]
    public class AimingHelperConfiguration : ScriptableObject
    {
        [Serializable]
        public class ItemContainer
        {
            [SerializeField] private List<BaseEntity> _targetTypes = new();
            public IReadOnlyList<BaseEntity> TargetTypes => _targetTypes;
        
            [SerializeField] private float _targetDistance;
            public float TargetDistance => _targetDistance;

            public bool ContainsTargetType(BaseEntity target)
            {
                return _targetTypes.Any(t => target.GetType() == t.GetType());
            }
        }
        
        [SerializeField] private SerializableDictionary<BaseItem, ItemContainer> _aimingHelperConfigurations = new();

        public bool TryGetAimingHelperConfiguration(BaseItem targetItem, out ItemContainer aimingHelperConfiguration)
        {
            aimingHelperConfiguration = null;
            
            foreach (var c in _aimingHelperConfigurations)
            {
                if (c.Key.Equals(targetItem))
                {
                    aimingHelperConfiguration = c.Value;
                    return true;
                }
            }
            
            return false;
        }
    }
}