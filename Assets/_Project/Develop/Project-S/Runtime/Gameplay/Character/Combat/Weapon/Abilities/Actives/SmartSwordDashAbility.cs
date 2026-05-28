using System.Collections; // Важливо для корутин!
using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

[CreateAssetMenu(fileName = "SmartSwordDash", menuName = "Project-S/Abilities/Actives/SmartSwordDash")]
public class SmartSwordDashAbility : WeaponActiveData
{
    [Header("Налаштування ривка")]
    public float maxDashDistance = 15f;
    public float dashSpeed = 40f;
    public float hitRadius = 2f;
    public float dashTurnSpeed = 90f;

    [Header("Ефекти")]
    [Tooltip("Ефект великого розрізу, який з'явиться в точці удару")]
    public ParticleSystem bigCutEffect;

    [Header("Урон")]
    public float poiseDamage = 150f;
    public List<DamageInstance> dashDamage = new List<DamageInstance>();

    public override bool ResetChargeOnUse => true;

    public override void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim, GameObject weaponModel)
    {
        // Запускаємо процес ривка, який триватиме у часі
        combatCtrl.StartCoroutine(SteerableDashRoutine(combatCtrl));
    }

    private IEnumerator SteerableDashRoutine(CombatController combatCtrl)
    {
        var motor = combatCtrl.GetComponentInParent<Project_S.Runtime.Gameplay.Character.Movement.CharacterMotor>();
        float dashDuration = maxDashDistance / dashSpeed;

        if (motor != null)
        {
            // Штовхаємо гравця
            motor.ForceAttackDash(dashSpeed, dashDuration, dashTurnSpeed);
        }

        float endTime = Time.time + dashDuration;
        HashSet<IDamageReceiver> alreadyHit = new HashSet<IDamageReceiver>();

        // Цей цикл працює КОЖЕН КАДР, поки триває ривок
        while (Time.time < endTime)
        {
            // Малюємо зону удару прямо перед гравцем на ходу
            Vector3 hitCenter = combatCtrl.transform.position + (combatCtrl.transform.forward * 1f) + (Vector3.up * 1f);

            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);
            bool hitSomeone = false;

            foreach (Collider col in hitColliders)
            {
                if (col.transform.root == combatCtrl.transform.root) continue; // Ігноруємо себе

                IDamageReceiver receiver = col.GetComponentInParent<IDamageReceiver>();
                if (receiver != null && !alreadyHit.Contains(receiver))
                {
                    alreadyHit.Add(receiver); // Щоб не вдарити одного ворога двічі

                    // НАНОСИМО УРОН!
                    var request = new DamageRequest(combatCtrl.gameObject, new List<DamageInstance>(dashDamage), poiseDamage, combatCtrl.ActiveWeapon);
                    receiver.ReceiveDamage(request);

                    // Малюємо ефект розрізу там, де зловили ворога
                    if (bigCutEffect != null)
                    {
                        Instantiate(bigCutEffect, col.ClosestPoint(hitCenter), Quaternion.LookRotation(-combatCtrl.transform.forward));
                    }

                    hitSomeone = true;
                }
            }

            // Якщо ми врізалися у ворога, зупиняємо політ! (Як у Демомана)
            if (hitSomeone && motor != null)
            {
                motor.StopAttackDash();
                break; // Зупиняємо корутину перевірок
            }

            // Чекаємо до наступного кадру і повторюємо перевірку
            yield return null;
        }
    }
}