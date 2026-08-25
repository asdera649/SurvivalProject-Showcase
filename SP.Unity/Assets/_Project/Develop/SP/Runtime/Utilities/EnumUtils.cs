using System;
using System.Linq;

namespace SP.Runtime.Utilities
{
    public static class EnumUtils
    {
        public static int GetValueIndex<T>(T value) where T: Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Select((x, i) => new { item = x, index = i }).Single(x => value.Equals(x.item)).index;
        }

        public static T GetValueByIndex<T>(int index) where T: Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ElementAt(index);
        }
    }
}