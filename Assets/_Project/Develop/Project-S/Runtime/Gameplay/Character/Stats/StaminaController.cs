using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Respawn;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class StaminaController : MonoBehaviour, IPlayerRespawnResettable
    {
        [Header("References")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private BuffController _buffs;

        [Header("Regeneration")]
        [Min(0f)]
        [Tooltip("Seconds after stamina decreases before regeneration can start.")]
        [SerializeField] private float _regenDelay = 0.65f;
        [SerializeField] private float _blockRegenMultiplier = 0.3f;

        private float _regenBlockedUntil;
        private float _lastObservedStamina;
        private bool _isRegenerating;

        public void ResetForRespawn()
        {
            _regenBlockedUntil = 0f;
            SyncLastObservedStamina();
        }

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            if (_blockController == null) _blockController = GetComponent<BlockController>();
            if (_inventory == null) _inventory = GetComponent<InventoryController>();
            if (_buffs == null) _buffs = GetComponent<BuffController>();
            SyncLastObservedStamina();
        }

        private void OnEnable()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            if (_stats != null)
            {
                _stats.Changed -= OnStatChanged;
                _stats.Changed += OnStatChanged;
                SyncLastObservedStamina();
            }
        }

        private void OnDisable()
        {
            if (_stats != null)
                _stats.Changed -= OnStatChanged;
        }

        public bool Has(float amount)
        {
            float finalAmount = GetFinalSpendAmount(amount);
            return finalAmount <= 0f || (_stats != null && _stats.Get(StatType.Stamina) >= finalAmount);
        }

        public bool Spend(float amount)
        {
            float finalAmount = GetFinalSpendAmount(amount);
            if (finalAmount > 0f && (_stats == null || _stats.Get(StatType.Stamina) < finalAmount))
                return false;

            if (finalAmount > 0f)
                _stats.Add(StatType.Stamina, -finalAmount);

            BlockRegeneration();
            return true;
        }

        private void Update()
        {
            if (_stats == null) return;
            if (Time.time < _regenBlockedUntil) return;

            float currentStamina = _stats.Get(StatType.Stamina);
            float maxStamina = _stats.Get(StatType.MaxStamina);

            if (currentStamina >= maxStamina) return;

            float regenRate = _stats.Get(StatType.StaminaRegen);

            if (_blockController != null && _blockController.IsBlocking)
                regenRate *= _blockRegenMultiplier;

            if (_inventory != null)
                regenRate *= _inventory.GetWeightPenaltyMultiplier();

            if (regenRate <= 0f)
                return;

            float nextStamina = Mathf.Min(maxStamina, currentStamina + regenRate * Time.deltaTime);
            if (nextStamina <= currentStamina)
                return;

            _isRegenerating = true;
            try
            {
                _stats.Set(StatType.Stamina, nextStamina);
            }
            finally
            {
                _isRegenerating = false;
                SyncLastObservedStamina();
            }
        }

        private float GetFinalSpendAmount(float amount)
        {
            if (amount <= 0f) return 0f;

            if (_buffs == null)
                _buffs = GetComponent<BuffController>();

            float multiplier = _buffs != null ? _buffs.StaminaCostMultiplier : 1f;
            return Mathf.Max(0f, amount * multiplier);
        }

        private void OnStatChanged(StatType type, float value)
        {
            if (type != StatType.Stamina)
                return;

            if (_isRegenerating)
            {
                _lastObservedStamina = value;
                return;
            }

            if (value < _lastObservedStamina - 0.001f)
                BlockRegeneration();

            _lastObservedStamina = value;
        }

        private void BlockRegeneration()
        {
            float delay = Mathf.Max(0f, _regenDelay);
            _regenBlockedUntil = Mathf.Max(_regenBlockedUntil, Time.time + delay);
        }

        private void SyncLastObservedStamina()
        {
            _lastObservedStamina = _stats != null ? _stats.Get(StatType.Stamina) : 0f;
        }
    }
}
