using System;
using System.Collections.Generic;
using Mirror;
using SP.Runtime.Core.Systems.Inventory;
using SP.Runtime.Core.Utilities;
using SP.Runtime.Utilities;

namespace SP.Runtime.Core.Items.Serializers
{
    public static class ItemSerializer
    {
        #region Structs
        
        [Serializable]
        private struct Item
        {
            public Item(string name, IReadOnlyList<object> fields)
            {
                Name = name;
                Fields = fields;
            }

            public readonly string Name;
            public readonly IReadOnlyList<object> Fields;
        }
        
        #endregion

        public static void WriteItem(this NetworkWriter writer, BaseItem value)
        {
            var itemData = new Item();

            if (value != null)
            {
                itemData = GetItemData(value);
            }
            
            var bytes = BinaryUtils.WriteToBytes(itemData);

            writer.WriteUShort(checked((ushort)(bytes.Length + 1)));
            writer.WriteBytes(bytes, 0, bytes.Length);
        }

        public static BaseItem ReadItem(this NetworkReader reader)
        {
            BaseItem output = null;
            
            var size = reader.ReadUShort();

            if (size > 0)
            {
                var realSize = size - 1;
                var bytes = reader.ReadBytesSegment(realSize);

                var itemData = (Item)BinaryUtils.ReadBytes(bytes.Array, bytes.Offset, bytes.Count);
                
                if (TryInstantiateItem(itemData, out var item))
                {
                    output = item;
                }
            }

            return output;
        }

        #region Utilities
        
        private static Item GetItemData(BaseItem item)
        {
            var temp = ReflectionUtils.GetFields(item.GetType(), typeof(CustomSyncVarAttribute), true);

            var fields = new List<object>();

            foreach (var f in temp)
            {
                fields.Add(f.GetValue(item));
            }

            return new Item(item.name, fields);
        }

        private static bool TryInstantiateItem(Item data, out BaseItem output)
        {
            output = null;
            
            if (ItemUtils.TryLoadItem(data.Name, out var item))
            {
                output = BaseItem.Instantiate(item);
                
                var fields = ReflectionUtils.GetFields(output.GetType(), typeof(CustomSyncVarAttribute), true);

                for (var i = 0; i < fields.Count; i++)
                {
                    fields[i].SetValue(output, data.Fields[i]);
                }
            }

            return output != null;
        }
        
        #endregion
    }
}