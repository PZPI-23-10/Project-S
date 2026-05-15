using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class StaminaController : MonoBehaviour
    {
        [Header("Зв'язки")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private InventoryController _inventory;

        [Header("Налаштування регенерації")]
        [SerializeField] private float _regenDelay = 0.65f;
        [SerializeField] private float _blockRegenMultiplier = 0.3f;

        private float _regenBlockedUntil;

        public bool Has(float amount)
        {
            return _stats.Get(StatType.Stamina) > 0.01f;
        }
        // Цей метод викликає CharacterMotor для витрати стаміни
        public bool Spend(float amount)
        {
            if (!Has(amount)) return false; // Якщо вже мінус - блокуємо дію

            _stats.Add(StatType.Stamina, -amount); // Віднімаємо (може піти в мінус)
            _regenBlockedUntil = Time.time + _regenDelay;
            return true;
        }

        private void Update()
        {
            // Якщо час затримки ще не пройшов - нічого не робимо
            if (Time.time < _regenBlockedUntil) return;

            float currentStamina = _stats.Get(StatType.Stamina);
            float maxStamina = _stats.Get(StatType.MaxStamina);

            if (currentStamina >= maxStamina) return;

            float regenRate = _stats.Get(StatType.StaminaRegen);

            if (_blockController != null && _blockController.IsBlocking)
            {
                regenRate *= _blockRegenMultiplier;
            }

            if (_inventory != null)
            {
                regenRate *= _inventory.GetWeightPenaltyMultiplier();
            }

            float nextStamina = Mathf.Min(maxStamina, currentStamina + regenRate * Time.deltaTime);
            _stats.Set(StatType.Stamina, nextStamina);
        }
    }
}