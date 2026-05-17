using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Harvesting;
using Project_S.Runtime.Gameplay.Loot;
using UnityEngine;

namespace Project_S.Editor.Tests
{
    public class LootEconomyEditModeTests
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
        public void LootTable_RollsGuaranteedAndChanceDropsDeterministically()
        {
            var guaranteed = CreateItem("Guaranteed");
            var chanceHit = CreateItem("Chance Hit");
            var chanceMiss = CreateItem("Chance Miss");
            var table = ScriptableObject.CreateInstance<LootTableData>();
            _objects.Add(table);

            table.GuaranteedDrops = new List<LootItemDrop>
            {
                new LootItemDrop { Item = guaranteed, MinAmount = 2, MaxAmount = 4, Chance = 1f }
            };
            table.ChanceDrops = new List<LootItemDrop>
            {
                new LootItemDrop { Item = chanceHit, MinAmount = 1, MaxAmount = 3, Chance = 0.5f },
                new LootItemDrop { Item = chanceMiss, MinAmount = 1, MaxAmount = 3, Chance = 0.25f }
            };

            var rolls = table.Roll(new FixedLootRandom(
                new[] { 0.4f, 0.9f },
                new[] { 3, 2 }));

            Assert.That(rolls, Has.Count.EqualTo(2));
            Assert.That(rolls[0].Item, Is.EqualTo(guaranteed));
            Assert.That(rolls[0].Amount, Is.EqualTo(3));
            Assert.That(rolls[1].Item, Is.EqualTo(chanceHit));
            Assert.That(rolls[1].Amount, Is.EqualTo(2));
        }

        [Test]
        public void LootDropper_AddsLootAndSoulAshToPlayer()
        {
            var bone = CreateItem("Bone");
            var table = CreateLootTable(bone, amount: 2, soulAsh: 10);
            var player = CreatePlayer(4, out var inventory, out var wallet);
            var dropper = CreateDropper(table);

            Assert.That(dropper.DropFor(player), Is.True);

            Assert.That(inventory.GetItemCount(bone), Is.EqualTo(2));
            Assert.That(wallet.Amount, Is.EqualTo(10));
            Assert.That(Object.FindObjectsOfType<ItemPickup>(), Is.Empty);
        }

        [Test]
        public void LootDropper_SpawnsPickupWhenInventoryIsFull()
        {
            var blocker = CreateItem("Blocker", stackable: false);
            var leather = CreateItem("Leather");
            var table = CreateLootTable(leather, amount: 1, soulAsh: 0);
            var player = CreatePlayer(1, out var inventory, out _);
            inventory.AddItem(blocker, 1);
            var dropper = CreateDropper(table);

            Assert.That(dropper.DropFor(player), Is.True);

            var pickups = Object.FindObjectsOfType<ItemPickup>();
            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.EqualTo(leather));
            Assert.That(pickups[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void SimpleEnemy_DropsLootOnlyWhenKilledByPlayer()
        {
            var meat = CreateItem("Grey Meat");
            var table = CreateLootTable(meat, amount: 1, soulAsh: 10);
            var player = CreatePlayer(4, out var inventory, out var wallet);
            var enemy = CreateEnemy(table, health: 5f);

            enemy.ReceiveDamage(new DamageRequest(player, 5f, 0f, DamageType.Slashing));

            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(1));
            Assert.That(wallet.Amount, Is.EqualTo(10));

            var hazard = new GameObject("Hazard");
            _objects.Add(hazard);
            var secondEnemy = CreateEnemy(table, health: 5f);
            secondEnemy.ReceiveDamage(new DamageRequest(hazard, 5f, 0f, DamageType.Slashing));

            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(1));
            Assert.That(wallet.Amount, Is.EqualTo(10));
        }

        [Test]
        public void BerryBush_GrantsBerriesOnce()
        {
            var berry = CreateItem("Berry");
            var player = CreatePlayer(4, out var inventory, out _);
            var interactor = player.AddComponent<PlayerInteractor>();
            InvokePrivate(interactor, "Awake");

            var bushObject = new GameObject("Berry Bush");
            _objects.Add(bushObject);
            var bush = bushObject.AddComponent<BerryBushResourceNode>();
            bush.Configure(berry, 3, 3);

            bush.Interact(interactor);
            bush.Interact(interactor);

            Assert.That(inventory.GetItemCount(berry), Is.EqualTo(3));
            Assert.That(bush.IsDepleted, Is.True);
        }

