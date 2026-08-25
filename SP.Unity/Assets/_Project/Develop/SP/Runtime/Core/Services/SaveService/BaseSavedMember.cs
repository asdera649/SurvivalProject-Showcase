using System;
using CI.QuickSave.Core.Serialisers;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace SP.Runtime.Core.Services.SaveService
{
    [Serializable]
    public class BaseSavedMember
    {
        public BaseSavedMember(string memberName, object value)
        {
            MemberName = memberName;
            Value = JsonSerialiser.SerialiseKey(value);
        }
        
        public string MemberName { get; }
        public JToken Value { get; }

        public bool TryReadValue(Type objectType, out object output)
        {
            output = null;
                
            output = JsonSerialiser.Deserialise(objectType, Value);

            if (output == null)
            {
                Debug.LogWarning("Couldn't read the value!");
            }
                
            return output != null;
        }
    }
}