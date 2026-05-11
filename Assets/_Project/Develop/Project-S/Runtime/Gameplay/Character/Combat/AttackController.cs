using System;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class AttackController : MonoBehaviour
    {
        [SerializeField] private CombatConfig _config;
        [SerializeField] private StaminaController _stamina;

        private float _nextAttackTime;
        private int _comboIndex;

        public event Action<int, bool> AttackStarted;
        public event Action ExhaustedAttackStarted;

        public void Tick(PlayerInputSnapshot input)
        {
            if (Time.time < _nextAttackTime)
                return;

            if (input.HeavyAttackPressed)
            {
                StartAttack(true);
                return;
            }

            if (input.LightAttackPressed)
                StartAttack(false);
        }

        private void StartAttack(bool heavy)
        {
            var staminaCost = heavy ? _config.HeavyAttackStaminaCost : _config.LightAttackStaminaCost;
            var cooldown = heavy ? _config.HeavyAttackCooldown : _config.LightAttackCooldown;

            if (!_stamina.Spend(staminaCost))
            {
                ExhaustedAttackStarted?.Invoke();
                _nextAttackTime = Time.time + cooldown;
                return;
            }

            _comboIndex = (_comboIndex + 1) % 3;
            AttackStarted?.Invoke(_comboIndex, heavy);
            _nextAttackTime = Time.time + cooldown;
        }
    }
}
