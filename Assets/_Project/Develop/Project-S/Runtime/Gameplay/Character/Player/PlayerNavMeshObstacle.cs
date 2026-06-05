using KinematicCharacterController;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Character.Player
{
    [DisallowMultipleComponent]
    public class PlayerNavMeshObstacle : MonoBehaviour
    {
        private const float RadiusPadding = 0.05f;

        [SerializeField] private KinematicCharacterMotor _motor;
        [SerializeField] private CapsuleCollider _capsule;
        [SerializeField] private NavMeshObstacle _obstacle;
        [SerializeField] private float _movingDisableSpeed = 0.1f;
        [SerializeField] private float _reenableDelay = 0.2f;

        private Vector3 _lastPosition;
        private float _lastMovedTime;

        public float ObstacleRadius => _obstacle != null ? _obstacle.radius : ResolveRadius();

        private void Awake()
        {
            Apply(true);
            _lastPosition = transform.position;
        }

        private void OnValidate()
        {
            Apply(false);
        }

        private void Update()
        {
            UpdateObstacleState();
        }

        public void Apply(bool createObstacle = true)
        {
            ResolveReferences(createObstacle);

            if (_obstacle == null)
                return;

            _obstacle.shape = NavMeshObstacleShape.Capsule;
            _obstacle.carving = false;
            _obstacle.radius = ResolveRadius();
            _obstacle.height = ResolveHeight();
            _obstacle.center = ResolveCenter();
        }

        private void ResolveReferences(bool createObstacle)
        {
            if (_motor == null)
                _motor = GetComponent<KinematicCharacterMotor>();

            if (_capsule == null)
                _capsule = _motor != null && _motor.Capsule != null
                    ? _motor.Capsule
                    : GetComponent<CapsuleCollider>();

            if (_obstacle == null)
                _obstacle = GetComponent<NavMeshObstacle>();

            if (_obstacle == null && createObstacle)
                _obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        private float ResolveRadius()
        {
            return Mathf.Max(0.01f, (_capsule != null ? _capsule.radius : 0.5f) + RadiusPadding);
        }

        private float ResolveHeight()
        {
            float radius = ResolveRadius();
            return Mathf.Max(radius * 2f, _capsule != null ? _capsule.height : 2f);
        }

        private Vector3 ResolveCenter()
        {
            return _capsule != null ? _capsule.center : Vector3.zero;
        }

        private void UpdateObstacleState()
        {
            if (_obstacle == null)
                return;

            Vector3 position = transform.position;
            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = (position - _lastPosition).magnitude / deltaTime;
            _lastPosition = position;

            if (speed > _movingDisableSpeed)
                _lastMovedTime = Time.time;

            bool shouldBeEnabled = Time.time - _lastMovedTime >= _reenableDelay;
            if (_obstacle.enabled != shouldBeEnabled)
                _obstacle.enabled = shouldBeEnabled;
        }
    }
}
