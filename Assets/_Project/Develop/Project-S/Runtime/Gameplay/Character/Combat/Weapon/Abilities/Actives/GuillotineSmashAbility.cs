using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

[CreateAssetMenu(fileName = "GuillotineSmash", menuName = "Project-S/Abilities/Actives/GuillotineSmash")]
public class GuillotineSmashAbility : WeaponActiveData
{
    [Header("Налаштування 'Казні'")]
    public float hitRadius = 2.5f;       // Наскільки широкий удар
    public float forwardOffset = 1.5f;   // Наскільки далеко перед гравцем він б'є (щоб не бити під себе)

    [Header("Ефекти")]
    [Tooltip("Ефект удару об землю (пил, тріщини, іскри)")]
    public ParticleSystem smashEffect;

    [Header("Урон")]
    public float poiseDamage = 300f; // Збиває з ніг майже будь-кого
    public List<DamageInstance> massiveDamage = new List<DamageInstance>();

    public override bool ResetChargeOnUse => true;

    public override void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim, GameObject weaponModel)
    {
        // 1. Знаходимо епіцентр удару (на землі прямо перед гравцем)
        Vector3 impactPoint = combatCtrl.transform.position + (combatCtrl.transform.forward * forwardOffset);

        // 2. Створюємо крутий ефект удару об землю
        if (smashEffect != null)
        {
            Instantiate(smashEffect, impactPoint, Quaternion.identity);
        }

        // 3. Шукаємо всіх нещасних, які опинилися в зоні ураження
        Collider[] hitColliders = Physics.OverlapSphere(impactPoint, hitRadius);
        HashSet<IDamageReceiver> hitEnemies = new HashSet<IDamageReceiver>();
        bool hitSomeone = false;

        foreach (var col in hitColliders)
        {
            if (col.transform.root == combatCtrl.transform.root) continue; // Себе не б'ємо

            IDamageReceiver receiver = col.GetComponentInParent<IDamageReceiver>();
            if (receiver != null && !hitEnemies.Contains(receiver))
            {
                hitEnemies.Add(receiver);

                // ВІДПРАВЛЯЄМО КОЛОСАЛЬНИЙ УРОН!
                var request = new DamageRequest(
                    combatCtrl.gameObject,
                    new List<DamageInstance>(massiveDamage),
                    poiseDamage,
                    combatCtrl.ActiveWeapon
                );

                receiver.ReceiveDamage(request);
                hitSomeone = true;
            }
        }

        if (hitSomeone)
        {
            Debug.Log("<color=red>[Казнь]</color> Ворога втиснуто в землю потужним ударом!");
        }

        // Візуалізація зони удару в редакторі (щоб ти міг налаштувати радіус)
        Debug.DrawRay(impactPoint, Vector3.up * 2f, Color.red, 2f);
    }
}