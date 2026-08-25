using System;
using System.Collections.Generic;
using System.Reflection;

namespace SP.Runtime.Utilities
{
    public static class ReflectionUtils
    {
        private static readonly BindingFlags BindingFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        #region Fields
        
        public static IReadOnlyList<FieldInfo> GetFieldsByAttribute(this Type objectType, Type attributeType)
        {
            var output = new List<FieldInfo>();

            while (true)
            {
                var fields = objectType.GetFields(BindingFlags);

                foreach (var f in fields)
                {
                    if (f.IsDefined(attributeType, true))
                    {
                        output.Add(f);
                    }
                }

                objectType = objectType.BaseType;

                if (objectType == null)
                {
                    break;
                }
            }
        
            return output;
        }
        
        public static bool TryGetFieldByName(this Type objectType, string name, out FieldInfo output)
        {
            output = null;
            
            while (true)
            {
                var fields = objectType.GetFields(BindingFlags);

                foreach (var f in fields)
                {
                    if (f.Name == name)
                    {
                        output = f;
                        break;
                    }
                }

                objectType = objectType.BaseType;

                if (objectType == null)
                {
                    break;
                }
            }
        
            return output != null;
        }
        
        // В будущем надо избавится от findAttributes, сделать два метода один занимается поиском
        // чисто FieldInfo, а другой поиском FieldInfo с атрибутом.
        // Upd: Уже сделал метод GetFieldsByAttribute, когда нибудь дойдет дело и до этого метода. 
        public static IReadOnlyList<FieldInfo> GetFields(Type objectType, Type targetType, bool findAttributes)
        {
            var output = new List<FieldInfo>();

            while (true)
            {
                var fields = objectType.GetFields(BindingFlags);
            
                foreach (var f in fields)
                {
                    if (findAttributes)
                    {
                        if (f.IsDefined(targetType, true))
                        {
                            output.Add(f);
                        }
                    }
                    else
                    {
                        if (f.FieldType == targetType)
                        {
                            output.Add(f);
                        }
                    }
                }

                objectType = objectType.BaseType;

                if (objectType == null)
                {
                    break;
                }
            }
            
            return output;
        }
        
        #endregion

        #region Members
        
        public static IReadOnlyList<MemberInfo> GetMembersByAttribute(this Type objectType, Type attributeType)
        {
            var output = new List<MemberInfo>();

            while (true)
            {
                var members = objectType.GetMembers(BindingFlags);

                foreach (var m in members)
                {
                    if (m.IsDefined(attributeType, true))
                    {
                        output.Add(m);
                    }
                }

                objectType = objectType.BaseType;

                if (objectType == null)
                {
                    break;
                }
            }
        
            return output;
        }
        
        public static bool TryGetMemberByName(this Type objectType, string name, out MemberInfo output)
        {
            output = null;
            
            while (true)
            {
                var members = objectType.GetMembers(BindingFlags);

                foreach (var m in members)
                {
                    if (m.Name == name)
                    {
                        output = m;
                        break;
                    }
                }

                objectType = objectType.BaseType;

                if (objectType == null)
                {
                    break;
                }
            }
        
            return output != null;
        }
        
        #endregion

        #region Method
        
        public static void InvokeMethod(object obj, string name, object[] parameters)
        {
            var objectType = obj.GetType();

            while (true)
            {
                var method = objectType.GetMethod(name, BindingFlags);

                if (method != null)
                {
                    method.Invoke(obj, parameters);
                    break;
                }

                objectType = objectType.BaseType;

                if (objectType == null)
                {
                    break;
                }
            }
        }
        
        #endregion
    }
}