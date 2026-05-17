using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Harvesting;
using UnityEngine;

namespace Project_S.Editor.Tests
{
    public class ForgeTierEditModeTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var pickup in Object.FindObjectsOfType<ItemPickup>())
                Object.DestroyImmediate(pickup.gameObject);

            foreach (var obj in _objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        [Test]
        public void FurnaceRecipe_ConsumesOreAndCharcoalOnceAndFinishesOnce()
        {
            var ore = CreateItem("Iron Ore", true, 20);
            var charcoal = CreateItem("Charcoal", true, 40);
            var chunk = CreateItem("Krytsia Chunk", true, 20);
            CreatePlayer(4, out var inventory, out var wallet, out _);
            var furnace = CreateStation(CraftingContext.Furnace, "Furnace", "Smelt");
            var recipe = CreateRecipe(CraftingContext.Furnace, chunk, 1, new[]
            {
                new CraftingItemAmount(ore, 1),
                new CraftingItemAmount(charcoal, 1)
            }, 0, 5f);

            inventory.AddItem(ore, 1);
            inventory.AddItem(charcoal, 1);

            Assert.That(furnace.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            Assert.That(inventory.GetItemCount(ore), Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(charcoal), Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(chunk), Is.EqualTo(0));

            furnace.Tick(4.9f);
            Assert.That(inventory.GetItemCount(chunk), Is.EqualTo(0));

            furnace.Tick(0.1f);
            Assert.That(inventory.GetItemCount(chunk), Is.EqualTo(1));

            furnace.Tick(10f);
            Assert.That(inventory.GetItemCount(chunk), Is.EqualTo(1));
        }

        [Test]
        public void FurnaceRecipe_SpawnsPickupIfInventoryFillsBeforeFinish()
        {
            var ore = CreateItem("Iron Ore", true, 20);
            var charcoal = CreateItem("Charcoal", true, 40);
            var chunk = CreateItem("Krytsia Chunk", true, 20);
            var blocker = CreateItem("Blocker", false, 1);
            CreatePlayer(1, out var inventory, out var wallet, out _);
            var furnace = CreateStation(CraftingContext.Furnace, "Furnace", "Smelt");
            var recipe = CreateRecipe(CraftingContext.Furnace, chunk, 1, new[]
            {
                new CraftingItemAmount(ore, 1),
                new CraftingItemAmount(charcoal, 1)
            }, 0, 1f);

            inventory.AddItem(ore, 1);
            inventory.AddItem(charcoal, 1);
            Assert.That(furnace.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            inventory.AddItem(blocker, 1);

            furnace.Tick(1f);

            var pickups = Object.FindObjectsOfType<ItemPickup>();
            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.EqualTo(chunk));
            Assert.That(pickups[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void AnvilRecipe_FailsWithoutCostsAndLeavesInventoryAndSoulAshUnchanged()
        {
            var chunk = CreateItem("Krytsia Chunk", true, 20);
            var ingot = CreateItem("Iron Ingot", true, 20);
            CreatePlayer(3, out var inventory, out var wallet, out _);
            var anvil = CreateStation(CraftingContext.Anvil, "Anvil", "Forge");
            var recipe = CreateRecipe(CraftingContext.Anvil, ingot, 1, new[] { new CraftingItemAmount(chunk, 2) }, 65, 0f);

            inventory.AddItem(chunk, 1);
            wallet.Add(64);

            Assert.That(anvil.TryStartRecipe(recipe, inventory, wallet, out var check), Is.False);
            Assert.That(check.CanCraft, Is.False);
            Assert.That(inventory.GetItemCount(chunk), Is.EqualTo(1));
            Assert.That(wallet.Amount, Is.EqualTo(64));
            Assert.That(inventory.GetItemCount(ingot), Is.EqualTo(0));
        }

        [Test]
        public void AnvilRecipe_InstantlyCreatesIronIngot()
        {
            var chunk = CreateItem("Krytsia Chunk", true, 20);
            var ingot = CreateItem("Iron Ingot", true, 20);
            CreatePlayer(3, out var inventory, out var wallet, out _);
            var anvil = CreateStation(CraftingContext.Anvil, "Anvil", "Forge");
            var recipe = CreateRecipe(CraftingContext.Anvil, ingot, 1, new[] { new CraftingItemAmount(chunk, 2) }, 65, 0f);

            inventory.AddItem(chunk, 2);
            wallet.Add(65);

            Assert.That(anvil.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            Assert.That(anvil.IsCooking, Is.False);
            Assert.That(inventory.GetItemCount(chunk), Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(ingot), Is.EqualTo(1));
            Assert.That(wallet.Amount, Is.EqualTo(0));
        }

        [Test]
        public void IronWeaponAssets_HaveExpectedMvpStats()
        {
            AssertWeapon(
                "Crafting/Items/Weapons/KrytsiaAxe",
                DamageType.Slashing,
                57f,
                35f,
                2,
                0.5f,
                0.16f,
                HarvestToolType.Axe);

            AssertWeapon(
                "Crafting/Items/Weapons/KrytsiaHammer",
                DamageType.Blunt,
                65f,
                45f,
                1,
                0.75f,
                0.14f,
                HarvestToolType.None);

            AssertWeapon(
                "Crafting/Items/Weapons/Misericorde",
                DamageType.Piercing,
                14f,
                2f,
                4,
                0f,
                0.36f,
                HarvestToolType.None);

            AssertWeapon(
                "Crafting/Items/Weapons/IronSword",
                DamageType.Slashing,
                26f,
                15f,
                3,
                0.5f,
                0.3f,
                HarvestToolType.None);
        }

        [Test]
        public void RecipeCatalog_LoadsFurnaceAndAnvilRecipes()
        {
            var recipes = CraftingService.LoadRecipes();

            Assert.That(recipes.Any(x => x.Context == CraftingContext.Furnace && x.RecipeName == "Krytsia Chunk"), Is.True);
            Assert.That(recipes.Any(x => x.Context == CraftingContext.Anvil && x.RecipeName == "Iron Ingot"), Is.True);
            Assert.That(recipes.Any(x => x.Context == CraftingContext.Anvil && x.RecipeName == "Krytsia Axe"), Is.True);
            Assert.That(recipes.Any(x => x.Context == CraftingContext.Anvil && x.RecipeName == "Krytsia Hammer"), Is.True);
            Assert.That(recipes.Any(x => x.Context == CraftingContext.Anvil && x.RecipeName == "Misericorde"), Is.True);
            Assert.That(recipes.Any(x => x.Context == CraftingContext.Anvil && x.RecipeName == "Iron Sword"), Is.True);
        }

        private void AssertWeapon(
            string resourcesPath,
            DamageType damageType,
            float damage,
            float poise,
            int combo,
            float block,
            float parry,
            HarvestToolType tool)
        {
            var weapon = Resources.Load<WeaponItemData>(resourcesPath);
            Assert.That(weapon, Is.Not.Null, resourcesPath);
            Assert.That(weapon.DamageProfile, Has.Count.EqualTo(1));
            Assert.That(weapon.DamageProfile[0].Type, Is.EqualTo(damageType));
            Assert.That(weapon.DamageProfile[0].Amount, Is.EqualTo(damage).Within(0.001f));
            Assert.That(weapon.PoiseDamage, Is.EqualTo(poise).Within(0.001f));
            Assert.That(weapon.MaxComboHits, Is.EqualTo(combo));
            Assert.That(weapon.BlockMitigation, Is.EqualTo(block).Within(0.001f));
            Assert.That(weapon.ParryWindow, Is.EqualTo(parry).Within(0.001f));
            Assert.That(weapon.HarvestTool, Is.EqualTo(tool));
        }

        private ItemData CreateItem(string itemName, bool stackable, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.ItemName = itemName;
            item.Kind = ItemKind.Resource;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            _objects.Add(item);
            return item;
        }

        private TimedCraftingStation CreateStation(CraftingContext context, string displayName, string actionLabel)
        {
            var go = new GameObject(displayName);
            _objects.Add(go);
            var station = go.AddComponent<TimedCraftingStation>();
            station.ConfigureStation(context, displayName, actionLabel, false, null, 0f, 0f);
            return station;
        }

        private CraftingRecipeData CreateRecipe(
            CraftingContext context,
            ItemData output,
            int outputAmount,
            IEnumerable<CraftingItemAmount> ingredients,
            int soulAshCost,
            float duration)
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipeData>();
            recipe.Context = context;
            recipe.Output = new CraftingItemAmount(output, outputAmount);
            recipe.Ingredients = new List<CraftingItemAmount>(ingredients);
            recipe.RequiredItems = new List<CraftingItemAmount>();
            recipe.SoulAshCost = soulAshCost;
            recipe.CraftDurationSeconds = duration;
            recipe.FuelSecondsCost = 0f;
            _objects.Add(recipe);
            return recipe;
        }

        private GameObject CreatePlayer(
            int inventorySize,
            out InventoryController inventory,
            out SoulAshWallet wallet,
            out CharacterStats stats)
        {
            var player = new GameObject("Player");
            _objects.Add(player);

            stats = player.AddComponent<CharacterStats>();
            ConfigureStats(stats);

            wallet = player.AddComponent<SoulAshWallet>();
            inventory = player.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", inventorySize);
            SetPrivateField(inventory, "_slots", new ItemStack[inventorySize]);
            SetPrivateField(inventory, "_stats", stats);
            return player;
        }

        private static void ConfigureStats(CharacterStats stats)
        {
            var configuredStats = new List<Stat>
            {
                CreateStat(StatType.CarryWeight, 130f, 0f, 200f)
            };

            SetPrivateField(stats, "_stats", configuredStats);
            InvokePrivate(stats, "Awake");
        }

        private static Stat CreateStat(StatType type, float baseValue, float min, float max)
        {
            var stat = new Stat();
            SetPrivateField(stat, "_type", type);
            SetPrivateField(stat, "_baseValue", baseValue);
            SetPrivateField(stat, "_minValue", min);
            SetPrivateField(stat, "_maxValue", max);
            return stat;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName, params object[] args)
        {
            var type = target.GetType();
            MethodInfo method = null;
            while (type != null && method == null)
            {
                method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(method, Is.Not.Null, $"Method {methodName} was not found.");
            method.Invoke(target, args);
        }
    }
}
