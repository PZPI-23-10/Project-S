using System;
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Harvesting;
using Project_S.Runtime.Gameplay.Upgrades;
using Project_S.Runtime.Services.Save;
using Project_S.Runtime.Services.SceneManagement;
using Project_S.Runtime.Services.Storage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Editor.Tests
{
    public class GameSaveSystemEditModeTests
    {
        private const string TestSceneName = "GameSaveSystemEditModeScene";
        private const string TestScenePath = "Assets/_Project/Develop/Project-S/Editor/Tests/GameSaveSystemEditModeScene.unity";

        private readonly List<UnityEngine.Object> _objects = new List<UnityEngine.Object>();
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _hadMainSave;
        private string _mainSaveBackup;
        private bool _hadUpgradeProgress;
        private string _upgradeProgressBackup;

        [SetUp]
        public void SetUp()
        {
            BackupRealPlayerPrefsKeys();
            AssetDatabase.DeleteAsset(TestScenePath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Scene scene = SceneManager.GetActiveScene();
            Assert.That(EditorSceneManager.SaveScene(scene, TestScenePath), Is.True);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            Assert.That(scene.name, Is.EqualTo(TestSceneName));
            SceneManager.SetActiveScene(scene);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var disposable in _disposables)
                disposable.Dispose();

            _disposables.Clear();

            foreach (var obj in _objects)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }

            _objects.Clear();
            PlayerStorage.DisableSingleton();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(TestScenePath);
            RestoreRealPlayerPrefsKeys();
        }

        [Test]
        public void StoredObject_PersistsSingleMainSaveSlot()
        {
            const string key = "GameSaveSystemEditModeTests.Main";
            var storage = CreateStorage();
            storage.DataStorage.DeleteKey(key);

            var first = new StoredObject<GameSaveData>(key, storage.DataStorage, new GameSaveData());
            first.Value.HasSave = true;
            first.Value.ActiveSceneName = "YavWorld";
            first.Save();
            first.Dispose();

            var second = new StoredObject<GameSaveData>(key, storage.DataStorage, new GameSaveData());
            Assert.That(second.Value.HasSave, Is.True);
            Assert.That(second.Value.ActiveSceneName, Is.EqualTo("YavWorld"));
            second.Dispose();

            storage.DataStorage.DeleteKey(key);
        }

        [Test]
        public void SaveAssetRegistry_LoadsItemAndRecipeAssets()
        {
            var registry = new SaveAssetRegistry();

            var wood = registry.GetItem("Wood");
            var recipe = registry.GetRecipe("Campfire_RoastedBerries");

            Assert.That(wood, Is.Not.Null);
            Assert.That(registry.GetItemId(wood), Is.EqualTo("Wood"));
            Assert.That(recipe, Is.Not.Null);
            Assert.That(registry.GetRecipeId(recipe), Is.EqualTo("Campfire_RoastedBerries"));
        }

        [Test]
        public void InventoryAndStorages_RoundtripSlotsAndSoulAsh()
        {
            var registry = new SaveAssetRegistry();
            var wood = LoadItem("Crafting/Items/Resources/Wood");
            var stone = LoadItem("Crafting/Items/Resources/Stone");

            var inventory = CreateInventory("InventoryA");
            inventory.AddItem(wood, 7);
            inventory.AddItem(stone, 3);

            var inventorySnapshot = inventory.CaptureSaveSlots(registry);
            var restoredInventory = CreateInventory("InventoryB");
            restoredInventory.RestoreSaveSlots(inventorySnapshot, registry);

            Assert.That(restoredInventory.GetItemCount(wood), Is.EqualTo(7));
            Assert.That(restoredInventory.GetItemCount(stone), Is.EqualTo(3));

            var baseStorage = CreateBaseStorage("BaseA");
            baseStorage.AddItem(wood, 4);
            baseStorage.AddSoulAsh(9);

            var baseSlots = baseStorage.CaptureSaveSlots(registry);
            var restoredBase = CreateBaseStorage("BaseB");
            restoredBase.RestoreSaveState(baseSlots, baseStorage.SoulAshAmount, registry);

            Assert.That(restoredBase.GetItemCount(wood), Is.EqualTo(4));
            Assert.That(restoredBase.SoulAshAmount, Is.EqualTo(9));

            var chest = CreateGeneralStorage("ChestA");
            chest.AddItem(stone, 5);

            var chestSlots = chest.CaptureSaveSlots(registry);
            var restoredChest = CreateGeneralStorage("ChestB");
            restoredChest.RestoreSaveSlots(chestSlots, registry);

            Assert.That(restoredChest.GetItemCount(stone), Is.EqualTo(5));
        }

        [Test]
        public void GameSaveService_RoundtripsPlayerState()
        {
            Scene scene = SceneManager.GetActiveScene();
            var registry = new SaveAssetRegistry();
            var wood = LoadItem("Crafting/Items/Resources/Wood");
            var weapon = LoadWeapon("Crafting/Items/Weapons/WoodClub");
            var offhand = LoadWeapon("Crafting/Items/Weapons/FlintKnife");
            var storage = CreateStorage();

            var player = CreatePlayer(out var inventory, out var wallet, out var stats, out var equipment, out var combat);
            player.transform.SetPositionAndRotation(new Vector3(3f, 4f, 5f), Quaternion.Euler(0f, 45f, 0f));
            inventory.AddItem(wood, 7);
            wallet.Add(123);
            equipment.RestoreSlots(new List<ItemData> { weapon, null, null }, 0);
            combat.EquipWeapon(weapon);
            combat.EquipOffhand(offhand);
            stats.Set(StatType.Health, 42f);
            stats.Set(StatType.Stamina, 21f);

            using (var service = CreateService(storage, registry))
                service.SaveNow();

            inventory.RestoreSaveSlots(new List<ItemStackSaveData>(), registry);
            wallet.SetAmount(0);
            equipment.RestoreSlots(new List<ItemData> { null, null, null }, 0);
            combat.EquipWeapon(null);
            combat.EquipOffhand(null);
            stats.Set(StatType.Health, 1f);
            stats.Set(StatType.Stamina, 1f);
            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            using (var service = CreateService(storage, registry))
            {
                Assert.That(service.BeginLoadOrStartNew("Fallback"), Is.EqualTo(scene.name));
                service.ApplyAfterSceneLoaded(scene);
            }

            Assert.That(inventory.GetItemCount(wood), Is.EqualTo(7));
            Assert.That(wallet.Amount, Is.EqualTo(123));
            Assert.That(equipment.GetItemInSlot(0), Is.EqualTo(weapon));
            Assert.That(combat.SavedCurrentWeapon, Is.EqualTo(weapon));
            Assert.That(combat.EquippedOffhandItem, Is.EqualTo(offhand));
            Assert.That(stats.GetRaw(StatType.Health), Is.EqualTo(42f).Within(0.001f));
            Assert.That(stats.GetRaw(StatType.Stamina), Is.EqualTo(21f).Within(0.001f));
            Assert.That(player.transform.position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
        }

        [Test]
        public void GameSaveService_SaveWithoutPlayerDoesNotOverwriteExistingPlayerSnapshot()
        {
            Scene scene = SceneManager.GetActiveScene();
            var registry = new SaveAssetRegistry();
            var wood = LoadItem("Crafting/Items/Resources/Wood");
            var storage = CreateStorage();

            var player = CreatePlayer(out var inventory, out var wallet, out _, out _, out _);
            player.transform.position = new Vector3(9f, 1f, 2f);
            inventory.AddItem(wood, 11);
            wallet.SetAmount(77);

            using (var service = CreateService(storage, registry))
                service.SaveNow();

            UnityEngine.Object.DestroyImmediate(player.gameObject);

            using (var service = CreateService(storage, registry))
                service.SaveNow("NoPlayerDuringSceneRestart");

            var restoredPlayer = CreatePlayer(out var restoredInventory, out var restoredWallet, out _, out _, out _);
            restoredPlayer.transform.position = Vector3.zero;

            using (var service = CreateService(storage, registry))
            {
                Assert.That(service.BeginLoadOrStartNew("Fallback"), Is.EqualTo(scene.name));
                service.ApplyAfterSceneLoaded(scene);
            }

            Assert.That(restoredInventory.GetItemCount(wood), Is.EqualTo(11));
            Assert.That(restoredWallet.Amount, Is.EqualTo(77));
            Assert.That(restoredPlayer.transform.position, Is.EqualTo(new Vector3(9f, 1f, 2f)));
        }

        [Test]
        public void GameSaveService_RoundtripsWorldState()
        {
            Scene scene = SceneManager.GetActiveScene();
            var registry = new SaveAssetRegistry();
            var wood = LoadItem("Crafting/Items/Resources/Wood");
            var stone = LoadItem("Crafting/Items/Resources/Stone");
            var recipe = registry.GetRecipe("Campfire_RoastedBerries");
            var storage = CreateStorage();
            CreatePlayer(out _, out _, out _, out _, out _);

            var baseStorage = CreateBaseStorage("BaseStorage");
            baseStorage.AddItem(wood, 4);
            baseStorage.AddSoulAsh(9);

            var chest = CreateGeneralStorage("Chest");
            chest.AddItem(stone, 5);

            var station = CreateStation("Campfire");
            station.RestoreSaveState(100f, recipe, 10f, 5f, null);

            var resource = CreateResourceNode("Resource", 50f);
            resource.RestoreSaveState(0f, true);

            var enemy = CreateEnemy("Enemy", 30f);
            enemy.RestoreSaveState(8f, false);

            var authoredPickup = CreateScenePickup("AuthoredPickup", wood, 2);
            authoredPickup.RestoreSaveState(wood, 2, true);

            var pickup = WorldItemDropUtility.SpawnPickupAt(stone, 3, new Vector3(1f, 0f, 2f), Quaternion.Euler(0f, 15f, 0f));
            _objects.Add(pickup.gameObject);

            using (var service = CreateService(storage, registry))
                service.SaveNow();

            baseStorage.RestoreSaveState(new List<ItemStackSaveData>(), 0, registry);
            chest.RestoreSaveSlots(new List<ItemStackSaveData>(), registry);
            station.RestoreSaveState(0f, null, 0f, 0f, null);
            resource.RestoreSaveState(50f, false);
            enemy.RestoreSaveState(30f, false);
            authoredPickup.RestoreSaveState(wood, 2, false);
            UnityEngine.Object.DestroyImmediate(pickup.gameObject);

            using (var service = CreateService(storage, registry))
            {
                Assert.That(service.BeginLoadOrStartNew("Fallback"), Is.EqualTo(scene.name));
                service.ApplyAfterSceneLoaded(scene);
            }

            Assert.That(baseStorage.GetItemCount(wood), Is.EqualTo(4));
            Assert.That(baseStorage.SoulAshAmount, Is.EqualTo(9));
            Assert.That(chest.GetItemCount(stone), Is.EqualTo(5));
            Assert.That(station.FuelSeconds, Is.EqualTo(100f).Within(0.001f));
            Assert.That(station.ActiveRecipe, Is.EqualTo(recipe));
            Assert.That(station.RemainingCraftSeconds, Is.EqualTo(5f).Within(0.001f));
            Assert.That(resource.IsDepleted, Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(8f).Within(0.001f));
            Assert.That(authoredPickup.IsCollected, Is.True);
            Assert.That(authoredPickup.gameObject.activeSelf, Is.False);

            var restoredPickup = UnityEngine.Object.FindFirstObjectByType<RuntimeDroppedItem>();
            Assert.That(restoredPickup, Is.Not.Null);
            var restoredItemPickup = restoredPickup.GetComponent<ItemPickup>();
            Assert.That(restoredItemPickup.Item, Is.EqualTo(stone));
            Assert.That(restoredItemPickup.Amount, Is.EqualTo(3));
        }

        [Test]
        public void GameSaveService_DeleteSaveClearsMainSaveAndUpgradeProgress()
        {
            var storage = CreateStorage();
            var registry = new SaveAssetRegistry();

            using (var service = CreateService(storage, registry))
                service.SaveNow();

            using (var upgrades = new UpgradeProgressStore(storage))
                Assert.That(upgrades.Add("1"), Is.True);

            using (var service = CreateService(storage, registry))
                service.DeleteSave();

            var stored = new StoredObject<GameSaveData>(GameSaveService.MainSaveKey, storage.DataStorage, new GameSaveData());
            Assert.That(stored.Value.HasSave, Is.False);
            stored.Dispose();

            using (var upgrades = new UpgradeProgressStore(storage))
                Assert.That(upgrades.Has("1"), Is.False);
        }

        private GameSaveService CreateService(PlayerStorage storage, SaveAssetRegistry registry)
        {
            var service = new GameSaveService(storage, new SceneLoader(), registry);
            _disposables.Add(service);
            return service;
        }

        private PlayerStorage CreateStorage()
        {
            var obj = new GameObject("PlayerStorage");
            _objects.Add(obj);
            return obj.AddComponent<PlayerStorage>();
        }

        private InventoryController CreateInventory(string name)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            var inventory = obj.AddComponent<InventoryController>();
            InvokePrivate(inventory, "Awake");
            return inventory;
        }

        private BaseResourceStorage CreateBaseStorage(string name)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            var storage = obj.AddComponent<BaseResourceStorage>();
            InvokePrivate(storage, "OnEnable");
            return storage;
        }

        private GeneralItemStorage CreateGeneralStorage(string name)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            var storage = obj.AddComponent<GeneralItemStorage>();
            InvokePrivate(storage, "OnEnable");
            return storage;
        }

        private TimedCraftingStation CreateStation(string name)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            var station = obj.AddComponent<TimedCraftingStation>();
            InvokePrivate(station, "Awake");
            return station;
        }

        private HarvestableResourceNode CreateResourceNode(string name, float maxHealth)
        {
            var data = ScriptableObject.CreateInstance<ResourceNodeData>();
            data.MaxHealth = maxHealth;
            _objects.Add(data);

            var obj = new GameObject(name);
            _objects.Add(obj);
            var node = obj.AddComponent<HarvestableResourceNode>();
            node.Configure(data);
            return node;
        }

        private EnemyHealth CreateEnemy(string name, float maxHealth)
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.MaxHealth = maxHealth;
            _objects.Add(config);

            var obj = new GameObject(name);
            _objects.Add(obj);
            var enemy = obj.AddComponent<EnemyHealth>();
            enemy.Configure(config);
            return enemy;
        }

        private ItemPickup CreateScenePickup(string name, ItemData item, int amount)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            var pickup = obj.AddComponent<ItemPickup>();
            pickup.Item = item;
            pickup.Amount = amount;
            return pickup;
        }

        private PlayerFacade CreatePlayer(
            out InventoryController inventory,
            out SoulAshWallet wallet,
            out CharacterStats stats,
            out EquipmentSlots equipment,
            out CombatController combat)
        {
            var playerObject = new GameObject("Player");
            _objects.Add(playerObject);
            var weaponHolder = new GameObject("WeaponHolder").transform;
            weaponHolder.SetParent(playerObject.transform);
            var offhandHolder = new GameObject("OffhandHolder").transform;
            offhandHolder.SetParent(playerObject.transform);

            var player = playerObject.AddComponent<PlayerFacade>();
            stats = playerObject.AddComponent<CharacterStats>();
            ConfigureStats(stats);
            InvokePrivate(stats, "Awake");

            inventory = playerObject.AddComponent<InventoryController>();
            InvokePrivate(inventory, "Awake");
            wallet = playerObject.AddComponent<SoulAshWallet>();
            equipment = playerObject.AddComponent<EquipmentSlots>();
            combat = playerObject.AddComponent<CombatController>();
            SetPrivateField(combat, "_weaponHolder", weaponHolder);
            SetPrivateField(combat, "_offhandHolder", offhandHolder);
            return player;
        }

        private void ConfigureStats(CharacterStats stats)
        {
            SetPrivateField(stats, "_stats", new List<Stat>
            {
                CreateStat(StatType.MaxHealth, 100f, 0f, 300f),
                CreateStat(StatType.Health, 100f, 0f, 300f),
                CreateStat(StatType.MaxStamina, 50f, 0f, 200f),
                CreateStat(StatType.Stamina, 50f, 0f, 200f),
                CreateStat(StatType.MaxPoise, 30f, 0f, 200f),
                CreateStat(StatType.Poise, 30f, 0f, 200f),
                CreateStat(StatType.Hunger, 0f, 0f, 100f),
                CreateStat(StatType.Thirst, 0f, 0f, 100f),
                CreateStat(StatType.Fear, 0f, 0f, 100f),
                CreateStat(StatType.Curse, 0f, 0f, 100f),
                CreateStat(StatType.PhylacteryCharge, 10f, 0f, 100f),
                CreateStat(StatType.CarryWeight, 100f, 0f, 500f)
            });
        }

        private Stat CreateStat(StatType type, float baseValue, float minValue, float maxValue)
        {
            var stat = new Stat();
            SetPrivateField(stat, "_type", type);
            SetPrivateField(stat, "_baseValue", baseValue);
            SetPrivateField(stat, "_minValue", minValue);
            SetPrivateField(stat, "_maxValue", maxValue);
            return stat;
        }

        private ItemData LoadItem(string path)
        {
            var item = Resources.Load<ItemData>(path);
            Assert.That(item, Is.Not.Null, path);
            return item;
        }

        private WeaponItemData LoadWeapon(string path)
        {
            var item = Resources.Load<WeaponItemData>(path);
            Assert.That(item, Is.Not.Null, path);
            return item;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private void BackupRealPlayerPrefsKeys()
        {
            _hadMainSave = PlayerPrefs.HasKey(GameSaveService.MainSaveKey);
            _mainSaveBackup = _hadMainSave ? PlayerPrefs.GetString(GameSaveService.MainSaveKey) : null;
            _hadUpgradeProgress = PlayerPrefs.HasKey(UpgradeProgressStore.DefaultKey);
            _upgradeProgressBackup = _hadUpgradeProgress ? PlayerPrefs.GetString(UpgradeProgressStore.DefaultKey) : null;
        }

        private void RestoreRealPlayerPrefsKeys()
        {
            RestoreStringKey(GameSaveService.MainSaveKey, _hadMainSave, _mainSaveBackup);
            RestoreStringKey(UpgradeProgressStore.DefaultKey, _hadUpgradeProgress, _upgradeProgressBackup);
            PlayerPrefs.Save();
        }

        private static void RestoreStringKey(string key, bool hadKey, string value)
        {
            if (hadKey)
                PlayerPrefs.SetString(key, value);
            else
                PlayerPrefs.DeleteKey(key);
        }
    }
}
