using System;
using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Services.Storage;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Gameplay.Upgrades
{
    public class PlayerUpgradeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private SoulAshWallet _wallet;
        [SerializeField] private CombatController _combat;

        [Header("Definitions")]
        [SerializeField] private bool _loadDefinitionsFromResources = true;
        [SerializeField] private string _resourcesPath = "Upgrades";
        [SerializeField] private List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();

        [Header("Persistence")]
        [SerializeField] private bool _usePersistence = true;

        [Inject(Optional = true)] private PlayerStorage _playerStorage;

        private readonly HashSet<string> _appliedIds = new HashSet<string>();
        private UpgradeProgressStore _progressStore;
        private bool _initialized;

        public event Action Changed;

        public IReadOnlyCollection<string> PurchasedUpgradeIds
        {
            get
            {
                EnsureInitialized();
                return _progressStore != null ? _progressStore.PurchasedIds : Array.Empty<string>();
            }
        }

        public IReadOnlyList<UpgradeDefinition> Upgrades
        {
            get
            {
                EnsureInitialized();
                return _upgrades;
            }
        }

        public InventoryController Inventory
        {
            get
            {
                EnsureInitialized();
                return _inventory;
            }
        }

        public SoulAshWallet Wallet
        {
            get
            {
                EnsureInitialized();
                return _wallet;
            }
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void OnDestroy()
        {
            _progressStore?.Dispose();
            _progressStore = null;
        }

        public void EnsureInitialized()
        {
            if (_initialized)
                return;

            ResolveReferences();
            LoadDefinitions();
            InitializeProgressStore();
            _initialized = true;
            ApplyPurchasedUpgrades();
            SyncUnlockStates();
        }

        public bool HasUpgrade(string id)
        {
            EnsureInitialized();
            return _progressStore != null && _progressStore.Has(id);
        }

        public UpgradeDefinition GetUpgrade(string id)
        {
            EnsureInitialized();
            return _upgrades.FirstOrDefault(x => x != null && x.Id == id);
        }

        public UpgradeCheck Check(UpgradeDefinition upgrade)
        {
            EnsureInitialized();
            return CreateService().Check(upgrade, _progressStore?.PurchasedIds);
        }

        public bool TryPurchase(UpgradeDefinition upgrade, out UpgradeCheck check)
        {
            EnsureInitialized();

            if (!CreateService().TryConsumeCosts(upgrade, _progressStore?.PurchasedIds, out check))
                return false;

            if (upgrade == null || _progressStore == null || !_progressStore.Add(upgrade.Id))
            {
                check.AddProblem("Не вдалося зберегти апгрейд.");
                return false;
            }

            ApplyUpgrade(upgrade);
            SyncUnlockStates();
            Changed?.Invoke();
            return true;
        }

        public void RestorePurchasedUpgradeIds(IEnumerable<string> ids)
        {
            EnsureInitialized();
            _progressStore?.Replace(ids);
            ApplyPurchasedUpgrades();
            SyncUnlockStates();
            Changed?.Invoke();
        }

        public int GetOwnedItemCount(ItemData item)
        {
            EnsureInitialized();
            return CreateService().GetItemCount(item);
        }

        public int GetOwnedSoulAsh()
        {
            EnsureInitialized();
            return CreateService().GetSoulAshCount();
        }

        private void ResolveReferences()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            if (_inventory == null) _inventory = GetComponent<InventoryController>();
            if (_wallet == null) _wallet = GetComponent<SoulAshWallet>();
            if (_combat == null) _combat = GetComponent<CombatController>();

            if (_wallet == null && _inventory != null)
                _wallet = _inventory.GetComponent<SoulAshWallet>();

            if (_usePersistence && _playerStorage == null && PlayerStorage.HasInstance)
                _playerStorage = PlayerStorage.GetInstance;
        }

        private void LoadDefinitions()
        {
            if (!_loadDefinitionsFromResources)
                return;

            var loaded = Resources
                .LoadAll<UpgradeDefinition>(_resourcesPath)
                .Where(x => x != null)
                .OrderBy(x => x.Id)
                .ToList();

            if (loaded.Count > 0)
                _upgrades = loaded;
        }

        private void InitializeProgressStore()
        {
            _progressStore = new UpgradeProgressStore((DataStorage)null);
        }

        private void ApplyPurchasedUpgrades()
        {
            if (_progressStore == null)
                return;

            foreach (string id in _progressStore.PurchasedIds.ToList())
            {
                var upgrade = _upgrades.FirstOrDefault(x => x != null && x.Id == id);
                if (upgrade != null)
                    ApplyUpgrade(upgrade);
            }
        }

        private void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null || string.IsNullOrWhiteSpace(upgrade.Id) || !_appliedIds.Add(upgrade.Id))
                return;

            foreach (var effect in upgrade.Effects ?? Enumerable.Empty<UpgradeEffect>())
                ApplyEffect(effect);
        }

        private void ApplyEffect(UpgradeEffect effect)
        {
            if (effect == null)
                return;

            switch (effect.Type)
            {
                case UpgradeEffectType.StatAdd:
                    ApplyStatEffect(effect);
                    break;
                case UpgradeEffectType.UnlockOffhand:
                    _combat?.SetOffhandSkillUnlocked(true);
                    _combat?.TryShowCombatOffhand();
                    break;
                case UpgradeEffectType.DodgeInvulnerability:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ApplyStatEffect(UpgradeEffect effect)
        {
            if (_stats == null || Mathf.Approximately(effect.Amount, 0f))
                return;

            switch (effect.StatType)
            {
                case StatType.MaxHealth:
                    _stats.AddMaximumAndCurrent(StatType.MaxHealth, StatType.Health, effect.Amount);
                    break;
                case StatType.MaxStamina:
                    _stats.AddMaximumAndCurrent(StatType.MaxStamina, StatType.Stamina, effect.Amount);
                    break;
                case StatType.MaxPoise:
                    _stats.AddMaximumAndCurrent(StatType.MaxPoise, StatType.Poise, effect.Amount);
                    break;
                default:
                    _stats.AddPermanent(effect.StatType, effect.Amount, effect.ExpandStatLimit);
                    break;
            }
        }

        private void SyncUnlockStates()
        {
            if (_combat != null)
                _combat.SetOffhandSkillUnlocked(HasUpgrade(UpgradeIds.OffhandUnlock));
        }

        private UpgradeService CreateService()
        {
            return new UpgradeService(_inventory, _wallet, BaseResourceStorage.Active);
        }
    }
}
