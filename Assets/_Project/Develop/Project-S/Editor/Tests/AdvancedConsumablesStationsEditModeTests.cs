using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Editor.Tests
{
    public class AdvancedConsumablesStationsEditModeTests
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
        public void CharcoalPitRecipe_DoesNotNeedFuelAndConsumesCostsOnce()
        {
            var wood = CreateItem("Wood", true, 20, ItemKind.Resource);
            var charcoal = CreateItem("Charcoal", true, 40, ItemKind.Resource);
            CreatePlayer(4, out var inventory, out var wallet, out _, out _);
            var station = CreateStation(CraftingContext.CharcoalPit, "Charcoal Pit", "Burn", false, null, 0f, 0f);
            var recipe = CreateRecipe(CraftingContext.CharcoalPit, charcoal, 2, new[] { new CraftingItemAmount(wood, 1) }, 20, 5f, 0f);

            inventory.AddItem(wood, 1);
            wallet.Add(20);

            Assert.That(station.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(0));
            Assert.That(wallet.Amount, Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(charcoal), Is.EqualTo(0));

            station.Tick(4f);
            Assert.That(inventory.GetItemCount(charcoal), Is.EqualTo(0));

            station.Tick(1f);
            Assert.That(inventory.GetItemCount(charcoal), Is.EqualTo(2));

            station.Tick(10f);
            Assert.That(inventory.GetItemCount(charcoal), Is.EqualTo(2));
        }

        [Test]
        public void CauldronFuel_CapsAtMaxAndBlocksRecipesWithoutFuel()
        {
            var wood = CreateItem("Wood", true, 20, ItemKind.Resource);
            var potion = CreateItem("Potion", true, 5, ItemKind.Consumable);
            CreatePlayer(8, out var inventory, out var wallet, out _, out _);
            var emptyCauldron = CreateStation(CraftingContext.Cauldron, "Cauldron", "Brew", true, wood, 300f, 1200f);
            var fueledCauldron = CreateStation(CraftingContext.Cauldron, "Cauldron", "Brew", true, wood, 300f, 1200f);
            var recipe = CreateRecipe(CraftingContext.Cauldron, potion, 1, new CraftingItemAmount[0], 0, 5f, 10f);

            Assert.That(emptyCauldron.TryStartRecipe(recipe, inventory, wallet, out _), Is.False);

            inventory.AddItem(wood, 5);
            Assert.That(fueledCauldron.TryAddFuel(inventory), Is.True);
            Assert.That(fueledCauldron.TryAddFuel(inventory), Is.True);
            Assert.That(fueledCauldron.TryAddFuel(inventory), Is.True);
            Assert.That(fueledCauldron.TryAddFuel(inventory), Is.True);
            Assert.That(fueledCauldron.TryAddFuel(inventory), Is.False);
            Assert.That(fueledCauldron.FuelSeconds, Is.EqualTo(1200f).Within(0.001f));
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(1));

            Assert.That(fueledCauldron.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            Assert.That(fueledCauldron.FuelSeconds, Is.EqualTo(1190f).Within(0.001f));
        }

        [Test]
        public void GenericTimedStation_SpawnsPickupWhenInventoryFillsBeforeFinish()
        {
            var raw = CreateItem("Raw", true, 10, ItemKind.Resource);
            var output = CreateItem("Output", true, 10, ItemKind.Resource);
            var blocker = CreateItem("Blocker", false, 1, ItemKind.Resource);
            CreatePlayer(1, out var inventory, out var wallet, out _, out _);
            var station = CreateStation(CraftingContext.CharcoalPit, "Charcoal Pit", "Burn", false, null, 0f, 0f);
            var recipe = CreateRecipe(CraftingContext.CharcoalPit, output, 1, new[] { new CraftingItemAmount(raw, 1) }, 0, 1f, 0f);

            inventory.AddItem(raw, 1);
            Assert.That(station.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            inventory.AddItem(blocker, 1);

            station.Tick(1f);

            var pickups = Object.FindObjectsOfType<ItemPickup>();
            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.EqualTo(output));
            Assert.That(pickups[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void MaxInventoryStacks_BlocksSecondLightningGreaseStack()
        {
            var grease = CreateItem("Lightning Grease", true, 10, ItemKind.Consumable);
            grease.MaxInventoryStacks = 1;
            CreatePlayer(3, out var inventory, out _, out _, out _);

            Assert.That(inventory.AddItem(grease, 10), Is.True);
            Assert.That(inventory.CanAddItem(grease, 1), Is.False);
            Assert.That(inventory.AddItem(grease, 1), Is.False);
            Assert.That(inventory.GetItemCount(grease), Is.EqualTo(10));
        }

        [Test]
        public void AdvancedConsumables_RestoreStatsApplyBuffsAndConsumeStack()
        {
            var healing = CreateItem("Healing Poultice", true, 5, ItemKind.Consumable);
            healing.HealthRestoreAmount = 60f;
            var food = CreateItem("Meat With Berries", true, 5, ItemKind.Consumable);
            food.HungerRestoreAmount = 60f;
            food.TimedBuffType = TimedBuffType.AttackDamage;
            food.TimedBuffCategory = TimedBuffCategory.Food;
            food.TimedBuffMultiplier = 1.1f;
            food.TimedBuffDurationSeconds = 60f;
            var mavka = CreateItem("Mavka Potion", true, 5, ItemKind.Consumable);
            mavka.StaminaRestoreAmount = 10f;
            mavka.TimedBuffType = TimedBuffType.StaminaCost;
            mavka.TimedBuffCategory = TimedBuffCategory.Potion;
            mavka.TimedBuffMultiplier = 0f;
            mavka.TimedBuffDurationSeconds = 10f;

            CreatePlayer(4, out var inventory, out _, out var stats, out var buffs);
            stats.Set(StatType.Health, 10f);
            stats.Set(StatType.Hunger, 80f);
            stats.Set(StatType.Stamina, 20f);
            inventory.AddItem(healing, 1);
            inventory.AddItem(food, 1);
            inventory.AddItem(mavka, 1);

            Assert.That(inventory.TryUseItemAtSlot(0), Is.True);
            Assert.That(stats.Get(StatType.Health), Is.EqualTo(70f).Within(0.001f));
            Assert.That(inventory.GetItemCount(healing), Is.EqualTo(0));

            Assert.That(inventory.TryUseItemAtSlot(1), Is.True);
            Assert.That(stats.Get(StatType.Hunger), Is.EqualTo(20f).Within(0.001f));
            Assert.That(buffs.AttackDamageMultiplier, Is.EqualTo(1.1f).Within(0.001f));

            Assert.That(inventory.TryUseItemAtSlot(2), Is.True);
            Assert.That(stats.Get(StatType.Stamina), Is.EqualTo(30f).Within(0.001f));
            Assert.That(buffs.StaminaCostMultiplier, Is.EqualTo(0f).Within(0.001f));
            Assert.That(inventory.GetItemCount(mavka), Is.EqualTo(0));
        }

        [Test]
        public void BuffCategoryLimits_UseExpectedReplacementRules()
        {
            var foodBuffs = CreateBuffs("Food Buffs");
            foodBuffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Food, 1.1f, 60f);
            foodBuffs.ApplyBuff(TimedBuffType.SoulAshReward, TimedBuffCategory.Food, 1.15f, 60f);
            Assert.That(foodBuffs.GetActiveCount(TimedBuffCategory.Food), Is.EqualTo(1));
            Assert.That(foodBuffs.AttackDamageMultiplier, Is.EqualTo(1f).Within(0.001f));
            Assert.That(foodBuffs.SoulAshRewardMultiplier, Is.EqualTo(1.15f).Within(0.001f));

            var potionBuffs = CreateBuffs("Potion Buffs");
            potionBuffs.ApplyBuff(TimedBuffType.StaminaCost, TimedBuffCategory.Potion, 0f, 60f);
            potionBuffs.ApplyBuff(TimedBuffType.AttackSpeed, TimedBuffCategory.Potion, 1.12f, 60f);
            potionBuffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Potion, 1.2f, 60f);
            Assert.That(potionBuffs.GetActiveCount(TimedBuffCategory.Potion), Is.EqualTo(2));
            Assert.That(potionBuffs.StaminaCostMultiplier, Is.EqualTo(1f).Within(0.001f));
            Assert.That(potionBuffs.AttackSpeedMultiplier, Is.EqualTo(1.12f).Within(0.001f));
            Assert.That(potionBuffs.AttackDamageMultiplier, Is.EqualTo(1.2f).Within(0.001f));

            var weaponBuffs = CreateBuffs("Weapon Buffs");
            weaponBuffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Weapon, 1.35f, 60f);
            weaponBuffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Weapon, 1.2f, 60f);
            Assert.That(weaponBuffs.GetActiveCount(TimedBuffCategory.Weapon), Is.EqualTo(1));
            Assert.That(weaponBuffs.AttackDamageMultiplier, Is.EqualTo(1.2f).Within(0.001f));

            var debuffs = CreateBuffs("Debuffs");
            debuffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Debuff, 0.9f, 60f);
            debuffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Debuff, 0.8f, 60f);
            Assert.That(debuffs.GetActiveCount(TimedBuffCategory.Debuff), Is.EqualTo(2));
            Assert.That(debuffs.AttackDamageMultiplier, Is.EqualTo(0.72f).Within(0.001f));
        }

        [Test]
        public void StaminaCostBuff_AffectsSpend()
        {
            var player = CreatePlayer(2, out _, out _, out var stats, out var buffs);
            var stamina = player.AddComponent<StaminaController>();
            SetPrivateField(stamina, "_stats", stats);
            SetPrivateField(stamina, "_buffs", buffs);
            InvokePrivate(stamina, "Awake");

            buffs.ApplyBuff(TimedBuffType.StaminaCost, TimedBuffCategory.Potion, 0f, 10f);
            Assert.That(stamina.Spend(25f), Is.True);
            Assert.That(stats.Get(StatType.Stamina), Is.EqualTo(50f).Within(0.001f));

            buffs.Tick(11f);
            Assert.That(stamina.Spend(10f), Is.True);
            Assert.That(stats.Get(StatType.Stamina), Is.EqualTo(40f).Within(0.001f));
        }

        [Test]
        public void AttackSpeedBuff_AffectsCombatControllerMultiplier()
        {
            var player = CreatePlayer(2, out _, out _, out _, out var buffs);
            var combat = player.AddComponent<CombatController>();
            var weapon = ScriptableObject.CreateInstance<WeaponItemData>();
            weapon.AttackSpeedMultiplier = 1f;
            _objects.Add(weapon);
            SetPrivateField(combat, "_currentWeapon", weapon);

            buffs.ApplyBuff(TimedBuffType.AttackSpeed, TimedBuffCategory.Potion, 1.12f, 60f);

            Assert.That(combat.GetAttackSpeedMultiplier(), Is.EqualTo(1.12f).Within(0.001f));
        }

        [Test]
        public void HomePotion_TeleportsToRecordedHomeAfterDelay()
        {
            var home = new Vector3(2f, 0f, 2f);
            var player = CreatePlayer(2, out var inventory, out _, out _, out _);
            var teleport = player.AddComponent<HomeTeleportController>();
            teleport.SetHomePosition(home);
            player.transform.position = new Vector3(10f, 0f, 0f);

            var homePotion = CreateItem("Home Potion", true, 5, ItemKind.Consumable);
            homePotion.SpecialEffect = ConsumableSpecialEffectType.HomeTeleport;
            homePotion.SpecialEffectDelaySeconds = 5f;
            inventory.AddItem(homePotion, 1);

            Assert.That(inventory.TryUseItemAtSlot(0), Is.True);
            Assert.That(teleport.IsTeleporting, Is.True);
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(10f, 0f, 0f)));

            teleport.Tick(4.9f);
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(10f, 0f, 0f)));

            teleport.Tick(0.1f);
            Assert.That(player.transform.position, Is.EqualTo(home));
            Assert.That(inventory.GetItemCount(homePotion), Is.EqualTo(0));
        }

        private ItemData CreateItem(string itemName, bool stackable, int maxStack, ItemKind kind)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.ItemName = itemName;
            item.Kind = kind;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            _objects.Add(item);
            return item;
        }

        private TimedCraftingStation CreateStation(
            CraftingContext context,
            string displayName,
            string actionLabel,
            bool usesFuel,
            ItemData fuelItem,
            float secondsPerFuelItem,
            float maxFuelSeconds)
        {
            var go = new GameObject(displayName);
            _objects.Add(go);
            var station = go.AddComponent<TimedCraftingStation>();
            station.ConfigureStation(context, displayName, actionLabel, usesFuel, fuelItem, secondsPerFuelItem, maxFuelSeconds);
            return station;
        }

        private CraftingRecipeData CreateRecipe(
            CraftingContext context,
            ItemData output,
            int outputAmount,
            IEnumerable<CraftingItemAmount> ingredients,
            int soulAshCost,
            float duration,
            float fuel)
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipeData>();
            recipe.Context = context;
            recipe.Output = new CraftingItemAmount(output, outputAmount);
            recipe.Ingredients = new List<CraftingItemAmount>(ingredients);
            recipe.RequiredItems = new List<CraftingItemAmount>();
            recipe.SoulAshCost = soulAshCost;
            recipe.CraftDurationSeconds = duration;
            recipe.FuelSecondsCost = fuel;
            _objects.Add(recipe);
            return recipe;
        }

        private GameObject CreatePlayer(
            int inventorySize,
            out InventoryController inventory,
            out SoulAshWallet wallet,
            out CharacterStats stats,
            out BuffController buffs)
        {
            var player = new GameObject("Player");
            _objects.Add(player);

            stats = player.AddComponent<CharacterStats>();
            ConfigureStats(stats);

            buffs = player.AddComponent<BuffController>();
            wallet = player.AddComponent<SoulAshWallet>();
            inventory = player.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", inventorySize);
            SetPrivateField(inventory, "_slots", new ItemStack[inventorySize]);
            SetPrivateField(inventory, "_stats", stats);
            SetPrivateField(inventory, "_buffs", buffs);
            return player;
        }

        private BuffController CreateBuffs(string name)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            return go.AddComponent<BuffController>();
        }

        private static void ConfigureStats(CharacterStats stats)
        {
            var configuredStats = new List<Stat>
            {
                CreateStat(StatType.Health, 50f, 0f, 100f),
                CreateStat(StatType.Hunger, 50f, 0f, 100f),
                CreateStat(StatType.MaxStamina, 50f, 0f, 100f),
                CreateStat(StatType.Stamina, 50f, 0f, 100f),
                CreateStat(StatType.StaminaRegen, 10f, 0f, 100f),
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
