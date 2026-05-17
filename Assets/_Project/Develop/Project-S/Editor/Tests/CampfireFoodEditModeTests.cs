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
    public class CampfireFoodEditModeTests
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
        public void CampfireFuel_RemovesWoodAndCapsAtMax()
        {
            var wood = CreateItem("Wood", true, 20);
            var player = CreatePlayer(4, out var inventory, out _, out _, out _);
            var campfire = CreateCampfire(wood);
            inventory.AddItem(wood, 4);

            Assert.That(campfire.TryAddFuel(inventory), Is.True);
            Assert.That(campfire.TryAddFuel(inventory), Is.True);
            Assert.That(campfire.TryAddFuel(inventory), Is.True);
            Assert.That(campfire.TryAddFuel(inventory), Is.False);
            Assert.That(campfire.FuelSeconds, Is.EqualTo(900f).Within(0.001f));
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(1));
            Assert.That(player, Is.Not.Null);
        }

        [Test]
        public void CampfireRecipe_RequiresFuelCostsAndOutputCapacity()
        {
            var wood = CreateItem("Wood", true, 20);
            var raw = CreateItem("Grey Meat", true, 10);
            var cooked = CreateItem("Roasted Meat", true, 4);
            var blocker = CreateItem("Blocker", false, 1);
            CreatePlayer(2, out var inventory, out var wallet, out _, out _);
            var campfire = CreateCampfire(wood);
            var recipe = CreateCampfireRecipe(cooked, 1, new[] { new CraftingItemAmount(raw, 1) }, soulAshCost: 10, duration: 10f, fuel: 10f);

            inventory.AddItem(raw, 1);
            wallet.Add(10);

            Assert.That(campfire.CheckRecipe(recipe, inventory, wallet).CanCraft, Is.False);

            inventory.AddItem(wood, 1);
            Assert.That(campfire.TryAddFuel(inventory), Is.True);
            wallet.Spend(10);

            Assert.That(campfire.CheckRecipe(recipe, inventory, wallet).CanCraft, Is.False);

            CreatePlayer(1, out var fullInventory, out var fullWallet, out _, out _);
            fullInventory.AddItem(blocker, 1);
            fullWallet.Add(10);
            SetPrivateField(campfire, "_fuelSeconds", 20f);

            var noRemovalRecipe = CreateCampfireRecipe(cooked, 1, new CraftingItemAmount[0], soulAshCost: 0, duration: 1f, fuel: 1f);
            Assert.That(campfire.CheckRecipe(noRemovalRecipe, fullInventory, fullWallet).CanCraft, Is.False);
        }

        [Test]
        public void CampfireRecipe_ConsumesCostsOnStartAndGrantsOutputOnFinish()
        {
            var wood = CreateItem("Wood", true, 20);
            var raw = CreateItem("Grey Meat", true, 10);
            var cooked = CreateItem("Roasted Meat", true, 4);
            CreatePlayer(4, out var inventory, out var wallet, out _, out _);
            var campfire = CreateCampfire(wood);
            var recipe = CreateCampfireRecipe(cooked, 1, new[] { new CraftingItemAmount(raw, 1) }, soulAshCost: 10, duration: 5f, fuel: 5f);

            inventory.AddItem(raw, 1);
            inventory.AddItem(wood, 1);
            wallet.Add(10);
            campfire.TryAddFuel(inventory);

            Assert.That(campfire.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            Assert.That(inventory.GetItemCount(raw), Is.EqualTo(0));
            Assert.That(wallet.Amount, Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(cooked), Is.EqualTo(0));

            campfire.Tick(4f);
            Assert.That(inventory.GetItemCount(cooked), Is.EqualTo(0));

            campfire.Tick(1f);
            Assert.That(inventory.GetItemCount(cooked), Is.EqualTo(1));

            campfire.Tick(10f);
            Assert.That(inventory.GetItemCount(cooked), Is.EqualTo(1));
        }

        [Test]
        public void CampfireRecipe_SpawnsPickupIfInventoryFillsBeforeFinish()
        {
            var wood = CreateItem("Wood", true, 20);
            var raw = CreateItem("Grey Meat", true, 10);
            var cooked = CreateItem("Roasted Meat", true, 4);
            var blocker = CreateItem("Blocker", false, 1);
            CreatePlayer(1, out var inventory, out var wallet, out _, out _);
            var campfire = CreateCampfire(wood);
            var recipe = CreateCampfireRecipe(cooked, 1, new[] { new CraftingItemAmount(raw, 1) }, duration: 1f, fuel: 1f);

            inventory.AddItem(raw, 1);
            SetPrivateField(campfire, "_fuelSeconds", 10f);

            Assert.That(campfire.TryStartRecipe(recipe, inventory, wallet, out var check), Is.True, check.Message);
            inventory.AddItem(blocker, 1);
            campfire.Tick(1f);

            var pickups = Object.FindObjectsOfType<ItemPickup>();
            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.EqualTo(cooked));
            Assert.That(pickups[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void FoodUse_RestoresStatsAppliesBuffAndConsumesStack()
        {
            var berry = CreateItem("Berry", true, 30);
            berry.HungerRestoreAmount = 10f;
            berry.TimedBuffType = TimedBuffType.SoulAshReward;
            berry.TimedBuffCategory = TimedBuffCategory.Food;
            berry.TimedBuffMultiplier = 1.1f;
            berry.TimedBuffDurationSeconds = 60f;

            CreatePlayer(2, out var inventory, out _, out var stats, out var buffs);
            inventory.AddItem(berry, 1);

            Assert.That(inventory.TryUseItemAtSlot(0), Is.True);
            Assert.That(inventory.GetItemCount(berry), Is.EqualTo(0));
            Assert.That(stats.Get(StatType.Hunger), Is.EqualTo(40f).Within(0.001f));
            Assert.That(buffs.SoulAshRewardMultiplier, Is.EqualTo(1.1f).Within(0.001f));
        }

        [Test]
        public void FoodBuff_ReplacesFoodBuffButKeepsDebuff()
        {
            var player = CreatePlayer(2, out _, out _, out _, out var buffs);

            buffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Food, 1.1f, 60f);
            buffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Debuff, 0.95f, 60f);
            buffs.ApplyBuff(TimedBuffType.SoulAshReward, TimedBuffCategory.Food, 1.1f, 60f);

            Assert.That(buffs.AttackDamageMultiplier, Is.EqualTo(0.95f).Within(0.001f));
            Assert.That(buffs.SoulAshRewardMultiplier, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(player, Is.Not.Null);
        }

        [Test]
        public void AttackBuff_ModifiesMeleeHitDamage()
        {
            var attacker = CreatePlayer(2, out _, out _, out _, out var buffs);
            buffs.ApplyBuff(TimedBuffType.AttackDamage, TimedBuffCategory.Food, 2f, 60f);

            var weapon = ScriptableObject.CreateInstance<WeaponItemData>();
            weapon.DamageProfile = new List<DamageInstance> { new DamageInstance { Type = DamageType.Slashing, Amount = 10f } };
            _objects.Add(weapon);

            var hitbox = new GameObject("Hitbox");
            _objects.Add(hitbox);
            hitbox.AddComponent<BoxCollider>();
            var tester = hitbox.AddComponent<MeleeHitTester>();
            tester.Setup(weapon, attacker);
            tester.StartHitDetection();

            var target = new GameObject("Target");
            _objects.Add(target);
            var collider = target.AddComponent<BoxCollider>();
            var receiver = target.AddComponent<CaptureDamageReceiver>();

            InvokePrivate(tester, "OnTriggerEnter", collider);

            Assert.That(receiver.Received, Is.True);
            Assert.That(receiver.LastRequest.HealthDamage, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void SoulAshRewardBuff_AffectsRewardsButNotDirectAdds()
        {
            var player = CreatePlayer(2, out _, out var wallet, out _, out var buffs);
            buffs.ApplyBuff(TimedBuffType.SoulAshReward, TimedBuffCategory.Food, 1.1f, 60f);

            wallet.AddReward(10, player);
            wallet.Add(10);

            Assert.That(wallet.Amount, Is.EqualTo(21));
        }

        private ItemData CreateItem(string itemName, bool stackable, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.ItemName = itemName;
            item.Kind = ItemKind.Consumable;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            _objects.Add(item);
            return item;
        }

        private CampfireStation CreateCampfire(ItemData wood)
        {
            var go = new GameObject("Campfire");
            _objects.Add(go);
            var station = go.AddComponent<CampfireStation>();
            station.Configure(wood);
            return station;
        }

        private CraftingRecipeData CreateCampfireRecipe(
            ItemData output,
            int outputAmount,
            IEnumerable<CraftingItemAmount> ingredients,
            int soulAshCost = 0,
            float duration = 5f,
            float fuel = 5f)
        {
            var recipe = ScriptableObject.CreateInstance<CraftingRecipeData>();
            recipe.Context = CraftingContext.Campfire;
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

        private static void ConfigureStats(CharacterStats stats)
        {
            var configuredStats = new List<Stat>
            {
                CreateStat(StatType.Health, 50f, 0f, 100f),
                CreateStat(StatType.Hunger, 50f, 0f, 100f),
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
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method {methodName} was not found.");
            method.Invoke(target, args);
        }

        public class CaptureDamageReceiver : MonoBehaviour, IDamageReceiver
        {
            public bool Received;
            public DamageRequest LastRequest;

            public void ReceiveDamage(DamageRequest request)
            {
                Received = true;
                LastRequest = request;
            }
        }
    }
}
