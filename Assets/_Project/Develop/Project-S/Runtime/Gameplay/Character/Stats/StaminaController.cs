using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class StaminaController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private float _regenDelay = 0.65f;

        private float _regenBlockedUntil;

        public bool Has(float amount)
        {
            return _stats.Get(StatType.Stamina) >= amount;
        }

        public bool Spend(float amount)
        {
            if (!Has(amount))
                return false;

            _stats.Add(StatType.Stamina, -amount);
            _regenBlockedUntil = Time.time + _regenDelay;
            return true;
        }

        private void Update()
        {
            if (Time.time < _regenBlockedUntil)
                return;

            var stamina = _stats.Get(StatType.Stamina);
            var maxStamina = _stats.Get(StatType.MaxStamina);

            if (stamina >= maxStamina)
                return;

            _stats.Set(StatType.Stamina, stamina + _stats.Get(StatType.StaminaRegen) * Time.deltaTime);
        }
    }
}
