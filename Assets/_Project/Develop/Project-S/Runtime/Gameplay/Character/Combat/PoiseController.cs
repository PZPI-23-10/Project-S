using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class PoiseController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private CombatConfig _config;

        public bool IsBroken => _stats.Get(StatType.Poise) <= 0f;

        public void ApplyPoiseDamage(float amount)
        {
            _stats.Add(StatType.Poise, -amount);
        }

        private void Update()
        {
            if (IsBroken)
                return;

            var maxPoise = _stats.Get(StatType.MaxPoise);
            var poise = _stats.Get(StatType.Poise);

            if (poise < maxPoise)
                _stats.Set(StatType.Poise, poise + _config.PoiseRecoveryPerSecond * Time.deltaTime);
        }
    }
}
