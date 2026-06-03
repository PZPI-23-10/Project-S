using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

[CreateAssetMenu(fileName = "DistanceKeeper", menuName = "Project-S/Abilities/Passives/DistanceKeeper")]
public class DistanceKeeperPassive : WeaponPassiveData
{
    [Header("Налаштування відкидання (Спис)")]
    [Tooltip("Сила фізичного відкидання ворога назад")]
    public float pushForce = 15f;

    [Tooltip("Додатковий урон по балансу, щоб гарантовано перервати атаку ворога (Stagger)")]
    public float interruptPoiseDamage = 50f;

    public override void OnBeforeHit(CombatController attacker, Collider target, ref float poiseDamage, ref List<DamageInstance> damageProfile)
    {
        if (attacker == null) return;

        // Якщо це 3-й удар, накидаємо купу урону по балансу для переривання атаки
        if (attacker.ComboStep == 3)
        {
            poiseDamage += interruptPoiseDamage;
            // Тут не пишемо Debug.Log, щоб не спамити, напишемо його в OnAfterHit
        }
    }

    public override void OnAfterHit(CombatController attacker, Collider target, IDamageReceiver receiver)
    {
        if (attacker == null) return;

        // Після успішного 3-го удару робимо фізичний поштовх!
        if (attacker.ComboStep == 3)
        {
            // Шукаємо фізику на ворогу
            Rigidbody enemyRb = target.GetComponentInParent<Rigidbody>();

            if (enemyRb != null)
            {
                // Вираховуємо напрямок: від Гравця -> до Ворога
                Vector3 pushDirection = target.transform.position - attacker.transform.position;

                // Обнуляємо вісь Y, щоб ворог не летів у космос або під землю, а котився назад
                pushDirection.y = 0;
                pushDirection.Normalize(); // Робимо довжину вектора рівною 1

                // Застосовуємо силу відкидання (Impulse означає різкий удар, а не плавне штовхання)
                enemyRb.AddForce(pushDirection * pushForce, ForceMode.Impulse);

                Debug.Log("<color=cyan>[Дистанція]</color> 3-й удар! Ворога відкинуто назад!");
            }
            else
            {
                // Якщо у ворога немає Rigidbody, ми його просто приголомшуємо (перериваємо)
                Debug.Log("<color=cyan>[Дистанція]</color> 3-й удар! Атаку ворога перервано!");
            }
        }
    }
}