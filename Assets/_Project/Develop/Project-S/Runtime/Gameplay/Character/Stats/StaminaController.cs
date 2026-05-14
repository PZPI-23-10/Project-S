using Project_S.Runtime.Gameplay.Character.Combat;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class StaminaController : MonoBehaviour
    {
        [Header("Зв'язки")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;

        [Header("Налаштування регенерації")]
        [SerializeField] private float _regenDelay = 0.65f;
        [SerializeField] private float _blockRegenMultiplier = 0.3f;

        [Header("Налаштування бігу")]
        [SerializeField] private float _runCostPerSecond = 15f; // Скільки стаміни знімає біг за 1 секунду

        private float _regenBlockedUntil;

        // Властивість, яку може встановлювати твій скрипт пересування
        public bool IsRunning { get; set; }

        public bool Has(float amount) => _stats.Get(StatType.Stamina) >= amount;

        public bool Spend(float amount)
        {
            if (!Has(amount)) return false;
            _stats.Add(StatType.Stamina, -amount);
            _regenBlockedUntil = Time.time + _regenDelay;
            return true;
        }

        private void Update()
        {
            float currentStamina = _stats.Get(StatType.Stamina);
            float maxStamina = _stats.Get(StatType.MaxStamina);

            // ВИПРАВЛЕННЯ: Додали UnityEngine. перед Input, щоб обійти конфлікт імен
            bool isActuallyRunning = IsRunning || (UnityEngine.Input.GetKey(KeyCode.LeftShift) && UnityEngine.Input.GetAxis("Vertical") > 0);

            if (isActuallyRunning && currentStamina > 0)
            {
                // Витрачаємо стаміну за час (deltaTime)
                _stats.Add(StatType.Stamina, -_runCostPerSecond * Time.deltaTime);

                // Поки ми біжимо, регенерація заблокована
                _regenBlockedUntil = Time.time + _regenDelay;
                return; // Виходимо, щоб регенерація не працювала під час бігу
            }

            // РЕГЕНЕРАЦІЯ (якщо не біжимо і минула затримка)
            if (Time.time < _regenBlockedUntil) return;
            if (currentStamina >= maxStamina) return;

            float regenRate = _stats.Get(StatType.StaminaRegen);

            // Сповільнюємо реген, якщо піднятий блок
            if (_blockController != null && _blockController.IsBlocking)
            {
                regenRate *= _blockRegenMultiplier;
            }

            float nextStamina = Mathf.Min(maxStamina, currentStamina + regenRate * Time.deltaTime);
            _stats.Set(StatType.Stamina, nextStamina);
        }
    }
}