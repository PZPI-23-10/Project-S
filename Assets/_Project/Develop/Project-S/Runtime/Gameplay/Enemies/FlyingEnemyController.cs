using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public enum FlyingEnemyState
    {
        Hover,
        Dive,
        Windup,
        Attack,
        Retreat
    }

    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyMeleeAttack))]
    public class FlyingEnemyController : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private EnemyMeleeAttack _meleeAttack;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Transform _target;
        [SerializeField] private float _hoverHeight = 12f;
        [SerializeField] private float _hoverRadius = 24f;
        [SerializeField] private float _diveStopHeight = 1.2f;
        [SerializeField] private float _retreatDistanceThreshold = 0.35f;
        [SerializeField] private float _hoverPointRefreshTime = 5f;
        [SerializeField] private float _preAttackDelay = 1.2f;

        private FlyingEnemyState _state = FlyingEnemyState.Hover;
        private bool _hasAggro;
        private bool _attackStarted;
        private float _preAttackRemaining;
        private Vector3 _hoverOffset;
        private float _nextHoverPointTime;

        public bool HasAggro => _hasAggro;
        public bool IsMoving { get; private set; }
        public FlyingEnemyState CurrentState => _state;
        public bool IsHovering => _state == FlyingEnemyState.Hover;
        public bool IsDiving => _state == FlyingEnemyState.Dive;
        public bool IsWindingUp => _state == FlyingEnemyState.Windup;
        public bool IsAttacking => _state == FlyingEnemyState.Attack;
        public bool IsRetreating => _state == FlyingEnemyState.Retreat;

        private void Awake()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

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
            Vector3 flatToTarget = toTarget;
            flatToTarget.y = 0f;

            float flatDistance = flatToTarget.magnitude;
            UpdateAggro(flatDistance);
            RotateToward(flatToTarget);

            if (!_hasAggro)
                return;

            TickState(flatDistance);
        }

        public void Configure(
            EnemyConfig config,
            Transform target,
            float hoverHeight,
            float hoverRadius,
            float diveStopHeight,
            float retreatDistanceThreshold)
        {
            _config = config;
            _target = target;
            _hoverHeight = Mathf.Max(0.5f, hoverHeight);
            _hoverRadius = Mathf.Max(0f, hoverRadius);
            _diveStopHeight = Mathf.Max(0f, diveStopHeight);
            _retreatDistanceThreshold = Mathf.Max(0.01f, retreatDistanceThreshold);
            PickNewHoverOffset();

            if (_health == null)
                _health = GetComponent<EnemyHealth>();

            if (_meleeAttack == null)
                _meleeAttack = GetComponent<EnemyMeleeAttack>();

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

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
                {
                    _hasAggro = false;
                    _state = FlyingEnemyState.Hover;
                }

                return;
            }

            if (distance <= _config.AggroRange)
                _hasAggro = true;
        }

        private void TickState(float flatDistance)
        {
            Vector3 hoverPosition = GetHoverPosition();
            Vector3 divePosition = GetDivePosition();

            switch (_state)
            {
                case FlyingEnemyState.Hover:
                    MoveToward(hoverPosition);

                    if (Time.time >= _nextHoverPointTime || Vector3.Distance(transform.position, hoverPosition) <= _retreatDistanceThreshold)
                        PickNewHoverOffset();

                    if (_meleeAttack != null && _meleeAttack.CooldownRemaining <= 0f && !_meleeAttack.IsWindingUp)
                        _state = FlyingEnemyState.Dive;

                    break;

                case FlyingEnemyState.Dive:
                    MoveToward(divePosition);

                    if (Vector3.Distance(transform.position, divePosition) <= _retreatDistanceThreshold)
                    {
                        _attackStarted = false;
                        _preAttackRemaining = Mathf.Max(0f, _preAttackDelay);
                        _state = FlyingEnemyState.Windup;
                    }

                    break;

                case FlyingEnemyState.Windup:
                    _preAttackRemaining -= Time.deltaTime;

                    if (_preAttackRemaining <= 0f)
                        _state = FlyingEnemyState.Attack;

                    break;

                case FlyingEnemyState.Attack:
                    if (!_attackStarted)
                    {
                        _attackStarted = _meleeAttack != null && _meleeAttack.TryAttack(_target);
                        if (!_attackStarted)
                            _state = FlyingEnemyState.Retreat;
                    }

                    if (_meleeAttack == null || !_meleeAttack.IsWindingUp)
                    {
                        _attackStarted = false;
                        _state = FlyingEnemyState.Retreat;
                    }

                    break;

                case FlyingEnemyState.Retreat:
                    MoveToward(hoverPosition);

                    if (Vector3.Distance(transform.position, hoverPosition) <= _retreatDistanceThreshold)
                    {
                        PickNewHoverOffset();
                        _state = FlyingEnemyState.Hover;
                    }

                    break;
            }
        }

        private Vector3 GetHoverPosition()
        {
            if (_hoverOffset.sqrMagnitude <= 0.0001f)
                PickNewHoverOffset();

            return _target.position + _hoverOffset;
        }

        private void PickNewHoverOffset()
        {
            Vector2 randomCircle = Random.insideUnitCircle;
            if (randomCircle.sqrMagnitude <= 0.001f)
                randomCircle = Vector2.right;

            Vector2 horizontal = randomCircle.normalized * Random.Range(_hoverRadius * 0.8f, _hoverRadius * 1.35f);
            float height = Random.Range(_hoverHeight * 0.75f, _hoverHeight * 1.35f);
            _hoverOffset = new Vector3(horizontal.x, height, horizontal.y);
            _nextHoverPointTime = Time.time + _hoverPointRefreshTime;
        }

        private Vector3 GetDivePosition()
        {
            Vector3 toEnemy = transform.position - _target.position;
            toEnemy.y = 0f;

            Vector3 approachDirection = toEnemy.sqrMagnitude > 0.0001f
                ? toEnemy.normalized
                : -_target.forward.normalized;

            if (approachDirection.sqrMagnitude <= 0.0001f)
                approachDirection = Vector3.back;

            return _target.position
                + approachDirection * Mathf.Max(0.1f, _config.AttackRange * 0.85f)
                + Vector3.up * _diveStopHeight;
        }

        private void RotateToward(Vector3 flatToTarget)
        {
            if (flatToTarget.sqrMagnitude <= 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(flatToTarget.normalized, Vector3.up);
            float maxDegrees = Mathf.Max(0f, _config.RotationSpeed) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, maxDegrees);
        }

        private void MoveToward(Vector3 destination)
        {
            float step = Mathf.Max(0f, _config.MoveSpeed) * Time.deltaTime;
            Vector3 currentPosition = transform.position;
            transform.position = Vector3.MoveTowards(currentPosition, destination, step);
            IsMoving = step > 0f && (transform.position - currentPosition).sqrMagnitude > 0.000001f;
        }

        private void OnDied(EnemyHealth health)
        {
            IsMoving = false;
            _attackStarted = false;

            var sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                sphereCollider.radius = 0.85f;
                sphereCollider.center = new Vector3(0f, 0.45f, 0f);
            }

            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                _rigidbody.WakeUp();
            }

            enabled = false;
        }
    }
}
