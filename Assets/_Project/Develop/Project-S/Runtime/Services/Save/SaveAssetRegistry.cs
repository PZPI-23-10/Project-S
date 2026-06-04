using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Runtime.Services.Save
{
    public class SaveAssetRegistry
    {
        private readonly Dictionary<string, ItemData> _itemsById = new Dictionary<string, ItemData>();
        private readonly Dictionary<ItemData, string> _idsByItem = new Dictionary<ItemData, string>();
        private readonly Dictionary<string, CraftingRecipeData> _recipesById = new Dictionary<string, CraftingRecipeData>();
        private readonly Dictionary<CraftingRecipeData, string> _idsByRecipe = new Dictionary<CraftingRecipeData, string>();

        private bool _loaded;

        public void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;
            LoadItems();
            LoadRecipes();
        }

        public string GetItemId(ItemData item)
        {
            EnsureLoaded();
            if (item == null)
                return null;

            if (_idsByItem.TryGetValue(item, out string id))
                return id;

            return ResolveItemId(item);
        }

        public ItemData GetItem(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && _itemsById.TryGetValue(id, out var item)
                ? item
                : null;
        }

        public string GetRecipeId(CraftingRecipeData recipe)
        {
            EnsureLoaded();
            if (recipe == null)
                return null;

            if (_idsByRecipe.TryGetValue(recipe, out string id))
                return id;

            return ResolveRecipeId(recipe);
        }

        public CraftingRecipeData GetRecipe(string id)
        {
            EnsureLoaded();
            return !string.IsNullOrWhiteSpace(id) && _recipesById.TryGetValue(id, out var recipe)
                ? recipe
                : null;
        }

        private void LoadItems()
        {
            foreach (var item in Resources.LoadAll<ItemData>("Crafting/Items"))
            {
                if (item == null)
                    continue;

                string id = ResolveItemId(item);
                AddUnique(_itemsById, _idsByItem, id, item, "item");
            }
        }

        private void LoadRecipes()
        {
            foreach (var recipe in Resources.LoadAll<CraftingRecipeData>("Crafting/Recipes"))
            {
                if (recipe == null)
                    continue;

                string id = ResolveRecipeId(recipe);
                AddUnique(_recipesById, _idsByRecipe, id, recipe, "recipe");
            }
        }

        private static string ResolveItemId(ItemData item)
        {
            return !string.IsNullOrWhiteSpace(item.SaveId) ? item.SaveId : item.name;
        }

        private static string ResolveRecipeId(CraftingRecipeData recipe)
        {
            return !string.IsNullOrWhiteSpace(recipe.SaveId) ? recipe.SaveId : recipe.name;
        }

        private static void AddUnique<T>(
            Dictionary<string, T> byId,
            Dictionary<T, string> byAsset,
            string id,
            T asset,
            string assetKind) where T : Object
        {
            if (string.IsNullOrWhiteSpace(id) || asset == null)
                return;

            if (byId.TryGetValue(id, out var existing) && existing != asset)
            {
                Debug.LogWarning($"[Save] Duplicate {assetKind} save id '{id}' on {existing.name} and {asset.name}. First asset will be used.");
                return;
            }

            byId[id] = asset;
            byAsset[asset] = id;
        }
    }
}