        [Test]
        public void IronAndGromovytsiaNodes_GrantConfiguredDrops()
        {
            var ironOre = CreateItem("Iron Ore");
            var gromovytsia = CreateItem("Gromovytsia");
            var player = CreatePlayer(6, out var inventory, out _);
            var pickaxe = CreateWeapon(HarvestToolType.Pickaxe);

            var ironNode = CreateHarvestNode<IronOreNode>(CreateNodeData(ironOre, HarvestToolType.Pickaxe));
            ironNode.ReceiveDamage(new DamageRequest(player, 10f, 0f, DamageType.Blunt, pickaxe));

            var gromNode = CreateHarvestNode<GromovytsiaNode>(CreateNodeData(gromovytsia, HarvestToolType.Pickaxe));
            gromNode.ReceiveDamage(new DamageRequest(player, 10f, 0f, DamageType.Blunt, pickaxe));

            Assert.That(inventory.GetItemCount(ironOre), Is.EqualTo(2));
            Assert.That(inventory.GetItemCount(gromovytsia), Is.EqualTo(2));
        }

        private ItemData CreateItem(string itemName, bool stackable = true, int maxStack = 20)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.ItemName = itemName;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            _objects.Add(item);
            return item;
        }

        private WeaponItemData CreateWeapon(HarvestToolType tool)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponItemData>();
            weapon.HarvestTool = tool;
            _objects.Add(weapon);
            return weapon;
        }

        private GameObject CreatePlayer(int inventorySize, out InventoryController inventory, out SoulAshWallet wallet)
        {
            var player = new GameObject("Player");
            _objects.Add(player);

            inventory = player.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", inventorySize);
            SetPrivateField(inventory, "_slots", new ItemStack[inventorySize]);

            wallet = player.AddComponent<SoulAshWallet>();
            return player;
        }

        private LootTableData CreateLootTable(ItemData item, int amount, int soulAsh)
        {
            var table = ScriptableObject.CreateInstance<LootTableData>();
            table.SoulAshReward = soulAsh;
            table.GuaranteedDrops = new List<LootItemDrop>
            {
                new LootItemDrop { Item = item, MinAmount = amount, MaxAmount = amount, Chance = 1f }
            };
            _objects.Add(table);
            return table;
        }

        private LootDropper CreateDropper(LootTableData table)
        {
            var go = new GameObject("Dropper");
            _objects.Add(go);
            var dropper = go.AddComponent<LootDropper>();
            dropper.Configure(table);
            return dropper;
        }

        private SimpleEnemy CreateEnemy(LootTableData table, float health)
        {
            var enemyObject = new GameObject("Enemy");
            _objects.Add(enemyObject);
            enemyObject.AddComponent<LootDropper>().Configure(table);
            var enemy = enemyObject.AddComponent<SimpleEnemy>();
            enemy.Health = health;
            return enemy;
        }

        private ResourceNodeData CreateNodeData(ItemData item, HarvestToolType preferredTool)
        {
            var data = ScriptableObject.CreateInstance<ResourceNodeData>();
            data.MaxHealth = 5f;
            data.PreferredTool = preferredTool;
            data.MatchingToolDamageMultiplier = 1f;
            data.MismatchedToolDamageMultiplier = 0.2f;
            data.Drops = new List<ResourceDrop>
            {
                new ResourceDrop { Item = item, MinAmount = 2, MaxAmount = 2, Chance = 1f }
            };
            _objects.Add(data);
            return data;
        }

        private T CreateHarvestNode<T>(ResourceNodeData data) where T : HarvestableResourceNode
        {
            var nodeObject = new GameObject(typeof(T).Name);
            _objects.Add(nodeObject);
            var node = nodeObject.AddComponent<T>();
            node.Configure(data);
            return node;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
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

        private class FixedLootRandom : ILootRandom
        {
            private readonly Queue<float> _values;
            private readonly Queue<int> _amounts;

            public FixedLootRandom(IEnumerable<float> values, IEnumerable<int> amounts)
            {
                _values = new Queue<float>(values);
                _amounts = new Queue<int>(amounts);
            }

            public float Value()
            {
                return _values.Count > 0 ? _values.Dequeue() : 0f;
            }

            public int RangeInclusive(int minInclusive, int maxInclusive)
            {
                if (_amounts.Count <= 0)
                    return minInclusive;

                return Mathf.Clamp(_amounts.Dequeue(), minInclusive, maxInclusive);
            }
        }
    }
}
