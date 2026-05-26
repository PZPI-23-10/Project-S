using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Stats;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public interface IWeaponPassive
    {
        void OnBeforeHit(CombatController attacker, Collider target, ref float poiseDamage, ref List<DamageInstance> damageProfile);
        void OnAfterHit(CombatController attacker, Collider target, IDamageReceiver receiver);
    }

    public interface IWeaponActiveAbility
    {
        void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim);
    }

    public interface IOffhandAbility
    {
        void ExecuteOffhandAbility(CombatController combatCtrl, Animator anim);
    }
}