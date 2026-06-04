using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Ambient
{
    [RequireComponent(typeof(GroundNavMeshMover))]
    public class SparrowAmbientController : MonoBehaviour
    {
        private enum SparrowState
        {
            GroundIdle,
            GroundWalk,
            Peck,
            FlyAway,
            Return,
            Landing
        }

        private const float RotationSpeed = 540f;
        private const float LandingHeight = 0.25f;
        private const float MinFlyHeight = 4f;
        private const float MaxFlyHeight = 7f;
        private const float GroundArrivalDistance = 0.12f;
        private const float AgentRadius = 0.15f;
        private const float AgentHeight = 0.35f;
        private const float AgentBaseOffset = 0f;
        private const float RepathInterval = 0.25f;
        private const int AvoidancePriority = 70;

        private static readonly int FlyState = Animator.StringToHash("Fly");
        private static readonly int WalkState = Animator.StringToHash("Walk");
        private static readonly int EatState = Animator.StringToHash("Eat");
        private static readonly int IdleAState = Animator.StringToHash("Idle_A");
        private static readonly int IdleBState = Animator.StringToHash("Idle_B");
        private static readonly int IdleCState = Animator.StringToHash("Idle_C");

        [SerializeField] private Transform _player;
        [SerializeField] private Animator _animator;
        [SerializeField] private GroundNavMeshMover _mover;
        [SerializeField] private Vector3 _flockCenter;
        [SerializeField] private float _flockRadius = 6f;
        [SerializeField] private float _groundMoveSpeed = 0.8f;
        [SerializeField] private float _flyMoveSpeed = 5.5f;
        [SerializeField] private float _scareRadius = 5f;
        [SerializeField] private float _minReturnDelay = 6f;
        [SerializeField] private float _maxReturnDelay = 10f;

        private SparrowState _state = SparrowState.GroundIdle;
        private Vector3 _targetPosition;
        private Vector3 _landingPosition;
        private float _stateTimer;
        private int _currentAnimationHash;
        private bool _hasConfiguredFlockCenter;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (!_hasConfiguredFlockCenter)
                _flockCenter = SampleGround(transform.position);

            ConfigureMover();
            TryPlaceOnNavMesh();
            EnterGroundIdle();
        }

        private void Update()
        {
            ResolvePlayer();

            if (_player != null && IsGroundedState() && HorizontalDistanceToPlayer() <= _scareRadius)
                EnterFlyAway();

            switch (_state)
            {
                case SparrowState.GroundIdle:
                    TickGroundIdle();
                    break;
                case SparrowState.GroundWalk:
                    TickGroundWalk();
                    break;
                case SparrowState.Peck:
                    TickPeck();
                    break;
                case SparrowState.FlyAway:
                    TickFlyAway();
                    break;
                case SparrowState.Return:
                    TickReturn();
                    break;
                case SparrowState.Landing:
                    TickLanding();
                    break;
            }
        }

        public void Configure(
            Transform player,
            Vector3 flockCenter,
            float flockRadius,
            float groundMoveSpeed,
            float flyMoveSpeed,
            float scareRadius,
            float minReturnDelay,
            float maxReturnDelay)
        {
            _player = player;
            _flockRadius = Mathf.Max(0.5f, flockRadius);
            _flockCenter = SampleGround(flockCenter);
            _hasConfiguredFlockCenter = true;
            _groundMoveSpeed = Mathf.Max(0.05f, groundMoveSpeed);
            _flyMoveSpeed = Mathf.Max(0.05f, flyMoveSpeed);
            _scareRadius = Mathf.Max(0.1f, scareRadius);
            _minReturnDelay = Mathf.Max(0f, minReturnDelay);
            _maxReturnDelay = Mathf.Max(_minReturnDelay, maxReturnDelay);
            ResolveReferences();
            ConfigureMover();
            TryPlaceOnNavMesh();
        }

        public static Vector3 SampleGround(Vector3 position)
        {
            return GroundPositionSampler.SampleGroundOrNavMesh(position);
        }

        private void TickGroundIdle()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer > 0f)
                return;

            if (Random.value < 0.45f)
                EnterPeck();
            else
                EnterGroundWalk();
        }

        private void TickGroundWalk()
        {
            MoveGroundToward(_targetPosition, _groundMoveSpeed);

            if (HasReachedGroundDestination(_targetPosition, GroundArrivalDistance))
                EnterGroundIdle();
        }

        private void TickPeck()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
                EnterGroundIdle();
        }

        private void TickFlyAway()
        {
            MoveAirToward(_targetPosition, _flyMoveSpeed);

            if (Vector3.Distance(transform.position, _targetPosition) <= 0.35f)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0f)
                    EnterReturn();
            }
        }

        private void TickReturn()
        {
            MoveAirToward(_targetPosition, _flyMoveSpeed);

            if (Vector3.Distance(transform.position, _targetPosition) <= 0.35f)
                EnterLanding();
        }

        private void TickLanding()
        {
            MoveAirToward(_targetPosition, _flyMoveSpeed);

            if (Vector3.Distance(transform.position, _targetPosition) <= 0.12f)
            {
                PlaceAtLandingPosition();
                EnterGroundIdle();
            }
        }

        private void EnterGroundIdle()
        {
            _state = SparrowState.GroundIdle;
            _stateTimer = Random.Range(1.1f, 3.2f);
            EnableMover();
            StopMover();
            PlayRandomIdle();
        }

        private void EnterGroundWalk()
        {
            _state = SparrowState.GroundWalk;
            _targetPosition = RandomGroundPoint();
            PlayState(WalkState, 0.12f);
        }

        private void EnterPeck()
        {
            _state = SparrowState.Peck;
            _stateTimer = Random.Range(1.2f, 2.8f);
            StopMover();
            PlayState(EatState, 0.12f);
        }

        private void EnterFlyAway()
        {
            _state = SparrowState.FlyAway;
            _stateTimer = Random.Range(_minReturnDelay, _maxReturnDelay);
            StopMover();
            DisableMover();

            Vector3 away = transform.position - (_player != null ? _player.position : _flockCenter);
            away.y = 0f;
            if (away.sqrMagnitude <= 0.001f)
                away = Random.insideUnitSphere.WithY(0f);

            if (away.sqrMagnitude <= 0.001f)
                away = Vector3.forward;

            float flyHeight = Random.Range(MinFlyHeight, MaxFlyHeight);
            _targetPosition = transform.position
                + away.normalized * Random.Range(_flockRadius * 1.2f, _flockRadius * 1.9f)
                + Vector3.up * flyHeight;

            PlayState(FlyState, 0.08f);
        }

        private void EnterReturn()
        {
            _state = SparrowState.Return;
            _landingPosition = RandomGroundPoint();
            _targetPosition = _landingPosition + Vector3.up * Random.Range(MinFlyHeight, MaxFlyHeight);
            StopMover();
            DisableMover();
            PlayState(FlyState, 0.08f);
        }

        private void EnterLanding()
        {
            _state = SparrowState.Landing;
            _targetPosition = _landingPosition + Vector3.up * LandingHeight;
            StopMover();
            DisableMover();
            PlayState(FlyState, 0.08f);
        }

        private void MoveGroundToward(Vector3 destination, float speed)
        {
            if (_mover == null)
                return;

            EnableMover();

            if (!_mover.IsReady && !_mover.TryWarpToNearestNavMesh(Mathf.Max(1f, _flockRadius)))
                return;

            _mover.SetSpeed(speed);
            _mover.TryMoveTo(destination, Mathf.Max(1f, _flockRadius * 0.35f));

            Vector3 movement = _mover.Velocity;
            if (movement.sqrMagnitude <= 0.000001f)
                movement = destination - transform.position;

            if (movement.sqrMagnitude > 0.000001f)
                RotateToward(movement);
        }

        private void MoveAirToward(Vector3 destination, float speed)
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
            Vector2 offset = Random.insideUnitCircle * _flockRadius;
            Vector3 candidate = _flockCenter + new Vector3(offset.x, 0f, offset.y);
            return GroundPositionSampler.SampleNavMeshNearGround(candidate, _flockRadius);
        }

        private bool HasReachedGroundDestination(Vector3 destination, float extraDistance)
        {
            if (_mover != null && _mover.HasArrived(extraDistance))
                return true;

            return HorizontalDistance(transform.position, destination) <= extraDistance;
        }

        private void PlayRandomIdle()
        {
            float roll = Random.value;
            if (roll < 0.34f)
                PlayState(IdleAState, 0.12f);
            else if (roll < 0.67f)
                PlayState(IdleBState, 0.12f);
            else
                PlayState(IdleCState, 0.12f);
        }

        private void PlayState(int stateHash, float transitionDuration)
        {
            if (_animator == null || _currentAnimationHash == stateHash)
                return;

            _currentAnimationHash = stateHash;
            if (transitionDuration > 0f)
                _animator.CrossFade(stateHash, transitionDuration);
            else
                _animator.Play(stateHash);
        }

        private bool IsGroundedState()
        {
            return _state == SparrowState.GroundIdle
                || _state == SparrowState.GroundWalk
                || _state == SparrowState.Peck;
        }

        private float HorizontalDistanceToPlayer()
        {
            if (_player == null)
                return float.MaxValue;

            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            return toPlayer.magnitude;
        }

        private void ResolveReferences()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_mover == null)
                _mover = GetComponent<GroundNavMeshMover>();

            if (_animator != null)
                _animator.applyRootMotion = false;
        }

        private void ConfigureMover()
        {
            if (_mover == null)
                _mover = GetComponent<GroundNavMeshMover>();

            if (_mover == null)
                return;

            _mover.Configure(
                _groundMoveSpeed,
                GroundArrivalDistance,
                AgentRadius,
                AgentHeight,
                AgentBaseOffset,
                Mathf.Max(8f, _groundMoveSpeed * 4f),
                RotationSpeed,
                RepathInterval,
                AvoidancePriority);
        }

        private void TryPlaceOnNavMesh()
        {
            EnableMover();

            if (_mover != null && _mover.TryWarpToNearestNavMesh(Mathf.Max(2f, _flockRadius)))
                return;

            transform.position = SampleGround(transform.position);
        }

        private void PlaceAtLandingPosition()
        {
            transform.position = _landingPosition;
            EnableMover();

            if (_mover != null)
                _mover.TryWarpToNearestNavMesh(Mathf.Max(2f, _flockRadius));
        }

        private void StopMover()
        {
            if (_mover != null)
                _mover.Stop();
        }

        private void EnableMover()
        {
            if (_mover != null && _mover.Agent != null)
                _mover.Agent.enabled = true;
        }

        private void DisableMover()
        {
            if (_mover != null && _mover.Agent != null)
                _mover.Agent.enabled = false;
        }

        private void ResolvePlayer()
        {
            if (_player != null)
                return;

            var playerFacade = FindFirstObjectByType<PlayerFacade>();
            if (playerFacade != null)
                _player = playerFacade.transform;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }

    internal static class SparrowVectorExtensions
    {
        public static Vector3 WithY(this Vector3 value, float y)
        {
            value.y = y;
            return value;
        }
    }
}
