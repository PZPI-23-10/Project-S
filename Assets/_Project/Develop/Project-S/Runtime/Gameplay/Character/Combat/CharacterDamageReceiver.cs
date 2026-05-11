using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class CharacterDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Зв'язки")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private CombatConfig _combatConfig;

        public void ReceiveDamage(DamageRequest request)
        {
            float finalDamage = request.HealthDamage;
            float poiseDamage = request.PoiseDamage;

            if (_blockController != null && _blockController.IsBlocking)
            {
                if (_blockController.IsParryWindow)
                {
                    finalDamage = 0;
                    poiseDamage = 5;
                    Debug.Log("<color=cyan>[COMBAT]</color> PARRY!");
                }
                else
                {
                    float multiplier = _combatConfig != null ? _combatConfig.BlockDamageMultiplier : 0.5f;
                    finalDamage *= multiplier;
                    Debug.Log($"<color=blue>[COMBAT]</color> BLOCKED! Урон зменшено до {finalDamage}");
                }
            }

            if (_stats != null)
            {
                // Використовуємо метод Add, який ми знайшли в CharacterStats.cs
                // Передаємо мінус finalDamage, щоб відняти ХП
                _stats.Add(StatType.Health, -finalDamage);

                // Те саме для рівноваги (Poise)
                _stats.Add(StatType.Poise, -poiseDamage);
            }
        }
    }
}