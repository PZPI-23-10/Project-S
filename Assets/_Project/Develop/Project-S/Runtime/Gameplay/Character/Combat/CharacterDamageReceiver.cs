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
        [SerializeField] private PoiseController _poiseController;
        [SerializeField] private StaminaController _staminaController;

        public void ReceiveDamage(DamageRequest request)
        {
            float finalHealthDamage = request.HealthDamage;
            float finalPoiseDamage = request.PoiseDamage;

            // --- ЛОГІКА ЗАХИСТУ ---
            if (_blockController != null && _blockController.IsBlocking)
            {
                if (_blockController.IsParryWindow)
                {
                    finalHealthDamage = 0f;
                    finalPoiseDamage = 0f;
                    Debug.Log("<color=cyan>[ЗАХИСТ]</color> ПАРІРУВАННЯ! Енергію збережено.");
                }
                else
                {
                    float mult = _combatConfig != null ? _combatConfig.BlockDamageMultiplier : 0.35f;
                    finalHealthDamage *= mult;
                    finalPoiseDamage *= (mult * 0.5f);

                    // --- ПРАВИЛЬНЕ СПИСАННЯ СТАМІНИ ---
                    if (_staminaController != null)
                    {
                        float staminaCost = request.HealthDamage * 0.5f;

                        // Він автоматично заблокує регенерацію на 0.65 секунд
                        if (_staminaController.Spend(staminaCost))
                        {
                            Debug.Log($"<color=blue>[ЗАХИСТ]</color> БЛОК! Витрачено стаміни: {staminaCost}");
                        }
                        else
                        {

                            float currentStamina = _stats != null ? _stats.Get(StatType.Stamina) : 0f;
                            if (currentStamina > 0) _staminaController.Spend(currentStamina); // Списуємо залишки

                            finalHealthDamage = request.HealthDamage;
                            finalPoiseDamage = request.PoiseDamage;
                            Debug.LogWarning("<color=red>[ЗАХИСТ]</color> ПРОБИТТЯ БЛОКУ! Не вистачило енергії.");
                        }
                    }
                    else if (_stats != null) 
                    {
                        _stats.Add(StatType.Stamina, -request.HealthDamage * 0.5f);
                    }
                }
            }

            if (_stats != null)
            {
                if (finalHealthDamage > 0) _stats.Add(StatType.Health, -finalHealthDamage);

                if (finalPoiseDamage > 0)
                {
                    if (_poiseController != null)
                    {
                        Vector3 attackerPos = request.Source != null ? request.Source.transform.position : transform.position + transform.forward;

                        _poiseController.ApplyPoiseDamage(finalPoiseDamage, attackerPos);
                    }
                    else
                    {
                        _stats.Add(StatType.Poise, -finalPoiseDamage);
                    }
                }
            }
        }
    }
}