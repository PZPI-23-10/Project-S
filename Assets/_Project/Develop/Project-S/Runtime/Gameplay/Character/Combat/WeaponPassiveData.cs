using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Stats;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    // Тепер це ScriptableObject!
    public abstract class WeaponPassiveData : ScriptableObject
    {
        public abstract void OnBeforeHit(CombatController attacker, Collider target, ref float poiseDamage, ref List<DamageInstance> damageProfile);

        public abstract void OnAfterHit(CombatController attacker, Collider target, IDamageReceiver receiver);
    }
}