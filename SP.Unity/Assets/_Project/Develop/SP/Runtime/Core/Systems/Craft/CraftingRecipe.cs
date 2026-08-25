using System;
using System.Collections.Generic;
using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Craft
{
    [Serializable]
    public class CraftingRecipe
    {
        [SerializeField] private BaseItem _receivedItem;
        public BaseItem ReceivedItem => _receivedItem;

        [SerializeField] private float _craftingTime;
        public float CraftingTime => _craftingTime;

        [SerializeField] private int _requiredWorkbenchLevel;
        public int RequiredWorkbenchLevel => _requiredWorkbenchLevel;

        [SerializeField] private bool _craftWholesale;
        public bool CraftWholesale => _craftWholesale;

        [SerializeField] private RequiredItem[] _itemsForCrafting;
        public IReadOnlyList<RequiredItem> ItemsForCrafting => _itemsForCrafting;
    }
}