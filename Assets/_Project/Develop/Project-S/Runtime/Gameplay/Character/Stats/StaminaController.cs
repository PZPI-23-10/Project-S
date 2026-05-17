using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class StaminaController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private BuffController _buffs;

        [Header("Regeneration")]
        [SerializeField] private float _regenDelay = 0.65f;
        [SerializeField] private float _blockRegenMultiplier = 0.3f;

        private float _regenBlockedUntil;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            if (_blockController == null) _blockController = GetComponent<BlockController>();
            if (_inventory == null) _inventory = GetComponent<InventoryController>();
            if (_buffs == null) _buffs = GetComponent<BuffController>();
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

            _regenBlockedUntil = Time.time + _regenDelay;
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

            float nextStamina = Mathf.Min(maxStamina, currentStamina + regenRate * Time.deltaTime);
            _stats.Set(StatType.Stamina, nextStamina);
        }

        private float GetFinalSpendAmount(float amount)
        {
            if (amount <= 0f) return 0f;

            if (_buffs == null)
                _buffs = GetComponent<BuffController>();

            float multiplier = _buffs != null ? _buffs.StaminaCostMultiplier : 1f;
            return Mathf.Max(0f, amount * multiplier);
        }
    }
}
