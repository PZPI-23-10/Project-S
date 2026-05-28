using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

// Додаємо в меню створення Unity!
[CreateAssetMenu(fileName = "BoneCrusher", menuName = "Project-S/Abilities/Passives/BoneCrusher")]
public class BoneCrusherPassive : WeaponPassiveData
{
    public override void OnBeforeHit(CombatController attacker, Collider target, ref float poiseDamage, ref List<DamageInstance> damageProfile)
    {
        // Читаємо комбо прямо з Гравця
        if (attacker.ComboStep == 3)
        {
            poiseDamage *= 2f; // Подвоюємо урон
            Debug.Log("<color=red>[Костолом]</color> 3-й удар! Баланс-урон x2!");
        }
    }

    public override void OnAfterHit(CombatController attacker, Collider target, IDamageReceiver receiver)
    {
        // Нічого не робимо
    }
}