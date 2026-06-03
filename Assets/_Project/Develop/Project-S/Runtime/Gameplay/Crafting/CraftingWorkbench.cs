using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CraftingWorkbench : MonoBehaviour, IInteractable, ICraftingRecipeProvider
    {
        [SerializeField] private List<CraftingRecipeData> _availableRecipes = new List<CraftingRecipeData>();

        public string InteractionPrompt => "Верстак";
        public CraftingContext Context => CraftingContext.Workbench;
        public IReadOnlyList<CraftingRecipeData> AvailableRecipes => _availableRecipes;

        public bool AllowsRecipe(CraftingRecipeData recipe)
        {
            return recipe != null
                && recipe.Context == Context
                && _availableRecipes != null
                && _availableRecipes.Contains(recipe);
        }

        public void ConfigureRecipes(IEnumerable<CraftingRecipeData> recipes)
        {
            _availableRecipes = recipes?
                .Where(x => x != null)
                .ToList() ?? new List<CraftingRecipeData>();
        }

        public void Interact(PlayerInteractor interactor)
        {
            var inventoryUI = FindFirstObjectByType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogWarning("[Crafting] Inventory UI is missing.");
                return;
            }

            if (interactor != null)
            {
                inventoryUI.OpenWithCraftingContext(
                    CraftingContext.Workbench,
                    transform,
                    interactor.transform,
                    interactor.MenuCloseDistance,
                    this);
            }
            else
            {
                inventoryUI.OpenWithCraftingContext(CraftingContext.Workbench, this);
            }
        }
    }
}
