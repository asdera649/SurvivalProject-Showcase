using System;
using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Craft
{
    [Serializable]
    public class RequiredItem
    {
        public RequiredItem(BaseItem item, int quantity)
        {
            _item = item;
            _quantity = Mathf.Clamp(quantity, 0, int.MaxValue);
        }

        [SerializeField] private BaseItem _item;
        public BaseItem Item => _item;

        [SerializeField] private int _quantity;
        public int Quantity => _quantity;
    }
}