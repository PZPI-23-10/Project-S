using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Enemies;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Ambient
{
    public class NeutralAnimalController : MonoBehaviour
    {
        private enum AnimalState
        {
            Idle,
            Walk,
            Eat,
            Flee,
            Dead
        }

        private const float GroundProbeHeight = 25f;
        private const float GroundProbeDistance = 80f;
        private const float RotationSpeed = 540f;
        private const float FleeDistanceMultiplier = 1.35f;

        private static readonly int StateHash = Animator.StringToHash("State");
        private static readonly int VertHash = Animator.StringToHash("Vert");

        [SerializeField] private Transform _player;
        [SerializeField] private EnemyHealth _health;
        [SerializeField] private Animator _animator;
        [SerializeField] private Vector3 _herdCenter;
        [SerializeField] private float _herdRadius = 8f;
        [SerializeField] private float _walkSpeed = 1f;
        [SerializeField] private float _runSpeed = 4f;
        [SerializeField] private float _scareRadius = 6f;

        private AnimalState _state = AnimalState.Idle;
        private Vector3 _targetPosition;
        private float _stateTimer;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void Start()
        {
            EnterIdle();
        }

        private void Update()
        {
            if (_state == AnimalState.Dead)
                return;

            ResolvePlayer();

            if (_player != null && _state != AnimalState.Flee && HorizontalDistanceToPlayer() <= _scareRadius)
                EnterFlee();

            switch (_state)
            {
                case AnimalState.Idle:
                    TickIdle();
                    break;
                case AnimalState.Walk:
                    TickWalk();
                    break;
                case AnimalState.Eat:
                    TickEat();
                    break;
                case AnimalState.Flee:
                    TickFlee();
                    break;
            }
        }

        public void Configure(
            Transform player,
            EnemyHealth health,
            Vector3 herdCenter,
            float herdRadius,
            float walkSpeed,
            float runSpeed,
            float scareRadius)
        {
            _player = player;
            _health = health;
            _herdCenter = herdCenter;
            _herdRadius = Mathf.Max(0.5f, herdRadius);
            _walkSpeed = Mathf.Max(0.05f, walkSpeed);
            _runSpeed = Mathf.Max(_walkSpeed, runSpeed);
            _scareRadius = Mathf.Max(0.1f, scareRadius);
            ResolveReferences();
        }

        public static Vector3 SampleGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;

            position.y = 0f;
            return position;
        }

        private void TickIdle()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer > 0f)
                return;

            if (Random.value < 0.35f)
                EnterEat();
            else
                EnterWalk();
        }

        private void TickWalk()
        {
            MoveToward(_targetPosition, _walkSpeed);

            if (Vector3.Distance(transform.position, _targetPosition) <= 0.15f)
                EnterIdle();
        }

        private void TickEat()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                EnterIdle();
        }

        private void TickFlee()
        {
            MoveToward(_targetPosition, _runSpeed);

            if (Vector3.Distance(transform.position, _targetPosition) <= 0.25f)
            {
                _herdCenter = SampleGround(transform.position);
                EnterIdle();
            }
        }

        private void EnterIdle()
        {
            _state = AnimalState.Idle;
            _stateTimer = Random.Range(1.2f, 3.5f);
            SetAnimator(0f, 0f);
        }

        private void EnterWalk()
        {
            _state = AnimalState.Walk;
            _targetPosition = RandomGroundPoint();
            SetAnimator(0f, 1f);
        }

        private void EnterEat()
        {
            _state = AnimalState.Eat;
            _stateTimer = Random.Range(1.8f, 4f);
            SetAnimator(1f, 0f);
        }

        private void EnterFlee()
        {
            _state = AnimalState.Flee;

            Vector3 away = transform.position - (_player != null ? _player.position : _herdCenter);
            away.y = 0f;
            if (away.sqrMagnitude <= 0.001f)
                away = Random.insideUnitSphere.WithY(0f);

            if (away.sqrMagnitude <= 0.001f)
                away = transform.forward;

            float fleeDistance = Mathf.Max(_scareRadius * FleeDistanceMultiplier, _herdRadius * 0.6f);
            _targetPosition = SampleGround(transform.position + away.normalized * fleeDistance);
            SetAnimator(1f, 1f);
        }

        private void MoveToward(Vector3 destination, float speed)
        {
            Vector3 currentPosition = transform.position;
            transform.position = Vector3.MoveTowards(currentPosition, destination, speed * Time.deltaTime);

            Vector3 movement = transform.position - currentPosition;
            if (movement.sqrMagnitude > 0.000001f)
                RotateToward(movement);
        }

        private void RotateToward(Vector3 movement)
        {
            movement.y = 0f;
            if (movement.sqrMagnitude <= 0.000001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                RotationSpeed * Time.deltaTime);
        }

        private Vector3 RandomGroundPoint()
        {
            Vector2 offset = Random.insideUnitCircle * _herdRadius;
            return SampleGround(_herdCenter + new Vector3(offset.x, 0f, offset.y));
        }

        private void SetAnimator(float state, float vert)
        {
            if (_animator == null)
                return;

            _animator.SetFloat(StateHash, state);
            _animator.SetFloat(VertHash, vert);
        }

        private float HorizontalDistanceToPlayer()
        {
            if (_player == null)
                return float.MaxValue;

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.magnitude;
        }

        private void OnDied(EnemyHealth health)
        {
            _state = AnimalState.Dead;
            SetAnimator(0f, 0f);

            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var rigidbody = GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
            }
        }

        private void ResolveReferences()
        {
            if (_health == null)
                _health = GetComponent<EnemyHealth>();

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
    }
}
