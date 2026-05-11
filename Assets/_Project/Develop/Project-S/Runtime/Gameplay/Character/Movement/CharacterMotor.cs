using KinematicCharacterController;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Movement
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class CharacterMotor : MonoBehaviour, ICharacterController
    {
        [SerializeField] private MovementConfig _config;
        [SerializeField] private Transform _viewRoot;
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private StaminaController _stamina;

        private KinematicCharacterMotor _motor;
        private Vector2 _moveInput;
        private Vector3 _moveInputVector;
        private Vector3 _dodgeVelocity;
        private float _yaw;
        private float _pitch;
        private float _dodgeUntil;
        private float _dodgeCooldownUntil;
        private bool _sprintHeld;
        private bool _jumpRequested;
        private bool _jumpConsumed;

        public bool IsDodging => Time.time < _dodgeUntil;
        public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;

        private void Awake()
        {
            _motor = GetComponent<KinematicCharacterMotor>();
            _motor.CharacterController = this;
            _yaw = transform.eulerAngles.y;

            ValidateRequiredReferences();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Tick(PlayerInputSnapshot input)
        {
            UpdateView(input.Look);

            _moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            _moveInputVector = BuildMoveDirection(_moveInput);
            _sprintHeld = input.SprintHeld;

            if (input.JumpPressed)
                _jumpRequested = true;

            TryStartDodge(input);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (IsDodging)
            {
                currentVelocity = _dodgeVelocity;
                return;
            }

            if (_motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) *
                                  currentVelocity.magnitude;

                var inputRight = Vector3.Cross(_moveInputVector, _motor.CharacterUp);
                var reorientedInput = Vector3.Cross(_motor.GroundingStatus.GroundNormal, inputRight).normalized *
                                      _moveInputVector.magnitude;
                var targetVelocity = reorientedInput * GetMoveSpeed(deltaTime);

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1f - Mathf.Exp(-_config.Acceleration * deltaTime));
            }
            else
            {
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    var targetVelocity = _moveInputVector * _stats.Get(StatType.MoveSpeed);
                    var velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, Vector3.up);
                    currentVelocity += velocityDiff * _config.AirAcceleration * deltaTime;
                }

                currentVelocity += Vector3.up * _config.Gravity * deltaTime;
                currentVelocity *= 1f / (1f + _config.AirDrag * deltaTime);
            }

            TryApplyJump(ref currentVelocity);
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void PostGroundingUpdate(float deltaTime) { }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (_motor.GroundingStatus.IsStableOnGround)
                _jumpConsumed = false;
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

        private void UpdateView(Vector2 look)
        {
            _yaw += look.x * _config.MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * _config.MouseSensitivity, _config.MinPitch, _config.MaxPitch);

            if (_viewRoot != null)
                _viewRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void TryStartDodge(PlayerInputSnapshot input)
        {
            if (!input.DodgePressed || Time.time < _dodgeCooldownUntil || !_motor.GroundingStatus.IsStableOnGround)
                return;

            if (!_stamina.Spend(_config.DodgeStaminaCost))
                return;

            var direction = _moveInputVector;

            if (direction.sqrMagnitude <= 0.01f)
                direction = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;

            _dodgeVelocity = direction.normalized * _config.DodgeSpeed;
            _dodgeUntil = Time.time + _config.DodgeDuration;
            _dodgeCooldownUntil = Time.time + _config.DodgeCooldown;
        }

        private void TryApplyJump(ref Vector3 currentVelocity)
        {
            if (!_jumpRequested)
                return;

            _jumpRequested = false;

            if (_jumpConsumed || !_motor.GroundingStatus.IsStableOnGround)
                return;

            _motor.ForceUnground(0.1f);
            currentVelocity += Vector3.up * Mathf.Sqrt(_config.JumpHeight * -2f * _config.Gravity) -
                               Vector3.Project(currentVelocity, _motor.CharacterUp);
            _jumpConsumed = true;
        }

        private float GetMoveSpeed(float deltaTime)
        {
            var hasMove = _moveInput.sqrMagnitude > 0.01f;
            var canSprint = hasMove && _sprintHeld && _stamina.Spend(_config.SprintStaminaCostPerSecond * deltaTime);

            return canSprint ? _stats.Get(StatType.SprintSpeed) : _stats.Get(StatType.MoveSpeed);
        }

        private Vector3 BuildMoveDirection(Vector2 move)
        {
            var rotation = Quaternion.Euler(0f, _yaw, 0f);
            return rotation * new Vector3(move.x, 0f, move.y);
        }

        private void ValidateRequiredReferences()
        {
            if (_config == null)
                Debug.LogError($"{nameof(CharacterMotor)} requires {nameof(MovementConfig)} reference.", this);

            if (_stats == null)
                Debug.LogError($"{nameof(CharacterMotor)} requires {nameof(CharacterStats)} reference.", this);

            if (_stamina == null)
                Debug.LogError($"{nameof(CharacterMotor)} requires {nameof(StaminaController)} reference.", this);

            if (_stats == null)
                return;

            if (_stats.Get(StatType.MoveSpeed) <= 0f)
                Debug.LogError($"{nameof(CharacterMotor)} requires positive {nameof(StatType.MoveSpeed)}.", this);

            if (_stats.Get(StatType.SprintSpeed) <= 0f)
                Debug.LogError($"{nameof(CharacterMotor)} requires positive {nameof(StatType.SprintSpeed)}.", this);
        }
    }
}
