using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Navigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class GroundNavMeshMover : MonoBehaviour
    {
        private const float DefaultSampleRadius = 2f;
        private const float MovingVelocityThreshold = 0.01f;

        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private float _repathInterval = 0.2f;
        [SerializeField] private float _sampleRadius = DefaultSampleRadius;

        private float _repathTimer;
        private Vector3 _lastRequestedDestination;
        private bool _hasRequestedDestination;

        public NavMeshAgent Agent => _agent;
        public bool IsReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;
        public bool IsMoving => IsReady && !_agent.isStopped && _agent.velocity.sqrMagnitude > MovingVelocityThreshold;
        public bool HasPath => IsReady && _agent.hasPath;
        public NavMeshPathStatus PathStatus => IsReady ? _agent.pathStatus : NavMeshPathStatus.PathInvalid;
        public float RemainingDistance => IsReady ? _agent.remainingDistance : float.PositiveInfinity;
        public Vector3 Velocity => IsReady ? _agent.velocity : Vector3.zero;

        private void Awake()
        {
            ResolveAgent();
            ConfigureAgentDefaults();
        }

        private void OnEnable()
        {
            ResolveAgent();
            ConfigureAgentDefaults();
        }

        private void Update()
        {
            if (_repathTimer > 0f)
                _repathTimer -= Time.deltaTime;
        }

        public void Configure(
            float speed,
            float stoppingDistance,
            float radius,
            float height,
            float baseOffset,
            float acceleration,
            float angularSpeed,
            float repathInterval,
            int avoidancePriority)
        {
            ResolveAgent();

            if (_agent == null)
                return;

            _agent.speed = Mathf.Max(0f, speed);
            _agent.stoppingDistance = Mathf.Max(0f, stoppingDistance);
            _agent.radius = Mathf.Max(0.01f, radius);
            _agent.height = Mathf.Max(_agent.radius * 2f, height);
            _agent.baseOffset = baseOffset;
            _agent.acceleration = Mathf.Max(0.01f, acceleration);
            _agent.angularSpeed = Mathf.Max(0f, angularSpeed);
            _agent.avoidancePriority = Mathf.Clamp(avoidancePriority, 0, 99);

            _repathInterval = Mathf.Max(0.02f, repathInterval);
            ConfigureAgentDefaults();
        }

        public void SetSpeed(float speed)
        {
            ResolveAgent();

            if (_agent != null)
                _agent.speed = Mathf.Max(0f, speed);
        }

        public bool TryWarpToNearestNavMesh(float searchRadius)
        {
            ResolveAgent();

            if (_agent == null)
                return false;

            if (_agent.isOnNavMesh)
                return true;

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, Mathf.Max(0.01f, searchRadius), NavMesh.AllAreas))
                return false;

            _agent.Warp(hit.position);
            return _agent.isOnNavMesh;
        }

        public bool TryMoveTo(Vector3 destination, float sampleRadius = DefaultSampleRadius, bool forceRepath = false)
        {
            ResolveAgent();

            if (!IsReady)
                return false;

            float radius = Mathf.Max(0.01f, sampleRadius > 0f ? sampleRadius : _sampleRadius);
            if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                Stop();
                return false;
            }

            if (!forceRepath && _hasRequestedDestination && _repathTimer > 0f)
            {
                Vector3 delta = hit.position - _lastRequestedDestination;
                if (delta.sqrMagnitude < 0.05f * 0.05f)
                    return PathStatus != NavMeshPathStatus.PathInvalid;
            }

            _lastRequestedDestination = hit.position;
            _hasRequestedDestination = true;
            _repathTimer = _repathInterval;
            _agent.isStopped = false;
            return _agent.SetDestination(hit.position);
        }

        public bool HasArrived(float extraDistance = 0f)
        {
            if (!IsReady)
                return false;

            if (_agent.pathPending)
                return false;

            float allowedDistance = _agent.stoppingDistance + Mathf.Max(0f, extraDistance);
            return _agent.remainingDistance <= allowedDistance;
        }

        public void Stop()
        {
            if (!IsReady)
                return;

            _agent.isStopped = true;
            _agent.ResetPath();
            _hasRequestedDestination = false;
        }

        private void ResolveAgent()
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();
        }

        private void ConfigureAgentDefaults()
        {
            if (_agent == null)
                return;

            _agent.updateRotation = false;
            _agent.updateUpAxis = true;
            _agent.autoTraverseOffMeshLink = false;
            _agent.autoBraking = true;
        }
    }
}
