using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SP.Runtime.Core.Items;
using SP.Runtime.Core.Utilities;
using SP.Runtime.Utilities;

namespace SP.Runtime.Core.Services.SaveService.Converters
{
    public class ItemConverter : JsonConverter
    {
        #region Structs
        
        [AttributeUsage(AttributeTargets.Field)]
        public class SavedAttribute : Attribute { }

        [Serializable]
        private class SavedElement
        {
            public SavedElement(string itemName, IReadOnlyList<BaseSavedMember> fields)
            {
                ItemName = itemName;
                Fields = fields;
            }

            public string ItemName { get; }
            public IReadOnlyList<BaseSavedMember> Fields { get; }
        }
        
        #endregion
        
        public override bool CanConvert(Type objectType)
        {
            return typeof(BaseItem).IsAssignableFrom(objectType);
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            SavedElement savedElement = null;
            
            var item = (BaseItem)value;

            if (item != null)
            {
                var fields = new List<BaseSavedMember>();

                var fieldsByAttribute = item.GetType().GetFieldsByAttribute(typeof(SavedAttribute));

                foreach (var f in fieldsByAttribute)
                {
                    fields.Add(new BaseSavedMember(f.Name, f.GetValue(item)));
                }

                savedElement = new SavedElement(item.name, fields);
            }

            serializer.Serialize(writer, savedElement);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            BaseItem output = null;

            var savedElement = serializer.Deserialize<SavedElement>(reader);

            if (savedElement != null)
            {
                if (ItemUtils.TryLoadItem(savedElement.ItemName, out var item))
                {
                    output = BaseItem.Instantiate(item);

                    foreach (var m in savedElement.Fields)
                    {
                        if (output.GetType().TryGetFieldByName(m.MemberName, out var fieldInfo))
                        {
                            if (m.TryReadValue(fieldInfo.FieldType, out var value))
                            {
                                fieldInfo.SetValue(output, value);
                            }
                        }
                    }
                }
            }

            return output;
        }
    }
}
