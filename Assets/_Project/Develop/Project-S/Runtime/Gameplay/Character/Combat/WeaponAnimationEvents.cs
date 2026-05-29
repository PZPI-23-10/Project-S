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
            _combatController = GetComponentInParent<CombatController>();
        }

        public void PlaySwingSound()
        {
            if (_combatController != null)
                _combatController.AnimEvent_PlaySwingSound();
        }

        public void ExecuteHeavyAbility()
        {
            if (_combatController != null) 
                _combatController.AnimEvent_ExecuteHeavyAbility();
        }

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

        public void OpenComboWindow() 
        { 
            if (_combatController != null) _combatController.AnimEvent_OpenComboWindow(); 
        }
        public void TriggerNextCombo() 
        { 
            if (_combatController != null) _combatController.AnimEvent_TriggerNextCombo();
        }
    }
}