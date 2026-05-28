using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Character.Movement;

[CreateAssetMenu(fileName = "MisericordeExecution", menuName = "Project-S/Abilities/Actives/MisericordeExecution")]
public class MisericordeExecutionAbility : WeaponActiveData
{
    [Header("Налаштування мікро-кроку")]
    public float stepDistance = 1.5f;    // Маленький крок уперед (в метрах)
    public float stepSpeed = 15f;        // Комфортна швидкість підтягування
    public float hitRadius = 1.5f;       // Зона ураження

    [Header("Екзекуція (Удар по зламаному балансу)")]
    public float executionMultiplier = 4f;
    public ParticleSystem executionEffect;

    [Header("Базовий урон")]
    public float basePoiseDamage = 10f;
    public List<DamageInstance> baseDamage = new List<DamageInstance>();

    public override bool ResetChargeOnUse => true;

    public override void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim, GameObject weaponModel)
    {
        Vector3 startPos = combatCtrl.transform.position + (Vector3.up * 1f);
        Vector3 direction = combatCtrl.transform.forward;

        // 1. РОБИМО МІКРО-КРОК УПЕРЕД
        var motor = combatCtrl.GetComponentInParent<CharacterMotor>();
        if (motor != null)
        {
            float stepDuration = stepDistance / stepSpeed;
            // Робимо дуже короткий поштовх без можливості керувати ним (turnSpeed = 0)
            motor.ForceAttackDash(stepSpeed, stepDuration, 0f);
        }

        // 2. ШУКАЄМО ЦІЛЬ І НАНОСИМО УРОН
        // Перевіряємо зону прямо перед собою на дистанцію нашого мікро-кроку
        if (Physics.SphereCast(startPos, hitRadius, direction, out RaycastHit hit, stepDistance + 0.5f))
        {
            if (hit.collider.transform.root != combatCtrl.transform.root)
            {
                IDamageReceiver targetEnemy = hit.collider.GetComponentInParent<IDamageReceiver>();

                if (targetEnemy != null)
                {
                    float finalMultiplier = 1f;
                    bool isExecution = false;

                    PoiseController enemyPoise = hit.collider.GetComponentInParent<PoiseController>();

                    if (enemyPoise != null && enemyPoise.IsBroken)
                    {
                        isExecution = true;
                    }
                    else if (Random.value <= 0.3f) // Тимчасовий шанс для тестів
                    {
                        isExecution = true;
                    }

                    if (isExecution)
                    {
                        finalMultiplier = executionMultiplier;
                        Debug.Log($"<color=red>[Мізерикорд]</color> ФАТАЛЬНИЙ УДАР! Множник: x{finalMultiplier}");

                        if (executionEffect != null)
                        {
                            Instantiate(executionEffect, hit.point, Quaternion.LookRotation(-direction));
                        }
                    }

                    List<DamageInstance> finalDamage = new List<DamageInstance>();
                    foreach (var dmg in baseDamage)
                    {
                        finalDamage.Add(new DamageInstance { Type = dmg.Type, Amount = dmg.Amount * finalMultiplier });
                    }

                    var request = new DamageRequest(combatCtrl.gameObject, finalDamage, basePoiseDamage, combatCtrl.ActiveWeapon);
                    targetEnemy.ReceiveDamage(request);
                }
            }
        }
    }
}