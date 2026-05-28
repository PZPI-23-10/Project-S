using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public interface IOffhandAbility
    {
        void ExecuteOffhandAbility(CombatController combatCtrl, Animator anim);
    }
}