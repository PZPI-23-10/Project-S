using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Navigation;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Ambient;
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
        [SerializeField] private EnemyRangedAttack _rangedAttack;
        [SerializeField] private GroundNavMeshMover _mover;
        [SerializeField] private Transform _target;
        [SerializeField] private bool _wanderWhenIdle;
        [SerializeField] private Vector3 _homeCenter;
        [SerializeField] private float _homeRadius = 10f;
        [SerializeField] private float _idleWanderMinDelay = 1.5f;
        [SerializeField] private float _idleWanderMaxDelay = 4f;

        private bool _hasAggro;
        private float _stunRemaining;
        private float _idleWanderTimer;
        private Vector3 _wanderDestination;
        private bool _hasWanderDestination;

        public bool HasAggro => _hasAggro;
        public bool IsMoving { get; private set; }
        public bool IsStunned => _stunRemaining > 0f;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_rangedAttack == null)
                _rangedAttack = GetComponent<EnemyRangedAttack>();

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

            if (_stunRemaining > 0f)
            {
                _stunRemaining = Mathf.Max(0f, _stunRemaining - Time.deltaTime);

                if (_mover != null)
                    _mover.Stop();

                return;
            }

            ConfigureMover();
            EnsureTarget();

            if (_target == null)
            {
                TickIdleWander();
                return;
            }

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            UpdateAggro(distance);

            if (!_hasAggro)
            {
                TickIdleWander();
                return;
            }

            RotateToward(toTarget);

            if (TryHandleRangedCombat(distance, toTarget))
                return;

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

            if (_rangedAttack == null)
                _rangedAttack = GetComponent<EnemyRangedAttack>();

            if (_health != null)
                _health.Configure(config);

            if (_meleeAttack != null)
                _meleeAttack.Configure(config);

            if (_rangedAttack != null)
                _rangedAttack.Configure(config);

            ConfigureMover();
        }

        public void ConfigureHomeArea(Vector3 center, float radius, bool wanderWhenIdle)
        {
            _homeCenter = center;
            _homeRadius = Mathf.Max(0.5f, radius);
            _wanderWhenIdle = wanderWhenIdle;
            _idleWanderTimer = Random.Range(_idleWanderMinDelay, _idleWanderMaxDelay);
            _hasWanderDestination = false;
        }

        public void StunFor(float duration)
        {
            _stunRemaining = Mathf.Max(_stunRemaining, Mathf.Max(0f, duration));
            IsMoving = false;

            if (_mover != null)
                _mover.Stop();

            if (_meleeAttack != null)
                _meleeAttack.CancelAttack();

            if (_rangedAttack != null)
                _rangedAttack.CancelAttack();
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
                {
                    _hasAggro = false;
                    _idleWanderTimer = Random.Range(0.25f, 1f);
                    _hasWanderDestination = false;
                }

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

        private void TickIdleWander()
        {
            if (!_wanderWhenIdle || _config == null)
            {
                if (_mover != null)
                    _mover.Stop();

                return;
            }

            if (_meleeAttack != null && _meleeAttack.IsWindingUp)
            {
                if (_mover != null)
                    _mover.Stop();

                return;
            }

            ConfigureMover();

            if (_hasWanderDestination)
            {
                MoveToward(_wanderDestination);
                RotateTowardIdleMovement(_wanderDestination);
                if (_mover == null || _mover.HasArrived(0.35f) || HorizontalDistance(transform.position, _wanderDestination) <= 0.35f)
                {
                    _hasWanderDestination = false;
                    _idleWanderTimer = Random.Range(_idleWanderMinDelay, _idleWanderMaxDelay);
                }

                return;
            }

            if (_mover != null)
                _mover.Stop();

            _idleWanderTimer -= Time.deltaTime;
            if (_idleWanderTimer > 0f)
                return;

            _wanderDestination = RandomHomePoint();
            _hasWanderDestination = true;
        }

        private Vector3 RandomHomePoint()
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0.5f, _homeRadius);
            Vector3 target = _homeCenter + new Vector3(offset.x, 0f, offset.y);
            return GroundPositionSampler.SampleNavMeshNearGround(target, Mathf.Max(1f, _homeRadius));
        }

        private void RotateTowardIdleMovement(Vector3 destination)
        {
            Vector3 direction = Vector3.zero;

            if (_mover != null && _mover.Velocity.sqrMagnitude > 0.0001f)
                direction = _mover.Velocity;
            else
                direction = destination - transform.position;

            direction.y = 0f;
            RotateToward(direction);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private bool TryHandleRangedCombat(float distance, Vector3 toTarget)
        {
            if (_config == null || !_config.UseRangedAttack || _rangedAttack == null)
                return false;

            if (_rangedAttack.IsWindingUp)
            {
                if (_mover != null)
                    _mover.Stop();

                return true;
            }

            float attackRange = Mathf.Max(0f, _config.RangedAttackRange);
            float retreatDistance = Mathf.Max(0f, _config.RangedRetreatDistance);
            float preferredDistance = Mathf.Max(retreatDistance, _config.RangedPreferredDistance);

            if (distance < retreatDistance && toTarget.sqrMagnitude > 0.0001f)
            {
                Vector3 away = transform.position - toTarget.normalized * (preferredDistance - distance);
                MoveToward(away);
                return true;
            }

            if (distance <= attackRange)
            {
                if (_mover != null)
                    _mover.Stop();

                _rangedAttack.TryAttack(_target);
                return true;
            }

            MoveToward(_target.position);
            return true;
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

            var corpseHarvest = GetComponent<AnimalCorpseHarvest>();
            foreach (var collider in GetComponents<Collider>())
            {
                if (corpseHarvest != null && collider.gameObject == gameObject)
                {
                    collider.isTrigger = true;
                    continue;
                }

                collider.enabled = false;
            }

            var agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
                agent.enabled = false;

            enabled = false;

            MaceRageBuff playerRage = FindFirstObjectByType<MaceRageBuff>();
            if (playerRage != null)
            {
                playerRage.OnEnemyKilled();
            }
        }
    }
}
