using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class CharacterDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [Header("Зв'язки")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private CombatController _combatController; // Змінили конфіг на новий контролер
        [SerializeField] private PoiseController _poiseController;
        [SerializeField] private StaminaController _staminaController;

        public void ReceiveDamage(DamageRequest request)
        {
            // Беремо оригінальний урон
            DamageRequest modifiedRequest = request;

            // --- ЛОГІКА ЗАХИСТУ ---
            if (_blockController != null && _blockController.IsBlocking)
            {
                // Пропускаємо урон через блок (він сам застосує відсоток зброї)
                modifiedRequest = _blockController.ModifyIncomingDamage(request);

                // Якщо урон став 0, а був більший за 0 - це ідеальне парирування!
                if (modifiedRequest.HealthDamage == 0f && request.HealthDamage > 0f)
                {
                    Debug.Log("<color=cyan>[ЗАХИСТ]</color> ПАРИРУВАННЯ! Енергію збережено.");

                    // Повертаємо стаміну за успішне парирування (з паспорта зброї)
                    if (_combatController != null && _combatController.CurrentWeapon != null)
                    {
                        _stats.Add(StatType.Stamina, _combatController.CurrentWeapon.ParryStaminaReward);
                    }
                }
                else // Це звичайний блок (урон просто зменшився)
                {
                    float staminaCost = request.HealthDamage * 0.5f;

                    if (_staminaController != null)
                    {
                        // Пробуємо витратити стаміну на блок
                        if (_staminaController.Spend(staminaCost))
                        {
                            Debug.Log($"<color=blue>[ЗАХИСТ]</color> БЛОК! Витрачено стаміни: {staminaCost}");
                        }
                        else
                        {
                            // Якщо стаміна вже в мінусі - БЛОК ПРОБИТО
                            modifiedRequest = request; // Повертаємо повний урон (без порізки блоком)
                            Debug.LogWarning("<color=red>[ЗАХИСТ]</color> ПРОБИТТЯ БЛОКУ! Не вистачило енергії.");
                        }
                    }
                }
            }

            // --- ЗАСТОСУВАННЯ ФІНАЛЬНОГО УРОНУ ---
            if (_stats != null)
            {
                if (modifiedRequest.HealthDamage > 0)
                    _stats.Add(StatType.Health, -modifiedRequest.HealthDamage);

                if (modifiedRequest.PoiseDamage > 0)
                {
                    if (_poiseController != null)
                    {
                        Vector3 attackerPos = request.Source != null ? request.Source.transform.position : transform.position + transform.forward;
                        _poiseController.ApplyPoiseDamage(modifiedRequest.PoiseDamage, attackerPos);
                    }
                    else
                    {
                        _stats.Add(StatType.Poise, -modifiedRequest.PoiseDamage);
                    }
                }
            }
        }
    }
}