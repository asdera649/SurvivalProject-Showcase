using System.Collections.Generic;
using SP.Runtime.Core.Items;
using UnityEngine;

namespace SP.Runtime.Core.Systems.Craft
{
    [CreateAssetMenu(menuName = "Craft/CraftCollection")]
    public class CraftCollection : ScriptableObject
    {
        [SerializeField] private List<CraftingRecipe> _craftingRecipes = new();
        public IReadOnlyList<CraftingRecipe> CraftingRecipes => _craftingRecipes;

        public bool TryGetCraftRecipeIndex(CraftingRecipe craftingRecipe, out int index)
        {
            index = -1;

            for (var i = 0; i < _craftingRecipes.Count; i++)
            {
                if (_craftingRecipes[i] == craftingRecipe)
                {
                    index = i;
                    break;
                }
            }

            return index != -1;
        }

        public bool TryGetCraftRecipe(BaseItem item, out CraftingRecipe output)
        {
            output = null;

            foreach (var r in _craftingRecipes)
            {
                if (r.ReceivedItem.Equals(item))
                {
                    output = r;
                    break;
                }
            }

            return output != null;
        }
    }
}
