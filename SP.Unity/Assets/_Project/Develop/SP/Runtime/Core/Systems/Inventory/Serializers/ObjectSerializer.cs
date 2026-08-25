using Mirror;
using SP.Runtime.Core.Utilities;

namespace SP.Runtime.Core.Systems.Inventory.Serializers
{
    public static class ObjectSerializer
    {
        public static void WriteObjectVar(this NetworkWriter writer, object value)
        {
            var bytes = BinaryUtils.WriteToBytes(value);

            writer.WriteUShort(checked((ushort)(bytes.Length + 1)));
            writer.WriteBytes(bytes, 0, bytes.Length);
        }

        public static object ReadObjectVar(this NetworkReader reader)
        {
            object output = null;
            
            var size = reader.ReadUShort();
            
            if (size > 0)
            {
                var realSize = size - 1;
                var bytes = reader.ReadBytesSegment(realSize);

                output = BinaryUtils.ReadBytes(bytes.Array, bytes.Offset, bytes.Count);
            }
            
            return output;
        }
    }
}