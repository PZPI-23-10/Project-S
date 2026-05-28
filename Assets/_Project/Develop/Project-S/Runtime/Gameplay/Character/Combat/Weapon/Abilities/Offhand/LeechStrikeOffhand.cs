using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

// Цей скрипт вішається прямо на ПРЕФАБ Мізерикорда (для лівої руки)
public class LeechStrikeOffhand : MonoBehaviour, IOffhandAbility
{
    [Header("Налаштування Укусу")]
    public float hitRadius = 0.5f;
    public float hitDistance = 1.5f;

    [Header("Урон")]
    // Тепер ти можеш в Інспекторі налаштувати тип урону (Pierce, Magic тощо) і кількість!
    public List<DamageInstance> damageProfile = new List<DamageInstance>();
    public float poiseDamage = 10f;

    [Header("Вампіризм (Нагороди)")]
    public float healthRestore = 20f;
    public float staminaRestore = 30f;

    [Header("Ефекти")]
    public ParticleSystem bloodDrainEffect;

    private Animator _myAnimator;

    private void Awake()
    {
        _myAnimator = GetComponent<Animator>();
    }

    public void ExecuteOffhandAbility(CombatController combatCtrl, Animator rightHandAnim)
    {
        if (_myAnimator != null)
        {
            _myAnimator.SetTrigger("Stab");
        }

        Vector3 startPos = combatCtrl.transform.position + Vector3.up * 1f;
        Vector3 direction = combatCtrl.transform.forward;

        if (Physics.SphereCast(startPos, hitRadius, direction, out RaycastHit hit, hitDistance))
        {
            IDamageReceiver targetEnemy = hit.collider.GetComponentInParent<IDamageReceiver>();

            if (targetEnemy != null && hit.collider.transform.root != combatCtrl.transform.root)
            {
                if (bloodDrainEffect != null)
                {
                    bloodDrainEffect.Play();
                }

                // Створюємо правильний DamageRequest, як у твоєму Молоті!
                var request = new DamageRequest(
                    combatCtrl.gameObject, // Хто атакує
                    damageProfile,         // Список урону (тип + кількість)
                    poiseDamage,           // Урон по балансу
                    null                   // Дані зброї (можна передати null для лівої руки)
                );

                targetEnemy.ReceiveDamage(request);

                // ==========================================
                // ВІДНОВЛЕННЯ ХП ТА СТАМІНИ
                // Розкоментуй і впиши свої функції, коли будеш готовий
                // ==========================================

                CharacterStats stats = combatCtrl.GetComponent<CharacterStats>();
                if (stats != null)
                {
                    // 1. Відновлюємо Стаміну
                    float currentStamina = stats.Get(StatType.Stamina);
                    float maxStamina = stats.Get(StatType.MaxStamina);
                    stats.Set(StatType.Stamina, Mathf.Min(maxStamina, currentStamina + staminaRestore));

                    // 2. Відновлюємо Здоров'я (припускаю, що у тебе є StatType.Health)
                    float currentHealth = stats.Get(StatType.Health);
                    float maxHealth = stats.Get(StatType.MaxHealth);
                    stats.Set(StatType.Health, Mathf.Min(maxHealth, currentHealth + healthRestore));

                    Debug.Log($"<color=green>[Вампіризм]</color> Відновлено {healthRestore} ХП та {staminaRestore} Стаміни!");
                }

                Debug.Log($"<color=red>[Мізерикорд]</color> Викачали життя з {hit.collider.name}!");
            }
        }
    }
}