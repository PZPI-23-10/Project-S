using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public abstract class WeaponActiveData : ScriptableObject
    {
        public abstract void ExecuteHeavyAbility(CombatController combatCtrl, Animator anim, GameObject weaponModel);

        // ÄÎÄÀªÌÎ ÎÑÜ ÖÅÉ ĞßÄÎÊ:
        public virtual bool ResetChargeOnUse => true;
    }
}