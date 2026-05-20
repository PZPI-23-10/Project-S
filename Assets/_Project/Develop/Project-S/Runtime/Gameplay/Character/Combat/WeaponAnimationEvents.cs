using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    // Цей скрипт має висіти ТАМ САМО, де висить Animator зброї!
    public class WeaponAnimationEvents : MonoBehaviour
    {
        private CombatController _combatController;

        private void Start()
        {
            // Шукаємо контролер боївки вище по ієрархії (на самому Гравці)
            _combatController = GetComponentInParent<CombatController>();
        }

        // Ці методи тепер побачить вікно Animation
        public void StartHitbox()
        {
            if (_combatController != null)
                _combatController.AnimEvent_StartHitbox();
        }

        public void StopHitbox()
        {
            if (_combatController != null)
                _combatController.AnimEvent_StopHitbox();
        }
    }
}