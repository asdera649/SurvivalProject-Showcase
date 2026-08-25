using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Utilities
{
    public static class ItemUtils
    {
        public static bool TryLoadItem(string name, out BaseItem item)
        {
            item = Resources.Load<BaseItem>("Items/" + name);

            return item != null;
        }
    }
}