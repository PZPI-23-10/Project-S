using System;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Loot;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public class EnemyHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private LootDropper _lootDropper;

        private float _currentHealth;
        private bool _dead;

        public event Action<EnemyHealth> Died;
        public event Action<EnemyHealth> HealthChanged;

        public EnemyConfig Config => _config;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _config != null ? Mathf.Max(1f, _config.MaxHealth) : 1f;
        public float NormalizedHealth => Mathf.Clamp01(_currentHealth / MaxHealth);
        public bool IsDead => _dead;

        private void Awake()
        {
            EnsureReferences();
            ResetHealth();
        }

        public void Configure(EnemyConfig config)
        {
            _config = config;
            ResetHealth();
        }

        public void ReceiveDamage(DamageRequest request)
        {
            if (_dead)
                return;

            if (_currentHealth <= 0f)
                ResetHealth();

            float damage = Mathf.Max(0f, request.HealthDamage);
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            HealthChanged?.Invoke(this);
            Debug.Log($"[Enemy] {EnemyName()} took {damage:F1} damage. HP: {_currentHealth:F1}");

            if (_currentHealth <= 0f)
                Die(request.Source);
        }

        private void Die(GameObject source)
        {
            if (_dead)
                return;

            _dead = true;
            EnsureReferences();

            if (_lootDropper != null)
                _lootDropper.DropFor(source, SoulAshReward());
            else
                TryGrantSoulAsh(source);

            Died?.Invoke(this);

            if (Application.isPlaying)
                Destroy(gameObject, DestroyDelayAfterDeath());
            else
                DestroyImmediate(gameObject);
        }

        private void ResetHealth()
        {
            _dead = false;
            _currentHealth = MaxHealth;
            HealthChanged?.Invoke(this);
        }

        private void EnsureReferences()
        {
            if (_lootDropper == null)
                _lootDropper = GetComponent<LootDropper>();
        }

        private string EnemyName()
        {
            if (_config != null && !string.IsNullOrWhiteSpace(_config.DisplayName))
                return _config.DisplayName;

            return name;
        }

        private int SoulAshReward()
        {
            return _config != null ? Mathf.Max(0, _config.SoulAshReward) : 0;
        }

        private float DestroyDelayAfterDeath()
        {
            return _config != null ? Mathf.Max(0f, _config.DestroyDelayAfterDeath) : 0f;
        }

        private void TryGrantSoulAsh(GameObject source)
        {
            int reward = SoulAshReward();
            if (reward <= 0 || source == null)
                return;

            var wallet = source.GetComponentInParent<SoulAshWallet>();
            if (wallet == null)
            {
                var inventory = source.GetComponentInParent<InventoryController>();
                if (inventory != null)
                    wallet = inventory.GetComponent<SoulAshWallet>();
            }

            if (wallet != null)
                wallet.AddReward(reward, source);
        }
    }
}
