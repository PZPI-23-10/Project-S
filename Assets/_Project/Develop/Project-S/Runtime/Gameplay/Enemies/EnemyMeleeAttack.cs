using System;
using Project_S.Runtime.Gameplay.Character.Combat;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public class EnemyMeleeAttack : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private LayerMask _targetLayers = ~0;

        private Transform _pendingTarget;
        private float _cooldownRemaining;
        private float _windupRemaining;
        private bool _isWindingUp;

        public bool IsWindingUp => _isWindingUp;
        public float CooldownRemaining => _cooldownRemaining;
        public float WindupDuration => _config != null ? Mathf.Max(0.01f, _config.AttackWindup) : 0.45f;

        public event Action<EnemyMeleeAttack> AttackStarted;
        public event Action<EnemyMeleeAttack> AttackResolved;

        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(EnemyConfig config)
        {
            _config = config;
        }

        public bool TryAttack(Transform target)
        {
            if (target == null || _config == null || _isWindingUp || _cooldownRemaining > 0f)
                return false;

            _pendingTarget = target;
            _windupRemaining = _config.AttackWindup;
            _isWindingUp = true;
            AttackStarted?.Invoke(this);

            if (_windupRemaining <= 0f)
                ResolveAttack();

            return true;
        }

        public void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            if (_cooldownRemaining > 0f)
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);

            if (!_isWindingUp)
                return;

            _windupRemaining -= deltaTime;
            if (_windupRemaining <= 0f)
                ResolveAttack();
        }

        private void ResolveAttack()
        {
            _isWindingUp = false;
            _cooldownRemaining = _config != null ? _config.AttackCooldown : 0f;
            AttackResolved?.Invoke(this);

            if (_config == null || _pendingTarget == null)
                return;

            var origin = _attackOrigin != null ? _attackOrigin : transform;
            if (TryDamagePendingTarget(origin))
                return;

            Vector3 center = origin.position + origin.forward * Mathf.Max(0f, _config.AttackRange * 0.5f);
            float radius = Mathf.Max(0.01f, _config.AttackRadius);
            var hits = Physics.OverlapSphere(center, radius, _targetLayers, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root)
                    continue;

                if (!hit.transform.IsChildOf(_pendingTarget) && hit.transform.root != _pendingTarget.root)
                    continue;

                var receiver = hit.GetComponentInParent<IDamageReceiver>();
                if (receiver == null)
                    continue;

                var request = new DamageRequest(
                    gameObject,
                    _config.HealthDamage,
                    _config.PoiseDamage,
                    _config.DamageType);

                receiver.ReceiveDamage(request);
                return;
            }
        }

        private bool TryDamagePendingTarget(Transform origin)
        {
            Vector3 toTarget = _pendingTarget.position - origin.position;
            toTarget.y = 0f;

            float allowedDistance = Mathf.Max(0f, _config.AttackRange) + Mathf.Max(0f, _config.AttackRadius);
            if (toTarget.sqrMagnitude > allowedDistance * allowedDistance)
                return false;

            var receiver = _pendingTarget.GetComponentInParent<IDamageReceiver>();
            if (receiver == null)
                receiver = _pendingTarget.GetComponentInChildren<IDamageReceiver>();

            if (receiver == null)
                return false;

            var request = new DamageRequest(
                gameObject,
                _config.HealthDamage,
                _config.PoiseDamage,
                _config.DamageType);

            receiver.ReceiveDamage(request);
            return true;
        }
    }
}
