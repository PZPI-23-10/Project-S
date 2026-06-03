using System.Collections.Generic;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public interface ICraftingRecipeProvider
    {
        CraftingContext Context { get; }
        IReadOnlyList<CraftingRecipeData> AvailableRecipes { get; }
        bool AllowsRecipe(CraftingRecipeData recipe);
    }
}
