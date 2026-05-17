using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class BlockController : MonoBehaviour
    {
        [SerializeField] private CombatController _combatController;

        private float _blockStartedAt;

        public bool IsBlocking { get; private set; }

        // ÷ей метод тепер викликаЇтьс€ з CombatController, коли ти тиснеш ѕ ћ
        public void StartBlock()
        {
            IsBlocking = true;
            _blockStartedAt = Time.time;

            // ƒл€ тесту на капсул≥ (зм≥нюЇ кол≥р)
            if (GetComponentInChildren<Renderer>() != null)
                GetComponentInChildren<Renderer>().material.color = Color.blue;
        }

        // ¬икликаЇтьс€ з CombatController, коли в≥дпускаЇш ѕ ћ
        public void StopBlock()
        {
            IsBlocking = false;

            if (GetComponentInChildren<Renderer>() != null)
                GetComponentInChildren<Renderer>().material.color = Color.white;
        }

        public bool IsParryWindow()
        {
            // якщо немаЇ зброњ - парирувати не можна
            if (!IsBlocking || _combatController == null || _combatController.CurrentWeapon == null)
                return false;

            return Time.time - _blockStartedAt <= _combatController.CurrentWeapon.ParryWindow;
        }

        public DamageRequest ModifyIncomingDamage(DamageRequest request)
        {
            if (!IsBlocking || _combatController == null || _combatController.CurrentWeapon == null)
                return request;

            var weapon = _combatController.CurrentWeapon;

            // 1. ”сп≥шне парируванн€
            if (IsParryWindow())
            {
                Debug.Log("<color=green>[Ѕлок]</color> ≤ƒ≈јЋ№Ќ≈ ѕј–»–”¬јЌЌя!");
                // “ут ми пот≥м додамо поверненн€ стам≥ни (weapon.ParryStaminaReward)
                return new DamageRequest(request.Source, 0f, 0f, request.Type, request.Weapon);
            }

            // 2. «вичайний блок
            // BlockMitigation: 0.5 означаЇ, що блокуЇтьс€ 50% урону. ќтже, пропускаЇмо (1 - 0.5) = 50%
            // якщо BlockMitigation = 0.8 (ўит), пропускаЇмо (1 - 0.8) = 20% урону
            float damageMultiplier = 1f - weapon.BlockMitigation;

            return new DamageRequest(
                request.Source,
                request.HealthDamage * damageMultiplier,
                request.PoiseDamage * damageMultiplier,
                request.Type,
                request.Weapon);
        }
    }
}