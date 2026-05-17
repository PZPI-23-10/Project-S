using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CraftingCheck
    {
        private readonly List<string> _problems = new List<string>();

        public IReadOnlyList<string> Problems => _problems;
        public bool CanCraft => _problems.Count == 0;
        public string Message => CanCraft ? "Ready" : string.Join("\n", _problems);

        public void AddProblem(string problem)
        {
            if (!string.IsNullOrWhiteSpace(problem))
                _problems.Add(problem);
        }
    }

    public class CraftingService
    {
        private readonly InventoryController _inventory;
        private readonly SoulAshWallet _wallet;

        public CraftingService(InventoryController inventory, SoulAshWallet wallet)
        {
            _inventory = inventory;
            _wallet = wallet;
        }

        public CraftingCheck Check(CraftingRecipeData recipe)
        {
            var check = new CraftingCheck();

            if (_inventory == null)
            {
                check.AddProblem("Inventory is missing.");
                return check;
            }

            if (_wallet == null)
            {
                check.AddProblem("Soul Ash wallet is missing.");
                return check;
            }

            if (recipe == null)
            {
                check.AddProblem("Recipe is missing.");
                return check;
            }

            if (recipe.Output == null || recipe.Output.Item == null || recipe.Output.Amount <= 0)
                check.AddProblem("Recipe output is not configured.");

            CheckAmounts(recipe.Ingredients, false, check);
            CheckAmounts(recipe.RequiredItems, true, check);

            if (recipe.SoulAshCost > 0 && !_wallet.CanSpend(recipe.SoulAshCost))
                check.AddProblem($"Need {recipe.SoulAshCost - _wallet.Amount} more Soul Ash.");

            var validIngredients = (recipe.Ingredients ?? Enumerable.Empty<CraftingItemAmount>())
                .Where(IsValidAmount)
                .ToList();
            bool hasAllIngredients = validIngredients.All(x => _inventory.GetItemCount(x.Item) >= x.Amount);

            if (hasAllIngredients && recipe.Output != null && recipe.Output.Item != null && recipe.Output.Amount > 0)
            {
                var removals = validIngredients
                    .Select(x => new ItemStack(x.Item, x.Amount))
                    .ToList();

                if (!_inventory.CanAddItemAfterRemoving(recipe.Output.Item, recipe.Output.Amount, removals))
                    check.AddProblem("Not enough inventory space for the crafted item.");
            }

            return check;
        }

        public bool TryCraft(CraftingRecipeData recipe, out CraftingCheck check)
        {
            if (!TryConsumeCosts(recipe, out check))
                return false;

            if (!_inventory.AddItem(recipe.Output.Item, recipe.Output.Amount))
            {
                check.AddProblem("Inventory changed before output could be added.");
                return false;
            }

            return true;
        }

        public bool TryConsumeCosts(CraftingRecipeData recipe, out CraftingCheck check)
        {
            check = Check(recipe);
            if (!check.CanCraft)
                return false;

            foreach (var ingredient in (recipe.Ingredients ?? Enumerable.Empty<CraftingItemAmount>()).Where(IsValidAmount))
            {
                if (!_inventory.TryRemoveItem(ingredient.Item, ingredient.Amount))
                {
                    check.AddProblem("Inventory changed before crafting completed.");
                    return false;
                }
            }

            if (!_wallet.Spend(recipe.SoulAshCost))
            {
                check.AddProblem("Soul Ash changed before crafting completed.");
                return false;
            }

            return true;
        }

        public static List<CraftingRecipeData> LoadRecipes(string resourcesPath = "Crafting/Recipes")
        {
            return Resources
                .LoadAll<CraftingRecipeData>(resourcesPath)
                .Where(x => x != null)
                .OrderBy(x => x.Context)
                .ThenBy(x => x.RecipeName)
                .ToList();
        }

        private void CheckAmounts(IEnumerable<CraftingItemAmount> amounts, bool nonConsumed, CraftingCheck check)
        {
            if (amounts == null)
                return;

            foreach (var amount in amounts)
            {
                if (!IsValidAmount(amount))
                {
                    check.AddProblem(nonConsumed ? "A requirement is not configured." : "An ingredient is not configured.");
                    continue;
                }

                int owned = _inventory.GetItemCount(amount.Item);
                if (owned < amount.Amount)
                {
                    string verb = nonConsumed ? "Requires" : "Need";
                    check.AddProblem($"{verb} {amount.Item.ItemName} x{amount.Amount - owned}.");
                }
            }
        }

        private static bool IsValidAmount(CraftingItemAmount amount)
        {
            return amount != null && amount.Item != null && amount.Amount > 0;
        }
    }
}
