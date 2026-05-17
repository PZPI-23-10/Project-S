using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Editor.Tests
{
    public class CraftingMvpEditModeTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        [Test]
        public void Inventory_AddAndRemove_RespectsStacksAndCapacity()
        {
            var wood = CreateItem("Wood", true, 5);
            var inventory = CreateInventory(2);

            Assert.That(inventory.CanAddItem(wood, 10), Is.True);
            Assert.That(inventory.AddItem(wood, 10), Is.True);
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(10));

            Assert.That(inventory.CanAddItem(wood, 1), Is.False);
            Assert.That(inventory.TryRemoveItem(wood, 3), Is.True);
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(7));
        }

        [Test]
        public void Crafting_CanUseSpaceFreedByConsumedIngredients()
        {
            var wood = CreateItem("Wood", true, 20);
            var axe = CreateItem("Stone Axe", false, 1);
            var inventory = CreateInventory(1);
            var wallet = CreateWallet();
            var recipe = CreateRecipe(axe, 1, new[] { new CraftingItemAmount(wood, 2) });

            inventory.AddItem(wood, 2);
            var service = new CraftingService(inventory, wallet);

            Assert.That(service.TryCraft(recipe, out var check), Is.True, check.Message);
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(axe), Is.EqualTo(1));
        }

        [Test]
        public void Crafting_FailureDoesNotConsumeInventoryOrSoulAsh()
        {
            var leather = CreateItem("Leather", true, 10);
            var bandage = CreateItem("Soul Bandage", true, 5);
            var inventory = CreateInventory(2);
            var wallet = CreateWallet();
            var recipe = CreateRecipe(
                bandage,
                1,
                new[] { new CraftingItemAmount(leather, 1) },
                soulAshCost: 10);

            inventory.AddItem(leather, 1);
            wallet.Add(5);

            var service = new CraftingService(inventory, wallet);

            Assert.That(service.TryCraft(recipe, out _), Is.False);
            Assert.That(inventory.GetItemCount(leather), Is.EqualTo(1));
            Assert.That(inventory.GetItemCount(bandage), Is.EqualTo(0));
            Assert.That(wallet.Amount, Is.EqualTo(5));
        }

        [Test]
        public void Crafting_FailsWhenOutputDoesNotFit()
        {
            var tool = CreateItem("Required Tool", false, 1);
            var axe = CreateItem("Stone Axe", false, 1);
            var inventory = CreateInventory(1);
            var wallet = CreateWallet();
            var recipe = CreateRecipe(
                axe,
                1,
                new CraftingItemAmount[0],
                requiredItems: new[] { new CraftingItemAmount(tool, 1) });

            inventory.AddItem(tool, 1);
            var service = new CraftingService(inventory, wallet);

            Assert.That(service.TryCraft(recipe, out _), Is.False);
            Assert.That(inventory.GetItemCount(tool), Is.EqualTo(1));
            Assert.That(inventory.GetItemCount(axe), Is.EqualTo(0));
        }

        [Test]
        public void SoulAshWallet_SpendClampsAndRaisesChanged()
        {
            var wallet = CreateWallet();
            int lastEventAmount = -1;
            wallet.Changed += amount => lastEventAmount = amount;

            wallet.Add(5);
            Assert.That(wallet.Amount, Is.EqualTo(5));
            Assert.That(lastEventAmount, Is.EqualTo(5));

            Assert.That(wallet.Spend(3), Is.True);
            Assert.That(wallet.Amount, Is.EqualTo(2));
            Assert.That(lastEventAmount, Is.EqualTo(2));

            Assert.That(wallet.Spend(3), Is.False);
            Assert.That(wallet.Amount, Is.EqualTo(2));
        }

        private ItemData CreateItem(string itemName, bool stackable, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.ItemName = itemName;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            _objects.Add(item);
            return item;
        }

        private InventoryController CreateInventory(int size)
        {
            var go = new GameObject("Inventory");
            _objects.Add(go);

            var inventory = go.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", size);
            SetPrivateField(inventory, "_slots", new ItemStack[size]);
            return inventory;
        }

        private SoulAshWallet CreateWallet()
        {
            var go = new GameObject("SoulAshWallet");
            _objects.Add(go);
            return go.AddComponent<SoulAshWallet>();
        }

        private CraftingRecipeData CreateRecipe(
            ItemData output,
            int outputAmount,
            IEnumerable<CraftingItemAmount> ingredients,
            int soulAshCost = 0,
            IEnumerable<CraftingItemAmount> requiredItems = null)
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipeData>();
            recipe.Output = new CraftingItemAmount(output, outputAmount);
            recipe.Ingredients = new List<CraftingItemAmount>(ingredients);
            recipe.RequiredItems = requiredItems == null
                ? new List<CraftingItemAmount>()
                : new List<CraftingItemAmount>(requiredItems);
            recipe.SoulAshCost = soulAshCost;
            _objects.Add(recipe);
            return recipe;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            field.SetValue(target, value);
        }
    }
}
