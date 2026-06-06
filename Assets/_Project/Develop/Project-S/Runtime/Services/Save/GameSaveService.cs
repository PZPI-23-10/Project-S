using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Harvesting;
using Project_S.Runtime.Gameplay.Portals;
using Project_S.Runtime.Gameplay.Respawn;
using Project_S.Runtime.Gameplay.Upgrades;
using Project_S.Runtime.Services.SceneManagement;
using Project_S.Runtime.Services.Storage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Services.Save
{
    public class GameSaveService : IDisposable
    {
        public const string MainSaveKey = "Game.Save.Main";

        private static readonly StatType[] VolatileStats =
        {
            StatType.Health,
            StatType.Stamina,
            StatType.Poise,
            StatType.Hunger,
            StatType.Thirst,
            StatType.Fear,
            StatType.Curse,
            StatType.PhylacteryCharge
        };

        private readonly PlayerStorage _playerStorage;
        private readonly SaveAssetRegistry _registry;
        private readonly HashSet<UnityEngine.Object> _subscribedObjects = new HashSet<UnityEngine.Object>();

        private StoredObject<GameSaveData> _storedSave;
        private GameSaveData _pendingLoad;
        private bool _isApplying;
        private bool _autosaveScheduled;
        private float _lastAutosaveTime = -999f;
        private GameSaveLifecycle _lifecycle;

        public GameSaveService(PlayerStorage playerStorage, SceneLoader sceneLoader, SaveAssetRegistry registry = null)
        {
            _playerStorage = playerStorage;
            _registry = registry ?? new SaveAssetRegistry();

            if (_playerStorage != null)
                _storedSave = new StoredObject<GameSaveData>(MainSaveKey, _playerStorage.DataStorage, new GameSaveData());

            ResetIncompatibleStoredSave();
            CreateLifecycle();
        }

        public bool HasSave => IsCompatibleSave(_storedSave?.Value);

        public string BeginLoadOrStartNew(string defaultSceneName)
        {
            ResetIncompatibleStoredSave();

            if (HasSave)
            {
                _pendingLoad = EnsureSaveShape(_storedSave.Value);
                if (!string.IsNullOrWhiteSpace(_pendingLoad.ActiveSceneName) && IsLevelScene(_pendingLoad.ActiveSceneName))
                    return _pendingLoad.ActiveSceneName;
            }

            _pendingLoad = null;
            return string.IsNullOrWhiteSpace(defaultSceneName) ? SceneNames.YavWorld : defaultSceneName;
        }

        public async UniTask LoadSavedGameOrStartNewAsync(string defaultSceneName = SceneNames.YavWorld)
        {
            string sceneName = BeginLoadOrStartNew(defaultSceneName);
            SceneTransitionRequestBus.RequestTransition(sceneName, null);
            await UniTask.Yield();
        }

        public bool ShouldRestorePlayerFromSave(string sceneName)
        {
            return IsCompatibleSave(_storedSave?.Value)
                && _pendingLoad != null
                && !string.IsNullOrWhiteSpace(sceneName)
                && _pendingLoad.ActiveSceneName == sceneName;
        }

        public void SaveNow()
        {
            SaveNow("Manual");
        }

        public void SaveNow(string reason)
        {
            if (_isApplying || _storedSave == null || _playerStorage == null)
                return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !IsLevelScene(activeScene.name))
                return;

            _registry.EnsureLoaded();
            ResetIncompatibleStoredSave();

            GameSaveData data = EnsureSaveShape(_storedSave.Value ?? new GameSaveData());
            PlayerState playerSnapshot = CapturePlayer();
            bool hadSave = data.HasSave;

            if (playerSnapshot == null && !hadSave)
                return;

            if (playerSnapshot != null)
                data.Player = playerSnapshot;
            else
                data.Player ??= new PlayerState();

            EnsureWorldShape(data.World);
            data.World.Pickups.RuntimeDropped.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && IsLevelScene(scene.name))
                    CaptureSceneIntoWorld(scene, data.World);
            }

            data.Version = GameSaveData.CurrentVersion;
            data.HasSave = true;
            data.SavedUtcTicks = DateTime.UtcNow.Ticks;
            data.ActiveSceneName = ResolveActiveLevelSceneName(data.ActiveSceneName);
            _storedSave.Value = data;
            _storedSave.Save();
            _playerStorage.DataStorage.SaveData();
            _lastAutosaveTime = Time.realtimeSinceStartup;
        }

        public void RequestAutosave(string reason)
        {
            if (_isApplying || _autosaveScheduled || _storedSave == null)
                return;

            AutosaveAfterDelay().Forget();
        }

        public void DeleteSave()
        {
            if (_playerStorage == null)
                return;

            _storedSave?.Release();
            _playerStorage.DataStorage.DeleteKey(MainSaveKey);
            _playerStorage.DataStorage.DeleteKey(UpgradeProgressStore.DefaultKey);
            _playerStorage.DataStorage.SaveData();
            _storedSave = new StoredObject<GameSaveData>(MainSaveKey, _playerStorage.DataStorage, new GameSaveData());
            _pendingLoad = null;
        }

        public void ApplyAfterSceneLoaded(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            bool canApplySave = IsCompatibleSave(_pendingLoad ?? _storedSave?.Value);

            _registry.EnsureLoaded();
            _isApplying = true;

            try
            {
                if (canApplySave)
                    ApplySceneState(scene);

                if (canApplySave && _pendingLoad != null && _pendingLoad.ActiveSceneName == scene.name)
                {
                    ApplyPlayer(_pendingLoad.Player);
                    _pendingLoad = null;
                }

                SubscribePlayer();
                SubscribeScene(scene);
            }
            finally
            {
                _isApplying = false;
            }
        }

        public void Dispose()
        {
            _storedSave?.Dispose();
            _storedSave = null;
        }

        private async UniTaskVoid AutosaveAfterDelay()
        {
            _autosaveScheduled = true;

            float dueTime = Time.realtimeSinceStartup + 2f;
            while (Application.isPlaying && Time.realtimeSinceStartup < dueTime)
                await UniTask.Yield();

            float minimumTime = _lastAutosaveTime + 10f;
            while (Application.isPlaying && Time.realtimeSinceStartup < minimumTime)
                await UniTask.Yield();

            _autosaveScheduled = false;
            SaveNow("Autosave");
        }

        private PlayerState CapturePlayer()
        {
            PlayerFacade player = FindPlayer();
            if (player == null)
                return null;

            var result = new PlayerState
            {
                Position = SaveVector3.From(player.transform.position),
                Rotation = SaveQuaternion.From(player.transform.rotation)
            };

            var inventory = player.GetComponent<InventoryController>();
            if (inventory != null)
                result.Inventory.Slots = inventory.CaptureSaveSlots(_registry);

            var wallet = player.GetComponent<SoulAshWallet>() ?? inventory?.GetComponent<SoulAshWallet>();
            result.SoulAsh = wallet != null ? wallet.Amount : 0;

            var equipment = player.GetComponentInChildren<EquipmentSlots>(true) ?? player.GetComponent<EquipmentSlots>();
            if (equipment != null)
            {
                result.Equipment.CurrentSlot = equipment.CurrentSlotIndex;
                result.Equipment.ItemIds = new List<string>();
                for (int i = 0; i < equipment.GetSize(); i++)
                    result.Equipment.ItemIds.Add(_registry.GetItemId(equipment.GetItemInSlot(i)));
            }

            var combat = player.GetComponent<CombatController>();
            if (combat != null)
            {
                result.Combat.CurrentWeaponId = _registry.GetItemId(combat.SavedCurrentWeapon);
                result.Combat.OffhandWeaponId = _registry.GetItemId(combat.EquippedOffhandItem);
            }

            var accessories = player.GetComponent<AccessorySlotController>() ?? player.GetComponentInChildren<AccessorySlotController>(true);
            if (accessories != null)
            {
                result.AccessoryItemIds = new List<string>();
                for (int i = 0; i < accessories.GetSize(); i++)
                    result.AccessoryItemIds.Add(_registry.GetItemId(accessories.GetItemInSlot(i)));
            }

            var upgrades = player.GetComponent<PlayerUpgradeController>();
            if (upgrades != null)
            {
                upgrades.EnsureInitialized();
                result.PurchasedUpgradeIds = upgrades.PurchasedUpgradeIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
            }

            var stats = player.Stats != null ? player.Stats : player.GetComponent<CharacterStats>();
            if (stats != null)
            {
                result.Stats = new Dictionary<StatType, float>();
                foreach (StatType type in VolatileStats)
                {
                    if (stats.TryGetStat(type, out _))
                        result.Stats[type] = stats.GetRaw(type);
                }
            }

            return result;
        }

        private void ApplyPlayer(PlayerState data)
        {
            PlayerFacade player = FindPlayer();
            if (player == null || data == null)
                return;

            data.Inventory ??= new InventoryState();
            data.Equipment ??= new EquipmentState();
            data.Combat ??= new CombatSaveState();
            data.AccessoryItemIds ??= new List<string>();
            data.Stats ??= new Dictionary<StatType, float>();
            data.PurchasedUpgradeIds ??= new List<string>();

            var inventory = player.GetComponent<InventoryController>();
            inventory?.RestoreSaveSlots(data.Inventory.Slots, _registry);

            var wallet = player.GetComponent<SoulAshWallet>() ?? inventory?.GetComponent<SoulAshWallet>();
            wallet?.SetAmount(data.SoulAsh);

            var equipment = player.GetComponentInChildren<EquipmentSlots>(true) ?? player.GetComponent<EquipmentSlots>();
            if (equipment != null)
            {
                var items = new List<ItemData>();
                foreach (string id in data.Equipment.ItemIds ?? new List<string>())
                    items.Add(_registry.GetItem(id));
                equipment.RestoreSlots(items, data.Equipment.CurrentSlot);
            }

            var accessories = player.GetComponent<AccessorySlotController>() ?? player.GetComponentInChildren<AccessorySlotController>(true);
            if (accessories != null)
            {
                IReadOnlyList<string> ids = data.AccessoryItemIds ?? new List<string>();
                for (int i = 0; i < accessories.GetSize(); i++)
                    accessories.SetSlot(i, i < ids.Count ? _registry.GetItem(ids[i]) as AccessoryItemData : null);
            }

            var combat = player.GetComponent<CombatController>();
            if (combat != null)
            {
                combat.EquipWeapon(_registry.GetItem(data.Combat.CurrentWeaponId) as WeaponItemData);
                combat.EquipOffhand(_registry.GetItem(data.Combat.OffhandWeaponId) as WeaponItemData);
                combat.TryShowCombatOffhand();
            }

            var upgrades = player.GetComponent<PlayerUpgradeController>();
            upgrades?.RestorePurchasedUpgradeIds(data.PurchasedUpgradeIds);

            var stats = player.Stats != null ? player.Stats : player.GetComponent<CharacterStats>();
            if (stats != null)
            {
                foreach (var stat in data.Stats)
                    stats.Set(stat.Key, stat.Value);
            }

            Quaternion rotation = data.Rotation.ToQuaternion();
            Vector3 position = data.Position.ToVector3();
            PlayerRespawnUtility.MovePlayer(player, position, rotation);
        }

        private void CaptureSceneIntoWorld(Scene scene, WorldState world)
        {
            EnsureWorldShape(world);

            foreach (var storage in FindSceneComponents<BaseResourceStorage>(scene, true))
            {
                world.Inventories[ResolveDomainId(storage)] = new InventoryState
                {
                    Slots = storage.CaptureSaveSlots(_registry),
                    SoulAsh = storage.SoulAshAmount
                };
            }

            foreach (var storage in FindSceneComponents<GeneralItemStorage>(scene, true))
            {
                world.Inventories[ResolveDomainId(storage)] = new InventoryState
                {
                    Slots = storage.CaptureSaveSlots(_registry)
                };
            }

            foreach (var station in FindSceneComponents<TimedCraftingStation>(scene, true))
            {
                world.CraftingStations[ResolveDomainId(station)] = new CraftingStationState
                {
                    FuelSeconds = station.FuelSeconds,
                    ActiveRecipeId = _registry.GetRecipeId(station.ActiveRecipe),
                    ActiveDurationSeconds = station.ActiveDurationSeconds,
                    RemainingCraftSeconds = station.RemainingCraftSeconds
                };
            }

            foreach (var node in FindSceneComponents<HarvestableResourceNode>(scene, true))
            {
                world.Resources[ResolveDomainId(node)] = new ResourceNodeState
                {
                    CurrentHealth = node.CurrentHealth,
                    Depleted = node.IsDepleted
                };
            }

            foreach (var enemy in FindSceneComponents<EnemyHealth>(scene, true))
            {
                world.Enemies[ResolveDomainId(enemy)] = new EnemyState
                {
                    CurrentHealth = enemy.CurrentHealth,
                    Dead = enemy.IsDead
                };
            }

            foreach (var portal in FindSceneComponents<BossPortal>(scene, true))
                portal.WriteToWorld(world);

            foreach (var pickup in FindSceneComponents<ItemPickup>(scene, true))
            {
                if (pickup.GetComponent<RuntimeDroppedItem>() != null)
                    continue;

                string id = ResolveDomainId(pickup);
                if (pickup.IsCollected)
                    world.Pickups.CollectedAuthoredIds.Add(id);
                else
                    world.Pickups.CollectedAuthoredIds.Remove(id);
            }

            CaptureRuntimePickups(scene, world.Pickups.RuntimeDropped);
        }

        private void ApplySceneState(Scene scene)
        {
            GameSaveData data = EnsureSaveShape(_pendingLoad ?? _storedSave?.Value);
            WorldState world = data?.World;
            if (world == null)
                return;

            EnsureWorldShape(world);

            foreach (var storage in FindSceneComponents<BaseResourceStorage>(scene, true))
            {
                if (world.Inventories.TryGetValue(ResolveDomainId(storage), out var saved))
                    storage.RestoreSaveState(saved.Slots, saved.SoulAsh, _registry);
            }

            foreach (var storage in FindSceneComponents<GeneralItemStorage>(scene, true))
            {
                if (world.Inventories.TryGetValue(ResolveDomainId(storage), out var saved))
                    storage.RestoreSaveSlots(saved.Slots, _registry);
            }

            InventoryController playerInventory = FindPlayer()?.GetComponent<InventoryController>();
            foreach (var station in FindSceneComponents<TimedCraftingStation>(scene, true))
            {
                if (world.CraftingStations.TryGetValue(ResolveDomainId(station), out var saved))
                {
                    station.RestoreSaveState(
                        saved.FuelSeconds,
                        _registry.GetRecipe(saved.ActiveRecipeId),
                        saved.ActiveDurationSeconds,
                        saved.RemainingCraftSeconds,
                        playerInventory);
                }
            }

            foreach (var node in FindSceneComponents<HarvestableResourceNode>(scene, true))
            {
                if (world.Resources.TryGetValue(ResolveDomainId(node), out var saved))
                    node.RestoreSaveState(saved.CurrentHealth, saved.Depleted);
            }

            foreach (var enemy in FindSceneComponents<EnemyHealth>(scene, true))
            {
                if (world.Enemies.TryGetValue(ResolveDomainId(enemy), out var saved))
                    enemy.RestoreSaveState(saved.CurrentHealth, saved.Dead);
            }

            foreach (var portal in FindSceneComponents<BossPortal>(scene, true))
                portal.RestoreFromWorld(world);

            foreach (var pickup in FindSceneComponents<ItemPickup>(scene, true))
            {
                if (pickup.GetComponent<RuntimeDroppedItem>() != null)
                    continue;

                bool collected = world.Pickups.CollectedAuthoredIds.Contains(ResolveDomainId(pickup));
                pickup.RestoreSaveState(pickup.Item, pickup.Amount, collected);
            }

            RestoreRuntimePickups(scene, world.Pickups.RuntimeDropped);
        }

        private void CaptureRuntimePickups(Scene scene, List<RuntimePickupState> result)
        {
            if (result == null)
                return;

            foreach (var marker in FindSceneComponents<RuntimeDroppedItem>(scene, true))
            {
                var pickup = marker.GetComponent<ItemPickup>();
                if (pickup == null || pickup.Item == null || pickup.Amount <= 0)
                    continue;

                result.Add(new RuntimePickupState
                {
                    Id = ResolveDomainId(marker),
                    SceneName = scene.name,
                    ItemId = _registry.GetItemId(pickup.Item),
                    Amount = pickup.Amount,
                    Position = SaveVector3.From(marker.transform.position),
                    Rotation = SaveQuaternion.From(marker.transform.rotation)
                });
            }
        }

        private void RestoreRuntimePickups(Scene scene, IReadOnlyList<RuntimePickupState> pickups)
        {
            foreach (var marker in FindSceneComponents<RuntimeDroppedItem>(scene, true))
                DestroyObject(marker.gameObject);

            if (pickups == null)
                return;

            foreach (var saved in pickups)
            {
                if (saved == null || (!string.IsNullOrWhiteSpace(saved.SceneName) && saved.SceneName != scene.name))
                    continue;

                ItemData item = _registry.GetItem(saved.ItemId);
                if (item == null || saved.Amount <= 0)
                {
                    Debug.LogWarning($"[Save] Runtime pickup item '{saved.ItemId}' was not found.");
                    continue;
                }

                ItemPickup pickup = WorldItemDropUtility.SpawnPickupAt(
                    item,
                    saved.Amount,
                    saved.Position.ToVector3(),
                    saved.Rotation.ToQuaternion());

                if (pickup != null)
                    SceneManager.MoveGameObjectToScene(pickup.gameObject, scene);
            }
        }

        private void SubscribePlayer()
        {
            PlayerFacade player = FindPlayer();
            if (player == null)
                return;

            Subscribe(player.GetComponent<InventoryController>(), inventory => inventory.OnInventoryChanged += OnAutosaveEvent);
            Subscribe(player.GetComponent<SoulAshWallet>(), wallet => wallet.Changed += OnAutosaveEvent);
            Subscribe(player.GetComponent<EquipmentSlots>() ?? player.GetComponentInChildren<EquipmentSlots>(true), equipment => equipment.Changed += OnAutosaveEvent);
            Subscribe(player.GetComponent<AccessorySlotController>() ?? player.GetComponentInChildren<AccessorySlotController>(true), accessories => accessories.Changed += OnAutosaveEvent);
            Subscribe(player.GetComponent<PlayerUpgradeController>(), upgrades => upgrades.Changed += OnAutosaveEvent);
            Subscribe(player.GetComponent<CharacterStats>(), stats => stats.Changed += OnAutosaveEvent);
        }

        private void SubscribeScene(Scene scene)
        {
            foreach (var storage in FindSceneComponents<BaseResourceStorage>(scene, true))
                Subscribe(storage, x => x.Changed += OnAutosaveEvent);

            foreach (var storage in FindSceneComponents<GeneralItemStorage>(scene, true))
                Subscribe(storage, x => x.Changed += OnAutosaveEvent);

            foreach (var station in FindSceneComponents<TimedCraftingStation>(scene, true))
                Subscribe(station, x => x.Changed += OnAutosaveEvent);

            foreach (var node in FindSceneComponents<HarvestableResourceNode>(scene, true))
            {
                Subscribe(node, x =>
                {
                    x.HealthChanged += OnAutosaveEvent;
                    x.HarvestCompleted += OnHarvestCompleted;
                });
            }

            foreach (var enemy in FindSceneComponents<EnemyHealth>(scene, true))
            {
                Subscribe(enemy, x =>
                {
                    x.HealthChanged += OnAutosaveEvent;
                    x.Died += OnEnemyDied;
                });
            }

            foreach (var portal in FindSceneComponents<BossPortal>(scene, true))
                Subscribe(portal, x => x.Changed += OnPortalChanged);

            foreach (var pickup in FindSceneComponents<ItemPickup>(scene, true))
            {
                if (pickup.GetComponent<RuntimeDroppedItem>() != null)
                    continue;

                Subscribe(pickup, x => x.Collected += OnPickupCollected);
            }
        }

        private void Subscribe<T>(T target, Action<T> subscribe) where T : UnityEngine.Object
        {
            if (target == null || !_subscribedObjects.Add(target))
                return;

            subscribe(target);
        }

        private void OnAutosaveEvent()
        {
            RequestAutosave("Changed");
        }

        private void OnAutosaveEvent(int _)
        {
            RequestAutosave("Changed");
        }

        private void OnAutosaveEvent(StatType _, float __)
        {
            RequestAutosave("StatsChanged");
        }

        private void OnAutosaveEvent(HarvestableResourceNode _)
        {
            RequestAutosave("ResourceChanged");
        }

        private void OnAutosaveEvent(EnemyHealth _)
        {
            RequestAutosave("EnemyChanged");
        }

        private void OnHarvestCompleted(HarvestableResourceNode _)
        {
            SaveNow("ResourceDepleted");
        }

        private void OnEnemyDied(EnemyHealth _)
        {
            SaveNow("EnemyDied");
        }

        private void OnPortalChanged(BossPortal _)
        {
            SaveNow("PortalChanged");
        }

        private void OnPickupCollected(ItemPickup _)
        {
            SaveNow("PickupCollected");
        }

        private string ResolveActiveLevelSceneName(string fallback)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (IsLevelScene(activeScene.name))
                return activeScene.name;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && IsLevelScene(scene.name))
                    return scene.name;
            }

            return string.IsNullOrWhiteSpace(fallback) ? SceneNames.YavWorld : fallback;
        }

        private void ResetIncompatibleStoredSave()
        {
            GameSaveData data = _storedSave?.Value;
            if (data == null || !data.HasSave || data.Version >= GameSaveData.CurrentVersion)
                return;

            Debug.LogWarning($"[Save] Save version {data.Version} is incompatible with version {GameSaveData.CurrentVersion}. Resetting save.");
            DeleteSave();
        }

        private static bool IsCompatibleSave(GameSaveData data)
        {
            return data != null && data.HasSave && data.Version >= GameSaveData.CurrentVersion;
        }

        private static GameSaveData EnsureSaveShape(GameSaveData data)
        {
            if (data == null)
                return null;

            data.Player ??= new PlayerState();
            data.Player.Inventory ??= new InventoryState();
            data.Player.Equipment ??= new EquipmentState();
            data.Player.Combat ??= new CombatSaveState();
            data.Player.AccessoryItemIds ??= new List<string>();
            data.Player.Stats ??= new Dictionary<StatType, float>();
            data.Player.PurchasedUpgradeIds ??= new List<string>();
            data.World ??= new WorldState();
            EnsureWorldShape(data.World);
            return data;
        }

        private static void EnsureWorldShape(WorldState world)
        {
            if (world == null)
                return;

            world.Flags ??= new HashSet<string>();
            world.Inventories ??= new Dictionary<string, InventoryState>();
            world.CraftingStations ??= new Dictionary<string, CraftingStationState>();
            world.Resources ??= new Dictionary<string, ResourceNodeState>();
            world.Enemies ??= new Dictionary<string, EnemyState>();
            world.Pickups ??= new PickupWorldState();
            world.Pickups.CollectedAuthoredIds ??= new HashSet<string>();
            world.Pickups.RuntimeDropped ??= new List<RuntimePickupState>();
        }

        private static bool IsLevelScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && sceneName != SceneNames.Boot
                && sceneName != SceneNames.Core
                && sceneName != SceneNames.Menu
                && sceneName != SceneNames.Credits;
        }

        private static PlayerFacade FindPlayer()
        {
            return UnityEngine.Object.FindFirstObjectByType<PlayerFacade>(FindObjectsInactive.Include);
        }

        private static List<T> FindSceneComponents<T>(Scene scene, bool includeInactive) where T : Component
        {
            var result = new List<T>();
            if (!scene.IsValid() || !scene.isLoaded)
                return result;

            foreach (GameObject root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<T>(includeInactive));

            return result;
        }

        private static string ResolveDomainId(Component component)
        {
            if (component == null)
                return null;

            var saveableId = component.GetComponent<SaveableObjectId>();
            if (saveableId != null && !string.IsNullOrWhiteSpace(saveableId.Id))
                return saveableId.Id;

            string scenePrefix = component.gameObject.scene.IsValid()
                ? component.gameObject.scene.name
                : "UnknownScene";

            return $"{scenePrefix}:{GetHierarchyPath(component.transform)}";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var stack = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }

        private static void DestroyObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
        }

        private void CreateLifecycle()
        {
            if (_lifecycle != null)
                return;

            if (!Application.isPlaying)
                return;

            var obj = new GameObject("GameSaveLifecycle");
            UnityEngine.Object.DontDestroyOnLoad(obj);
            _lifecycle = obj.AddComponent<GameSaveLifecycle>();
            _lifecycle.Initialize(this);
        }
    }
}
