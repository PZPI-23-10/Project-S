using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public enum CraftingContext
    {
        Hand,
        Workbench,
        Campfire,
        CharcoalPit,
        Cauldron,
        Furnace,
        Anvil
    }

    [Serializable]
    public class CraftingItemAmount
    {
        public ItemData Item;
        public int Amount = 1;

        public CraftingItemAmount() { }

        public CraftingItemAmount(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    [CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Project-S/Crafting/Recipe")]
    public class CraftingRecipeData : ScriptableObject
    {
        public string DisplayName;
        public CraftingContext Context;
        public CraftingItemAmount Output = new CraftingItemAmount();
        public List<CraftingItemAmount> Ingredients = new List<CraftingItemAmount>();
        public int SoulAshCost;
        public List<CraftingItemAmount> RequiredItems = new List<CraftingItemAmount>();
        [Min(0f)] public float CraftDurationSeconds;
        [Min(0f)] public float FuelSecondsCost;
        [TextArea(2, 5)] public string Description;

        public string RecipeName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(DisplayName))
                    return DisplayName;

                if (Output != null && Output.Item != null)
                    return Output.Item.ItemName;

                return name;
            }
        }
    }
}
