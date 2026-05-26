using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Ambient
{
    [RequireComponent(typeof(GroundNavMeshMover))]
    public class RetaliatingBoarController : MonoBehaviour
    {
        private enum BoarState
        {
            Idle,
            Walk,
            Chase,
            Attack,
            Dead
        }

        private const float GroundProbeHeight = 25f;
        private const float GroundProbeDistance = 80f;
        private const float RotationSpeed = 620f;

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int RunState = Animator.StringToHash("Run");
        private static readonly int AttackState = Animator.StringToHash("Attack1");
        private static readonly int HitState = Animator.StringToHash("Hit1");
        private static readonly int DeathState = Animator.StringToHash("Death1");

        [SerializeField] private Transform _player;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private GroundNavMeshMover _mover;
        [SerializeField] private Animator _animator;
        [SerializeField] private Vector3 _homeCenter;
        [SerializeField] private float _wanderRadius = 9f;
        [SerializeField] private float _walkSpeed = 1.1f;
        [SerializeField] private float _runSpeed = 4.8f;
        [SerializeField] private float _attackRange = 1.6f;
        [SerializeField] private float _runChaseDistance = 5.5f;

        private BoarState _state = BoarState.Idle;
        private Vector3 _targetPosition;
        private float _stateTimer;
        private bool _isAggro;
        private float _lastKnownHealth;
        private int _currentStateHash;

        private void Awake()
        {
            ResolveReferences();
            _lastKnownHealth = _health != null ? _health.CurrentHealth : 0f;
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_health != null)
            {
                _health.HealthChanged += OnHealthChanged;
                _health.Died += OnDied;
            }

            if (_meleeAttack != null)
                _meleeAttack.AttackStarted += OnAttackStarted;
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.HealthChanged -= OnHealthChanged;
                _health.Died -= OnDied;
            }

            if (_meleeAttack != null)
                _meleeAttack.AttackStarted -= OnAttackStarted;
        }

        private void Start()
        {
            EnterIdle();
        }

        private void Update()
        {
            if (_state == BoarState.Dead)
                return;

            ResolvePlayer();

            if (_isAggro && _player != null)
                TickAggro();
            else
                TickFriendly();
        }

        public void Configure(
            Transform player,
            EnemyHealth health,
            EnemyMeleeAttack meleeAttack,
            Vector3 homeCenter,
            float wanderRadius,
            float walkSpeed,
            float runSpeed,
            float attackRange)
        {
            _player = player;
            _health = health;
            _meleeAttack = meleeAttack;
            _homeCenter = homeCenter;
            _wanderRadius = Mathf.Max(0.5f, wanderRadius);
            _walkSpeed = Mathf.Max(0.05f, walkSpeed);
            _runSpeed = Mathf.Max(_walkSpeed, runSpeed);
            _attackRange = Mathf.Max(0.1f, attackRange);
            _lastKnownHealth = _health != null ? _health.CurrentHealth : 0f;
            ResolveReferences();
            ConfigureMover(_walkSpeed, 0.15f);
        }

        public static Vector3 SampleGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;

            position.y = 0f;
            return position;
        }

        private void TickFriendly()
        {
            switch (_state)
            {
                case BoarState.Idle:
                    _stateTimer -= Time.deltaTime;
                    if (_stateTimer <= 0f)
                        EnterWalk();
                    break;
                case BoarState.Walk:
                    MoveToward(_targetPosition, _walkSpeed);
                    if ((_mover != null && _mover.HasArrived(0.15f)) || Vector3.Distance(transform.position, _targetPosition) <= 0.15f)
                        EnterIdle();
                    break;
                default:
                    EnterIdle();
                    break;
            }
        }

        private void TickAggro()
        {
            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            RotateToward(toPlayer);

            float distance = toPlayer.magnitude;
            if (distance > _attackRange)
            {
                bool shouldRun = distance >= _runChaseDistance;
                float chaseSpeed = shouldRun ? _runSpeed : _walkSpeed;
                int chaseAnimation = shouldRun ? RunState : WalkState;

                _state = BoarState.Chase;
                PlayState(chaseAnimation, 0.1f);
                MoveToward(SampleGroundNearPlayer(), chaseSpeed);
                return;
            }

            _state = BoarState.Attack;
            PlayState(IdleState, 0.08f);
            if (_mover != null)
                _mover.Stop();
            if (_meleeAttack != null && !_meleeAttack.IsWindingUp)
                _meleeAttack.TryAttack(_player);
        }

        private void EnterIdle()
        {
            _state = BoarState.Idle;
            _stateTimer = UnityEngine.Random.Range(1.4f, 3.5f);
            if (_mover != null)
                _mover.Stop();
            PlayState(IdleState, 0.12f);
        }

        private void EnterWalk()
        {
            _state = BoarState.Walk;
            _targetPosition = RandomGroundPoint();
            ConfigureMover(_walkSpeed, 0.15f);
            PlayState(WalkState, 0.12f);
        }

        private void EnterAggro()
        {
            if (_isAggro)
                return;

            _isAggro = true;
            _state = BoarState.Chase;
            ConfigureMover(_runSpeed, Mathf.Max(0.1f, _attackRange - 0.05f));
            PlayState(HitState, 0.04f);
        }

        private void MoveToward(Vector3 destination, float speed)
        {
            if (_mover == null)
                return;

            if (!_mover.IsReady && !_mover.TryWarpToNearestNavMesh(_wanderRadius))
                return;

            _mover.SetSpeed(speed);
            _mover.TryMoveTo(destination, Mathf.Max(1f, _wanderRadius * 0.35f));

            Vector3 movement = _mover.Velocity;
            if (movement.sqrMagnitude > 0.000001f)
                RotateToward(movement);
        }

        private void RotateToward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.000001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }

        private Vector3 RandomGroundPoint()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * _wanderRadius;
            return SampleNavMeshPosition(_homeCenter + new Vector3(offset.x, 0f, offset.y), _wanderRadius);
        }

        private Vector3 SampleGroundNearPlayer()
        {
            Vector3 origin = _player.position + Vector3.up * GroundProbeHeight;
            var hits = Physics.RaycastAll(origin, Vector3.down, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (var hit in hits)
            {
                if (hit.transform == null)
                    continue;

                if (hit.transform.IsChildOf(_player) || hit.transform.IsChildOf(transform))
                    continue;

                return SampleNavMeshPosition(hit.point, _wanderRadius);
            }

            Vector3 fallback = _player.position;
            fallback.y = transform.position.y;
            return SampleNavMeshPosition(fallback, _wanderRadius);
        }

        private void PlayState(int stateHash, float transitionDuration)
        {
            if (_animator == null || _currentStateHash == stateHash)
                return;

            _currentStateHash = stateHash;
            if (transitionDuration > 0f)
                _animator.CrossFade(stateHash, transitionDuration);
            else
                _animator.Play(stateHash);
        }

        private void OnHealthChanged(EnemyHealth health)
        {
            if (health == null || health.IsDead)
                return;

            if (_lastKnownHealth > 0f && health.CurrentHealth < _lastKnownHealth)
            {
                PlayState(HitState, 0.04f);
                EnterAggro();
            }

            _lastKnownHealth = health.CurrentHealth;
        }

        private void OnAttackStarted(EnemyMeleeAttack attack)
        {
            PlayState(AttackState, 0.04f);
        }

        private void OnDied(EnemyHealth health)
        {
            _state = BoarState.Dead;
            if (_mover != null)
                _mover.Stop();
            PlayState(DeathState, 0.04f);

            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }

        private void ResolveReferences()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_mover == null)
                _mover = GetComponent<GroundNavMeshMover>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator != null)
                _animator.applyRootMotion = false;
        }

        private void ResolvePlayer()
        {
            if (_player != null)
                return;

            var playerFacade = FindFirstObjectByType<PlayerFacade>();
            if (playerFacade != null)
                _player = playerFacade.transform;
        }

        private void ConfigureMover(float speed, float stoppingDistance)
        {
            if (_mover == null)
                _mover = GetComponent<GroundNavMeshMover>();

            if (_mover == null)
                return;

            _mover.Configure(speed, stoppingDistance, 0.55f, 1.15f, 0f, Mathf.Max(10f, speed * 4f), RotationSpeed, 0.18f, 45);
        }

        private static Vector3 SampleNavMeshPosition(Vector3 position, float sampleRadius)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Max(0.5f, sampleRadius), NavMesh.AllAreas))
                return hit.position;

            return SampleGround(position);
        }
    }
}
