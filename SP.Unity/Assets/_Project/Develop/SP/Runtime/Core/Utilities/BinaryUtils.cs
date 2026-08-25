using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace SP.Runtime.Core.Utilities
{
    public static class BinaryUtils
    {
        public static byte[] WriteToBytes(object value)
        {
            var binFormatter = new BinaryFormatter();
            var mSteam = new MemoryStream();

            binFormatter.Serialize(mSteam, value);

            return mSteam.ToArray();
        }
    
        public static object ReadBytes(byte[] bytes, int offset, int count)
        {
            var mStream = new MemoryStream();
            var binFormatter = new BinaryFormatter();

            mStream.Write(bytes, offset, count);
            mStream.Position = 0;

            return binFormatter.Deserialize(mStream);
        }
    }
}