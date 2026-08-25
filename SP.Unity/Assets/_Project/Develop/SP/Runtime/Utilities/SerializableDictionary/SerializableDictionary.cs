using System;
using System.Collections.Generic;
using UnityEngine;

namespace SP.Runtime.Utilities.SerializableDictionary
{
    #region Structs
    
    [Serializable]
    public class SerializableKVP<K, V>
    {
        public SerializableKVP(K key, V value)
        {
            _key = key;
            _value = value;
        }
        
        [SerializeField] private K _key;
        public K Key => _key;

        [SerializeField] private V _value;
        public V Value => _value;
        
        public V this[K key] => Value;
    }
    
    #endregion
    
    [Serializable]
    public class SerializableDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKVP<K, V>> _keys = new();
        
        public new void Add(K key, V value)
        {
            base.Add(key, value);
            
            _keys.Add(new SerializableKVP<K, V>(key, value));
        }
        
        public new void Clear()
        {
            base.Clear();
            
            _keys.Clear();
        }

        public void OnBeforeSerialize()
        {
            if (!(Count > _keys.Count))
            {
                return;
            }
            
            foreach (var kvp in this)
            {
                _keys.Add(new SerializableKVP<K, V>(kvp.Key, kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            UpdateDictionaryInternal();
        }

        private void UpdateDictionaryInternal()
        {
            base.Clear();
            
            foreach (var kvp in _keys)
            {
                if (kvp == null)
                {
                    continue;
                }

                if (kvp.Key == null)
                {
                    continue;
                }

                if (kvp.Value == null)
                {
                    continue;
                }

                if (ContainsKey(kvp.Key))
                {
                    continue;
                }

                AddInternal(kvp.Key, kvp.Value);
            }
        }
        
        private void AddInternal(K key, V value)
        {
            base.Add(key, value);
        }
    }
}
