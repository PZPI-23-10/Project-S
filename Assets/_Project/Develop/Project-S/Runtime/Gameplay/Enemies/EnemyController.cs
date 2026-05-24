using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyMeleeAttack))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private Transform _target;

        private bool _hasAggro;

        public bool HasAggro => _hasAggro;
        public bool IsMoving { get; private set; }

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void Update()
        {
            IsMoving = false;

            if (_health != null && _health.IsDead)
                return;

            if (_config == null)
                return;

            EnsureTarget();

            if (_target == null)
                return;

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            UpdateAggro(distance);

            if (!_hasAggro)
                return;

            RotateToward(toTarget);

            if (distance <= _config.AttackRange)
            {
                if (_meleeAttack != null)
                    _meleeAttack.TryAttack(_target);

                return;
            }

            if (_meleeAttack != null && _meleeAttack.IsWindingUp)
                return;

            MoveToward(toTarget);
        }

        public void Configure(EnemyConfig config, Transform target)
        {
            _config = config;
            _target = target;

            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_health != null)
                _health.Configure(config);

            if (_meleeAttack != null)
                _meleeAttack.Configure(config);
        }

        private void EnsureTarget()
        {
            if (_target != null)
                return;

            var player = FindFirstObjectByType<PlayerFacade>();
            if (player != null)
                _target = player.transform;
        }

        private void UpdateAggro(float distance)
        {
            if (_hasAggro)
            {
                if (distance > _config.LoseTargetRange)
                    _hasAggro = false;

                return;
            }

            if (distance <= _config.AggroRange)
                _hasAggro = true;
        }

        private void RotateToward(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            float maxDegrees = Mathf.Max(0f, _config.RotationSpeed) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxDegrees);
        }

        private void MoveToward(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.0001f)
                return;

            float step = Mathf.Max(0f, _config.MoveSpeed) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, transform.position + toTarget.normalized, step);
            IsMoving = step > 0f;
        }

        private void OnDied(EnemyHealth health)
        {
            enabled = false;
        }
    }
}
