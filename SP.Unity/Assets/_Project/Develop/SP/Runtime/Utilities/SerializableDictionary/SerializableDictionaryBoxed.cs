using System;
using System.Collections.Generic;
using UnityEngine;

namespace SP.Runtime.Utilities.SerializableDictionary
{
    #region Structs
    
    [Serializable]
    public class SerializableKVPBoxed<K, V> : List<V>
    {
        public SerializableKVPBoxed(K key, List<V> values)
        {
            _key = key;
            _values = values;
        }
        
        public SerializableKVPBoxed(K key, V value)
        {
            _key = key;
            
            _values = new List<V>
            {
                value
            };
        }
        
        [SerializeField] private K _key;
        public K Key => _key;
        
        [SerializeField] private List<V> _values;
        public List<V> Values => _values;
        
        public V this[K key, int index] => Values[index];
        
        public List<V> this[K key] => Values;
        
        public V Value => _values[0];
    }
    
    #endregion
    
    [Serializable]
    public class SerializableDictionaryBoxed<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKVPBoxed<K, V>> _keys = new();

        public new void Add(K key, V value)
        {
            base.Add(key, value);
            
            _keys.Add(new SerializableKVPBoxed<K, V>(key, value));
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
                _keys.Add(new SerializableKVPBoxed<K, V>(kvp.Key, kvp.Value));
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

                if (kvp.Values == null)
                {
                    continue;
                }

                if (ContainsKey(kvp.Key))
                {
                    continue;
                }

                AddInternal(kvp.Key, kvp.Value);
                
                kvp.AddRange(kvp.Values);
                kvp.Values.CopyTo(kvp.ToArray());
            }
        }
        
        private void AddInternal(K key, V value)
        {
            base.Add(key, value);
        }
    }
}