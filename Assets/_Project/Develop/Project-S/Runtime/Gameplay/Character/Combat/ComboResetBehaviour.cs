using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class ComboResetBehaviour : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            CombatController combat = animator.GetComponentInParent<CombatController>();
            if (combat != null)
            {
                combat.ForceResetToIdle();
            }
        }
    }
}