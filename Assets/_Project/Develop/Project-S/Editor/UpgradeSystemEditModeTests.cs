using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Movement;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.HUD;
using Project_S.Runtime.Gameplay.Upgrades;
using Project_S.Runtime.Services.Storage;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Project_S.Editor.Tests
{
    public class UpgradeSystemEditModeTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private readonly List<string> _playerPrefsKeys = new List<string>();

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            foreach (string key in _playerPrefsKeys)
                PlayerPrefs.DeleteKey(key);

            _playerPrefsKeys.Clear();
            PlayerStorage.DisableSingleton();

            foreach (var obj in _objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        [Test]
        public void UpgradeService_ConsumesCostsFromInventoryWalletAndBaseStorage()
        {
            var wood = CreateItem("Wood", true, 20);
            var player = CreatePlayer(out var inventory, out var wallet);
            var baseStorage = CreateBaseStorage();
            var upgrade = CreateUpgrade("1", 8, new[] { new UpgradeItemCost { Item = wood, Amount = 3 } });

            inventory.AddItem(wood, 1);
            wallet.Add(5);
            baseStorage.AddItem(wood, 2);
            baseStorage.AddSoulAsh(3);

            var service = new UpgradeService(inventory, wallet, baseStorage);

            Assert.That(service.Check(upgrade, new HashSet<string>()).CanPurchase, Is.True);
            Assert.That(service.TryConsumeCosts(upgrade, new HashSet<string>(), out var check), Is.True, check.Message);
            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(0));
            Assert.That(baseStorage.GetItemCount(wood), Is.EqualTo(0));
            Assert.That(wallet.Amount, Is.EqualTo(0));
            Assert.That(baseStorage.SoulAshAmount, Is.EqualTo(0));
        }

        [Test]
        public void UpgradeService_DoesNotSpendAnythingWhenResourcesAreMissing()
        {
            var stone = CreateItem("Stone", true, 20);
            CreatePlayer(out var inventory, out var wallet);
            var baseStorage = CreateBaseStorage();
            var upgrade = CreateUpgrade("missing", 5, new[] { new UpgradeItemCost { Item = stone, Amount = 4 } });

            inventory.AddItem(stone, 2);
            wallet.Add(3);
            baseStorage.AddItem(stone, 1);
            baseStorage.AddSoulAsh(1);

            var service = new UpgradeService(inventory, wallet, baseStorage);

            Assert.That(service.TryConsumeCosts(upgrade, new HashSet<string>(), out var check), Is.False);
            Assert.That(check.CanPurchase, Is.False);
            Assert.That(inventory.GetItemCount(stone), Is.EqualTo(2));
            Assert.That(baseStorage.GetItemCount(stone), Is.EqualTo(1));
            Assert.That(wallet.Amount, Is.EqualTo(3));
            Assert.That(baseStorage.SoulAshAmount, Is.EqualTo(1));
        }

        [Test]
        public void UpgradeService_EnforcesDependenciesAndRejectsRepeatPurchases()
        {
            CreatePlayer(out var inventory, out var wallet);
            var service = new UpgradeService(inventory, wallet);
            var upgrade1 = CreateUpgrade("1");
            var star = CreateUpgrade("1*", prerequisites: new[] { "1", "2", "3" });
            var upgrade4 = CreateUpgrade("4", prerequisites: new[] { "1*" });

            Assert.That(service.Check(star, new HashSet<string> { "1", "2" }).CanPurchase, Is.False);
            Assert.That(service.Check(star, new HashSet<string> { "1", "2", "3" }).CanPurchase, Is.True);
            Assert.That(service.Check(upgrade4, new HashSet<string> { "1", "2", "3" }).CanPurchase, Is.False);
            Assert.That(service.Check(upgrade4, new HashSet<string> { "1", "2", "3", "1*" }).CanPurchase, Is.True);
            Assert.That(service.Check(upgrade1, new HashSet<string> { "1" }).CanPurchase, Is.False);
        }

        [Test]
        public void PlayerUpgradeController_PurchaseAppliesStatEffectsAndDynamicClamps()
        {
            var player = CreatePlayer(out var inventory, out _);
            var stats = player.AddComponent<CharacterStats>();
            ConfigureStats(stats);
            InvokePrivate(stats, "Awake");

            var controller = player.AddComponent<PlayerUpgradeController>();
            var upgrade = CreateUpgrade("stats");
            upgrade.Effects = new List<UpgradeEffect>
            {
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.MaxStamina, Amount = 10f, ExpandStatLimit = true },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.StaminaRegen, Amount = 2f },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.CarryWeight, Amount = 30f, ExpandStatLimit = true }
            };

            SetPrivateField(controller, "_loadDefinitionsFromResources", false);
            SetPrivateField(controller, "_usePersistence", false);
            SetPrivateField(controller, "_inventory", inventory);
            SetPrivateField(controller, "_stats", stats);
            SetPrivateField(controller, "_upgrades", new List<UpgradeDefinition> { upgrade });

            Assert.That(controller.TryPurchase(upgrade, out var check), Is.True, check.Message);
            Assert.That(stats.Get(StatType.MaxStamina), Is.EqualTo(110f).Within(0.001f));
            Assert.That(stats.Get(StatType.Stamina), Is.EqualTo(60f).Within(0.001f));
            Assert.That(stats.Get(StatType.StaminaRegen), Is.EqualTo(12f).Within(0.001f));
            Assert.That(stats.Get(StatType.CarryWeight), Is.EqualTo(160f).Within(0.001f));

            stats.Set(StatType.Stamina, 999f);
            Assert.That(stats.Get(StatType.Stamina), Is.EqualTo(110f).Within(0.001f));
            Assert.That(stats.GetNormalized(StatType.Stamina), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PlayerUpgradeController_AppliesAllUpgradeStatEffectsAndKeepsDynamicClamps()
        {
            var player = CreatePlayer(out var inventory, out _);
            var stats = player.AddComponent<CharacterStats>();
            ConfigureStats(stats);
            InvokePrivate(stats, "Awake");

            var controller = player.AddComponent<PlayerUpgradeController>();
            var upgrade = CreateUpgrade("all-stats");
            upgrade.Effects = new List<UpgradeEffect>
            {
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.MaxHealth, Amount = 20f, ExpandStatLimit = true },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.MaxStamina, Amount = 10f, ExpandStatLimit = true },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.MaxPoise, Amount = 10f, ExpandStatLimit = true },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.CarryWeight, Amount = 30f, ExpandStatLimit = true },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.MoveSpeed, Amount = 0.1f },
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.StaminaRegen, Amount = 2f }
            };

            ConfigureController(controller, inventory, stats, new[] { upgrade });

            Assert.That(controller.TryPurchase(upgrade, out var check), Is.True, check.Message);
            Assert.That(stats.Get(StatType.MaxHealth), Is.EqualTo(120f).Within(0.001f));
            Assert.That(stats.Get(StatType.Health), Is.EqualTo(70f).Within(0.001f));
            Assert.That(stats.Get(StatType.MaxStamina), Is.EqualTo(110f).Within(0.001f));
            Assert.That(stats.Get(StatType.Stamina), Is.EqualTo(60f).Within(0.001f));
            Assert.That(stats.Get(StatType.MaxPoise), Is.EqualTo(110f).Within(0.001f));
            Assert.That(stats.Get(StatType.Poise), Is.EqualTo(60f).Within(0.001f));
            Assert.That(stats.Get(StatType.CarryWeight), Is.EqualTo(160f).Within(0.001f));
            Assert.That(stats.Get(StatType.MoveSpeed), Is.EqualTo(5.1f).Within(0.001f));
            Assert.That(stats.Get(StatType.StaminaRegen), Is.EqualTo(12f).Within(0.001f));

            stats.Set(StatType.Health, 999f);
            stats.Set(StatType.Poise, 999f);
            Assert.That(stats.Get(StatType.Health), Is.EqualTo(120f).Within(0.001f));
            Assert.That(stats.GetNormalized(StatType.Health), Is.EqualTo(1f).Within(0.001f));
            Assert.That(stats.Get(StatType.Poise), Is.EqualTo(110f).Within(0.001f));
            Assert.That(stats.GetNormalized(StatType.Poise), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void PlayerUpgradeController_UnlockOffhandShowsEquippedOffhandPrefab()
        {
            var player = CreatePlayer(out var inventory, out _);
            var combat = player.AddComponent<CombatController>();
            var offhandHolder = new GameObject("OffhandHolder").transform;
            offhandHolder.SetParent(player.transform);
            var offhandPrefab = new GameObject("TestOffhandPrefab");
            _objects.Add(offhandPrefab);

            var unarmed = CreateWeapon("Unarmed", false, null);
            var offhand = CreateWeapon("Offhand", false, offhandPrefab);
            SetPrivateField(combat, "_offhandHolder", offhandHolder);
            SetPrivateField(combat, "_unarmedWeapon", unarmed);
            SetPrivateField(combat, "_equippedOffhandItem", offhand);

            var controller = player.AddComponent<PlayerUpgradeController>();
            var upgrade = CreateUpgrade(UpgradeIds.OffhandUnlock);
            upgrade.Effects = new List<UpgradeEffect>
            {
                new UpgradeEffect { Type = UpgradeEffectType.UnlockOffhand }
            };
            ConfigureController(controller, inventory, null, new[] { upgrade }, combat);

            Assert.That(controller.TryPurchase(upgrade, out var check), Is.True, check.Message);
            Assert.That(combat.IsOffhandSkillUnlocked, Is.True);
            Assert.That(offhandHolder.childCount, Is.EqualTo(1));
            Assert.That(offhandHolder.GetChild(0).name, Does.StartWith(offhandPrefab.name));
        }

        [Test]
        public void CharacterDamageReceiver_IgnoresHealthAndPoiseDamageWhileDodgingWithUpgrade()
        {
            LogAssert.ignoreFailingMessages = true;

            var player = CreatePlayer(out var inventory, out _);
            var stats = player.AddComponent<CharacterStats>();
            ConfigureStats(stats);
            InvokePrivate(stats, "Awake");

            var motor = player.AddComponent<CharacterMotor>();
            SetPrivateField(motor, "_dodgeUntil", Time.time + 10f);

            var controller = player.AddComponent<PlayerUpgradeController>();
            var dodgeUpgrade = CreateUpgrade(UpgradeIds.DodgeInvulnerability);
            dodgeUpgrade.Effects = new List<UpgradeEffect>
            {
                new UpgradeEffect { Type = UpgradeEffectType.DodgeInvulnerability }
            };
            ConfigureController(controller, inventory, stats, new[] { dodgeUpgrade });
            Assert.That(controller.TryPurchase(dodgeUpgrade, out var check), Is.True, check.Message);

            var receiver = player.AddComponent<CharacterDamageReceiver>();
            SetPrivateField(receiver, "_stats", stats);
            SetPrivateField(receiver, "_motor", motor);
            SetPrivateField(receiver, "_upgrades", controller);

            var attacker = new GameObject("Attacker");
            _objects.Add(attacker);
            receiver.ReceiveDamage(new DamageRequest(attacker, 25f, 15f, DamageType.Blunt));

            Assert.That(stats.Get(StatType.Health), Is.EqualTo(50f).Within(0.001f));
            Assert.That(stats.Get(StatType.Poise), Is.EqualTo(50f).Within(0.001f));
        }

        [Test]
        public void UpgradeProgressStore_PersistsPurchasedIds()
        {
            string key = "UpgradeSystemEditModeTests.PurchasedIds";
            _playerPrefsKeys.Add(key);
            PlayerPrefs.DeleteKey(key);

            var storageObject = new GameObject("PlayerStorage");
            _objects.Add(storageObject);
            var storage = storageObject.AddComponent<PlayerStorage>();

            using (var first = new UpgradeProgressStore(storage, key))
            {
                Assert.That(first.Add("1"), Is.True);
            }

            using (var second = new UpgradeProgressStore(storage, key))
            {
                Assert.That(second.Has("1"), Is.True);
            }
        }

        [Test]
        public void PlayerUpgradeController_PersistsAndReappliesPurchasedUpgradesAfterRecreate()
        {
            const string key = "Upgrades.PurchasedIds";
            _playerPrefsKeys.Add(key);
            PlayerPrefs.DeleteKey(key);

            var storageObject = new GameObject("PlayerStorage");
            _objects.Add(storageObject);
            var storage = storageObject.AddComponent<PlayerStorage>();

            var upgrade = CreateUpgrade("persistent-stat");
            upgrade.Effects = new List<UpgradeEffect>
            {
                new UpgradeEffect { Type = UpgradeEffectType.StatAdd, StatType = StatType.MaxHealth, Amount = 20f, ExpandStatLimit = true }
            };

            var firstPlayer = CreatePlayer(out var firstInventory, out _);
            var firstStats = firstPlayer.AddComponent<CharacterStats>();
            ConfigureStats(firstStats);
            InvokePrivate(firstStats, "Awake");
            var firstController = firstPlayer.AddComponent<PlayerUpgradeController>();
            ConfigureController(firstController, firstInventory, firstStats, new[] { upgrade }, playerStorage: storage, usePersistence: true);

            Assert.That(firstController.TryPurchase(upgrade, out var firstCheck), Is.True, firstCheck.Message);
            Assert.That(firstStats.Get(StatType.MaxHealth), Is.EqualTo(120f).Within(0.001f));

            Object.DestroyImmediate(firstPlayer);

            var secondPlayer = CreatePlayer(out var secondInventory, out _);
            var secondStats = secondPlayer.AddComponent<CharacterStats>();
            ConfigureStats(secondStats);
            InvokePrivate(secondStats, "Awake");
            var secondController = secondPlayer.AddComponent<PlayerUpgradeController>();
            ConfigureController(secondController, secondInventory, secondStats, new[] { upgrade }, playerStorage: storage, usePersistence: true);

            secondController.EnsureInitialized();

            Assert.That(secondController.HasUpgrade("persistent-stat"), Is.True);
            Assert.That(secondStats.Get(StatType.MaxHealth), Is.EqualTo(120f).Within(0.001f));
            Assert.That(secondStats.Get(StatType.Health), Is.EqualTo(70f).Within(0.001f));
        }

        [Test]
        public void InventoryUI_TabMethodsShowOnlySelectedPanel()
        {
            var uiObject = new GameObject("InventoryUI");
            _objects.Add(uiObject);
            var ui = uiObject.AddComponent<InventoryUI>();

            var mainWindow = new GameObject("MainInventoryWindow");
            var inventoryPanel = new GameObject("InventoryPanel");
            var contextPanel = new GameObject("ContextPanel");
            var upgradePanel = new GameObject("UpgradePanel");
            var lootingPanel = new GameObject("LootingPanel");
            var tabButtons = new GameObject("TabButtons");
            _objects.AddRange(new Object[] { mainWindow, inventoryPanel, contextPanel, upgradePanel, lootingPanel, tabButtons });

            SetPrivateField(ui, "_mainInventoryWindow", mainWindow);
            SetPrivateField(ui, "_inventoryPanel", inventoryPanel);
            SetPrivateField(ui, "_contextPanel", contextPanel);
            SetPrivateField(ui, "_upgradePanelRoot", upgradePanel);
            SetPrivateField(ui, "_lootingPanel", lootingPanel);
            SetPrivateField(ui, "_tabButtons", tabButtons);

            ui.ShowUpgradeTab();
            Assert.That(mainWindow.activeSelf, Is.True);
            Assert.That(inventoryPanel.activeSelf, Is.False);
            Assert.That(contextPanel.activeSelf, Is.False);
            Assert.That(upgradePanel.activeSelf, Is.True);

            ui.ShowCraftingTab();
            Assert.That(inventoryPanel.activeSelf, Is.False);
            Assert.That(contextPanel.activeSelf, Is.True);
            Assert.That(upgradePanel.activeSelf, Is.False);

            ui.ShowInventoryTab();
            Assert.That(inventoryPanel.activeSelf, Is.True);
            Assert.That(contextPanel.activeSelf, Is.False);
            Assert.That(upgradePanel.activeSelf, Is.False);
        }

        [Test]
        public void CoreScene_ContainsEditableUpgradePanelAndTabWiring()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Core.unity");

            var upgradePanel = FindSceneGameObject("UpgradePanel");
            var upgradeTab = FindSceneGameObject("UpgradeTabButton");
            var mainWindow = FindSceneGameObject("MainInventoryWindow");
            var tabButtons = FindSceneGameObject("TabButtons");
            var inventoryUi = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);

            Assert.That(upgradePanel, Is.Not.Null);
            Assert.That(upgradeTab, Is.Not.Null);
            Assert.That(upgradePanel.transform.parent, Is.EqualTo(mainWindow.transform));
            Assert.That(upgradeTab.transform.parent, Is.EqualTo(tabButtons.transform));

            var upgradePanelUi = upgradePanel.GetComponent<UpgradePanelUI>();
            Assert.That(upgradePanelUi, Is.Not.Null);
            Assert.That(upgradePanel.GetComponentsInChildren<UpgradeNodeView>(true).Length, Is.EqualTo(7));
            Assert.That(GetPrivateField<GameObject>(inventoryUi, "_upgradePanelRoot"), Is.EqualTo(upgradePanel));
            Assert.That(GetPrivateField<Button>(inventoryUi, "_upgradeTabButton"), Is.EqualTo(upgradeTab.GetComponent<Button>()));
            Assert.That(upgradeTab.GetComponent<Button>().onClick.GetPersistentMethodName(0), Is.EqualTo(nameof(InventoryUI.ShowUpgradeTab)));
        }

        private GameObject CreatePlayer(out InventoryController inventory, out SoulAshWallet wallet)
        {
            var player = new GameObject("Player");
            _objects.Add(player);

            inventory = player.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", 8);
            SetPrivateField(inventory, "_slots", new ItemStack[8]);

            wallet = player.AddComponent<SoulAshWallet>();
            return player;
        }

        private BaseResourceStorage CreateBaseStorage()
        {
            var storageObject = new GameObject("BaseStorage");
            _objects.Add(storageObject);
            var storage = storageObject.AddComponent<BaseResourceStorage>();
            SetPrivateField(storage, "_storageSize", 8);
            SetPrivateField(storage, "_slots", new ItemStack[8]);
            return storage;
        }

        private ItemData CreateItem(string itemName, bool stackable, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            _objects.Add(item);
            item.ItemName = itemName;
            item.Kind = ItemKind.Resource;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            return item;
        }

        private UpgradeDefinition CreateUpgrade(
            string id,
            int soulAshCost = 0,
            IEnumerable<UpgradeItemCost> itemCosts = null,
            IEnumerable<string> prerequisites = null)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            _objects.Add(upgrade);
            upgrade.Id = id;
            upgrade.DisplayName = id;
            upgrade.SoulAshCost = soulAshCost;
            upgrade.ItemCosts = itemCosts != null ? new List<UpgradeItemCost>(itemCosts) : new List<UpgradeItemCost>();
            upgrade.PrerequisiteIds = prerequisites != null ? new List<string>(prerequisites) : new List<string>();
            return upgrade;
        }

        private WeaponItemData CreateWeapon(string itemName, bool isTwoHanded, GameObject weaponPrefab)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponItemData>();
            _objects.Add(weapon);
            weapon.ItemName = itemName;
            weapon.Kind = ItemKind.Weapon;
            weapon.IsStackable = false;
            weapon.MaxStack = 1;
            weapon.IsTwoHanded = isTwoHanded;
            weapon.WeaponPrefab = weaponPrefab;
            return weapon;
        }

        private void ConfigureStats(CharacterStats stats)
        {
            SetPrivateField(stats, "_stats", new List<Stat>
            {
                CreateStat(StatType.MaxHealth, 100f, 0f, 100f),
                CreateStat(StatType.Health, 50f, 0f, 100f),
                CreateStat(StatType.MaxStamina, 100f, 0f, 100f),
                CreateStat(StatType.Stamina, 50f, 0f, 100f),
                CreateStat(StatType.StaminaRegen, 10f, 0f, 60f),
                CreateStat(StatType.MoveSpeed, 5f, 0f, 20f),
                CreateStat(StatType.CarryWeight, 130f, 0f, 130f),
                CreateStat(StatType.Poise, 50f, 0f, 100f),
                CreateStat(StatType.MaxPoise, 100f, 0f, 100f)
            });
        }

        private static void ConfigureController(
            PlayerUpgradeController controller,
            InventoryController inventory,
            CharacterStats stats,
            IEnumerable<UpgradeDefinition> upgrades,
            CombatController combat = null,
            PlayerStorage playerStorage = null,
            bool usePersistence = false)
        {
            SetPrivateField(controller, "_loadDefinitionsFromResources", false);
            SetPrivateField(controller, "_usePersistence", usePersistence);
            SetPrivateField(controller, "_inventory", inventory);
            SetPrivateField(controller, "_stats", stats);
            SetPrivateField(controller, "_combat", combat);
            SetPrivateField(controller, "_playerStorage", playerStorage);
            SetPrivateField(controller, "_upgrades", upgrades != null ? new List<UpgradeDefinition>(upgrades) : new List<UpgradeDefinition>());
        }

        private static Stat CreateStat(StatType type, float baseValue, float minValue, float maxValue)
        {
            var stat = new Stat();
            SetPrivateField(stat, "_type", type);
            SetPrivateField(stat, "_baseValue", baseValue);
            SetPrivateField(stat, "_minValue", minValue);
            SetPrivateField(stat, "_maxValue", maxValue);
            return stat;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            Assert.That(target, Is.Not.Null);
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            return (T)field.GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method {methodName} was not found.");
            method.Invoke(target, null);
        }

        private static GameObject FindSceneGameObject(string objectName)
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(x => x.name == objectName && x.scene.IsValid());
        }
    }
}
