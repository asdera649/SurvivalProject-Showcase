using System;
using System.Collections.Generic;
using System.Reflection;
using SP.Runtime.Utilities;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;

namespace SP.Runtime.Core.Services.SaveService
{
    public class SaveHandler : MonoBehaviour
    {
        #region Structs
        
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class SavedAttribute : Attribute { }
        
        #endregion

        [SerializeField] private string _guid;
        public string Guid => _guid;

        [Header("Settings")] 
        [SerializeField] private int _priority;
        public int Priority => _priority;

        [SerializeField] private bool _saveTransform = true;
        public bool SaveTransform => _saveTransform;

        public bool IsSavingSet { get; private set; }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (string.IsNullOrEmpty(_guid))
                {
                    _guid = System.Guid.NewGuid().ToString();
                }
            }
#endif
        }

        public IReadOnlyList<BaseSaveService.SavedMember> GetSavedMembers()
        {
            var output = new List<BaseSaveService.SavedMember>();
            
            var components = GetComponents<Component>();
            
            foreach (var c in components)
            {
                var members = c.GetType().GetMembersByAttribute(typeof(SavedAttribute));

                foreach (var m in members)
                {
                    object value;
                    
                    switch (m.MemberType)
                    {
                        case MemberTypes.Field:
                        {
                            var fieldInfo = (FieldInfo)m;
                            value = fieldInfo.GetValue(c);
                            break;
                        }
                        case MemberTypes.Property:
                        {
                            var propertyInfo = (PropertyInfo)m;
                            value = propertyInfo.GetValue(c);
                            break;
                        }
                        default:
                        {
                            throw new NotImplementedException();
                        }
                    }
                    
                    output.Add(new BaseSaveService.SavedMember(c.ToString(), m.Name, value));
                }
            }

            return output;
        }
        
        public void SetSavedMembers(IReadOnlyList<BaseSaveService.SavedMember> members)
        {
            var components = GetComponents<Component>();
            
             foreach (var m in members)
             {
                 foreach (var c in components)
                 {
                     if (c.ToString() == m.ComponentName)
                     {
                         if (c.GetType().TryGetMemberByName(m.MemberName, out var memberInfo))
                         {
                             switch (memberInfo.MemberType)
                             {
                                 case MemberTypes.Field:
                                 {
                                     var fieldInfo = (FieldInfo)memberInfo;
                                     
                                     if (m.TryReadValue(fieldInfo.FieldType, out var output))
                                     {
                                         fieldInfo.SetValue(c, output);
                                     }
                                     
                                     break;
                                 }
                                 case MemberTypes.Property:
                                 {
                                     var propertyInfo = (PropertyInfo)memberInfo;
                                     
                                     if (m.TryReadValue(propertyInfo.PropertyType, out var output))
                                     {
                                         propertyInfo.SetValue(c, output);
                                     }
                                     
                                     break;
                                 }
                                 default:
                                 {
                                     throw new NotImplementedException();
                                 }
                             }
                         }
                        
                         break;
                    }
                }
            }
            
            IsSavingSet = true;
            
            // Callbacks
            
            var handlers = GetComponents<ISaveHandler>();

            foreach (var h in handlers)
            {
                h.OnSetSavedElements();
            }
        }
    }
}
