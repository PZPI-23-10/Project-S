using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Harvesting;
using UnityEngine;

namespace Project_S.Editor.Tests
{
    public class HarvestingMvpEditModeTests
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
        public void ResourceNode_AppliesToolMultiplier()
        {
            var wood = CreateItem("Wood", true, 20);
            var data = CreateNodeData(wood, maxHealth: 10f, preferredTool: HarvestToolType.Axe, soulAsh: 0);
            data.MismatchedToolDamageMultiplier = 0.25f;

            var node = CreateNode(data);
            var player = CreatePlayer(out _, out _);
            var knife = CreateWeapon(HarvestToolType.Knife);

            node.ReceiveDamage(new DamageRequest(player, 8f, 0f, DamageType.Slashing, knife));

            Assert.That(node.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
        }

        [Test]
        public void ResourceNode_GrantsItemsAndSoulAshOnBreak()
        {
            var wood = CreateItem("Wood", true, 20);
            var data = CreateNodeData(wood, maxHealth: 5f, preferredTool: HarvestToolType.Axe, soulAsh: 2);
            var node = CreateNode(data);
            var player = CreatePlayer(out var inventory, out var wallet);
            var axe = CreateWeapon(HarvestToolType.Axe);

            node.ReceiveDamage(new DamageRequest(player, 5f, 0f, DamageType.Slashing, axe));

            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(2));
            Assert.That(wallet.Amount, Is.EqualTo(2));
        }

        [Test]
        public void ResourceNode_SpawnsPickupWhenInventoryIsFull()
        {
            var blocker = CreateItem("Blocker", false, 1);
            var wood = CreateItem("Wood", true, 20);
            var data = CreateNodeData(wood, maxHealth: 5f, preferredTool: HarvestToolType.Axe, soulAsh: 0);
            var node = CreateNode(data);
            var player = CreatePlayer(out var inventory, out _);
            var axe = CreateWeapon(HarvestToolType.Axe);

            SetPrivateField(inventory, "_inventorySize", 1);
            SetPrivateField(inventory, "_slots", new[] { new ItemStack(blocker, 1) });

            node.ReceiveDamage(new DamageRequest(player, 5f, 0f, DamageType.Slashing, axe));

            var pickups = Object.FindObjectsOfType<ItemPickup>();
            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.EqualTo(wood));
            Assert.That(pickups[0].Amount, Is.EqualTo(2));
        }

        [Test]
        public void SimpleEnemy_GrantsSoulAshWhenKilledByPlayer()
        {
            var player = CreatePlayer(out _, out var wallet);
            var enemyObject = new GameObject("Enemy");
            _objects.Add(enemyObject);
            var enemy = enemyObject.AddComponent<SimpleEnemy>();
            enemy.Health = 5f;

            enemy.ReceiveDamage(new DamageRequest(player, 5f, 0f, DamageType.Slashing));

            Assert.That(wallet.Amount, Is.EqualTo(10));
        }

        [Test]
        public void SimpleEnemy_DoesNotGrantSoulAshWhenSourceHasNoPlayerEconomy()
        {
            CreatePlayer(out _, out var wallet);
            var hazard = new GameObject("Hazard");
            _objects.Add(hazard);
            var enemyObject = new GameObject("Enemy");
            _objects.Add(enemyObject);
            var enemy = enemyObject.AddComponent<SimpleEnemy>();
            enemy.Health = 5f;

            enemy.ReceiveDamage(new DamageRequest(hazard, 5f, 0f, DamageType.Slashing));

            Assert.That(wallet.Amount, Is.EqualTo(0));
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

        private WeaponItemData CreateWeapon(HarvestToolType tool)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponItemData>();
            weapon.HarvestTool = tool;
            _objects.Add(weapon);
            return weapon;
        }

        private ResourceNodeData CreateNodeData(ItemData item, float maxHealth, HarvestToolType preferredTool, int soulAsh)
        {
            var data = ScriptableObject.CreateInstance<ResourceNodeData>();
            data.MaxHealth = maxHealth;
            data.PreferredTool = preferredTool;
            data.MatchingToolDamageMultiplier = 1f;
            data.MismatchedToolDamageMultiplier = 0.25f;
            data.SoulAshReward = soulAsh;
            data.Drops = new List<ResourceDrop>
            {
                new ResourceDrop
                {
                    Item = item,
                    MinAmount = 2,
                    MaxAmount = 2,
                    Chance = 1f
                }
            };
            _objects.Add(data);
            return data;
        }

        private HarvestableResourceNode CreateNode(ResourceNodeData data)
        {
            var nodeObject = new GameObject("Node");
            _objects.Add(nodeObject);
            var node = nodeObject.AddComponent<HarvestableResourceNode>();
            node.Configure(data);
            return node;
        }

        private GameObject CreatePlayer(out InventoryController inventory, out SoulAshWallet wallet)
        {
            var player = new GameObject("Player");
            _objects.Add(player);

            inventory = player.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", 16);
            SetPrivateField(inventory, "_slots", new ItemStack[16]);

            wallet = player.AddComponent<SoulAshWallet>();
            return player;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            field.SetValue(target, value);
        }
    }
}
