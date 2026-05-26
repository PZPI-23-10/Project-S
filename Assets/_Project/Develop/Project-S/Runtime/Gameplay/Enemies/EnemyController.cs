using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyMeleeAttack))]
    [RequireComponent(typeof(GroundNavMeshMover))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private GroundNavMeshMover _mover;
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

            if (_mover == null)
                _mover = GetComponent<GroundNavMeshMover>();

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

            ConfigureMover();
            EnsureTarget();

            if (_target == null)
                return;

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            UpdateAggro(distance);

            if (!_hasAggro)
            {
                if (_mover != null)
                    _mover.Stop();

                return;
            }

            RotateToward(toTarget);

            bool canAttack = distance <= _config.AttackRange && (_mover == null || _mover.PathStatus != NavMeshPathStatus.PathInvalid);
            if (canAttack)
            {
                if (_mover != null)
                    _mover.Stop();

                if (_meleeAttack != null)
                    _meleeAttack.TryAttack(_target);

                return;
            }

            if (_meleeAttack != null && _meleeAttack.IsWindingUp)
            {
                if (_mover != null)
                    _mover.Stop();

                return;
            }

            MoveToward(_target.position);
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

            ConfigureMover();
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

        private void MoveToward(Vector3 destination)
        {
            if (_mover == null)
                return;

            if (!_mover.IsReady && !_mover.TryWarpToNearestNavMesh(_config.AgentRadius + 2f))
                return;

            _mover.SetSpeed(_config.MoveSpeed);
            _mover.TryMoveTo(destination, _config.AgentRadius + 2f);
            IsMoving = _mover.IsMoving;
        }

        private void ConfigureMover()
        {
            if (_config == null)
                return;

            if (_mover == null)
                _mover = GetComponent<GroundNavMeshMover>();

            if (_mover == null)
                return;

            float stoppingDistance = Mathf.Max(0f, _config.AttackRange - _config.StoppingDistancePadding);
            _mover.Configure(
                _config.MoveSpeed,
                stoppingDistance,
                _config.AgentRadius,
                _config.AgentHeight,
                _config.AgentBaseOffset,
                Mathf.Max(8f, _config.MoveSpeed * 4f),
                _config.RotationSpeed,
                _config.RepathInterval,
                50);
        }

        private void OnDied(EnemyHealth health)
        {
            if (_mover != null)
                _mover.Stop();

            enabled = false;
        }
    }
}
