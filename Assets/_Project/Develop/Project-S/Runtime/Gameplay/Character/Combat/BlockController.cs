using Project_S.Runtime.Gameplay.Character.Input;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class BlockController : MonoBehaviour
    {
        [SerializeField] private CombatConfig _config;

        private bool _wasBlocking;
        private float _blockStartedAt;

        public bool IsBlocking { get; private set; }
        public bool IsParryWindow => IsBlocking && Time.time - _blockStartedAt <= _config.ParryWindow;

        public void Tick(PlayerInputSnapshot input)
        {
            IsBlocking = input.BlockHeld;

            if (IsBlocking && !_wasBlocking)
                _blockStartedAt = Time.time;

            _wasBlocking = IsBlocking;
        }

        public DamageRequest ModifyIncomingDamage(DamageRequest request)
        {
            if (!IsBlocking)
                return request;

            if (IsParryWindow)
                return new DamageRequest(request.Source, 0f, 0f, request.Type);

            return new DamageRequest(
                request.Source,
                request.HealthDamage * _config.BlockDamageMultiplier,
                request.PoiseDamage * _config.BlockDamageMultiplier,
                request.Type);
        }
    }
}
