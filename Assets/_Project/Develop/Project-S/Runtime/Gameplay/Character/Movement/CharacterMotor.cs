using KinematicCharacterController;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Character.Inventory; // ДОДАНО ДЛЯ ІНВЕНТАРЮ
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
        [SerializeField] private PoiseController _poiseController;
        [SerializeField] private InventoryController _inventory; // ДОДАНО ПОСИЛАННЯ НА ІНВЕНТАР

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
        public bool IsGrounded => _motor != null && _motor.GroundingStatus.IsStableOnGround;

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

            if (_poiseController != null && _poiseController.IsBroken)
            {
                _moveInput = Vector2.zero;
                _moveInputVector = Vector3.zero;
                _sprintHeld = false;
                _jumpRequested = false;
                return;
            }

            _moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            _moveInputVector = BuildMoveDirection(_moveInput);
            _sprintHeld = input.SprintHeld;

            if (input.JumpPressed)
            {
                _jumpRequested = true;
            }

            TryStartDodge(input);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            // ПРИБРАНО БЛОКУВАННЯ КАМЕРИ ПІД ЧАС ДЕШУ
            // Раніше тут було: if (IsDodging) return;
            currentRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_poiseController != null && _poiseController.PendingKnockback.sqrMagnitude > 0.001f)
            {
                currentVelocity += _poiseController.PendingKnockback;
            }

            if (_poiseController != null && _poiseController.IsBroken)
            {
                if (_motor.GroundingStatus.IsStableOnGround)
                {
                    currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, 10f * deltaTime);
                }
                else
                {
                    currentVelocity = Vector3.Project(currentVelocity, Vector3.up);
                    currentVelocity += Vector3.up * _config.Gravity * deltaTime;
                    currentVelocity *= 1f / (1f + _config.AirDrag * deltaTime);
                }
                return;
            }

            if (IsDodging)
            {
                currentVelocity = _dodgeVelocity;
                return;
            }

            if (_motor.GroundingStatus.IsStableOnGround)
            {
                currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity, _motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

                var inputRight = Vector3.Cross(_moveInputVector, _motor.CharacterUp);
                var reorientedInput = Vector3.Cross(_motor.GroundingStatus.GroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                var targetVelocity = reorientedInput * GetMoveSpeed(deltaTime);

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    1f - Mathf.Exp(-_config.Acceleration * deltaTime));
            }
            else
            {
                // ВІДНОВЛЕНО ІНЕРЦІЮ У ПОВІТРІ
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    // Тепер у повітрі використовується та сама швидкість (GetMoveSpeed), що й на землі.
                    // Якщо ти стрибнув на спринті, ти продовжиш летіти зі швидкістю спринту!
                    var targetVelocity = _moveInputVector * GetMoveSpeed(deltaTime);
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
            {
                _jumpConsumed = false;
            }
        }

        public bool IsColliderValidForCollisions(Collider coll) => true;
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

        private void UpdateView(Vector2 look)
        {
            if (_config == null) return;

            _yaw += look.x * _config.MouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - look.y * _config.MouseSensitivity, _config.MinPitch, _config.MaxPitch);

            if (_viewRoot != null)
            {
                _viewRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }

        private void TryStartDodge(PlayerInputSnapshot input)
        {
            if (_config == null || _stamina == null) return;

            if (!input.DodgePressed || Time.time < _dodgeCooldownUntil || !_motor.GroundingStatus.IsStableOnGround)
                return;

            if (!_stamina.Spend(_config.DodgeStaminaCost))
                return;

            var direction = _moveInputVector;

            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            }

            _dodgeVelocity = direction.normalized * _config.DodgeSpeed;
            _dodgeUntil = Time.time + _config.DodgeDuration;
            _dodgeCooldownUntil = Time.time + _config.DodgeCooldown;
        }

        private void TryApplyJump(ref Vector3 currentVelocity)
        {
            if (!_jumpRequested || _config == null) return;

            _jumpRequested = false;

            if (_jumpConsumed || !_motor.GroundingStatus.IsStableOnGround) return;

            // СТРИБОК ЖЕРЕ СТАМІНУ (Поставив 15 одиниць, можеш винести це в _config)
            if (_stamina != null && !_stamina.Spend(15f)) return;

            _motor.ForceUnground(0.1f);
            currentVelocity += Vector3.up * Mathf.Sqrt(_config.JumpHeight * -2f * _config.Gravity) -
                               Vector3.Project(currentVelocity, _motor.CharacterUp);
            _jumpConsumed = true;
        }

        private float GetMoveSpeed(float deltaTime)
        {
            if (_stats == null || _config == null || _stamina == null) return 5f;

            var hasMove = _moveInput.sqrMagnitude > 0.01f;

            // ПЕРЕВІРКА ВАГИ ТА БЛОКУВАННЯ СПРИНТУ
            bool isOverweight = false;
            float weightMultiplier = 1f;

            if (_inventory != null)
            {
                isOverweight = _inventory.GetCurrentWeight() > _inventory.GetMaxWeight();
                weightMultiplier = _inventory.GetWeightSpeedMultiplier();
            }

            // Якщо перевантажені — спринт автоматично відключається (не забирає стаміну і не дає швидкості)
            var canSprint = hasMove && _sprintHeld && !isOverweight && _stamina.Spend(_config.SprintStaminaCostPerSecond * deltaTime);

            float baseSpeed = canSprint ? _stats.Get(StatType.SprintSpeed) : _stats.Get(StatType.MoveSpeed);

            // Множимо фінальну швидкість на штраф від ваги (0.95, 0.4 або 0)
            return baseSpeed * weightMultiplier;
        }

        private Vector3 BuildMoveDirection(Vector2 move)
        {
            var rotation = Quaternion.Euler(0f, _yaw, 0f);
            return rotation * new Vector3(move.x, 0f, move.y);
        }

        private void ValidateRequiredReferences()
        {
            if (_config == null) Debug.LogError($"{nameof(CharacterMotor)} requires {nameof(MovementConfig)} reference.", this);
            if (_stats == null) Debug.LogError($"{nameof(CharacterMotor)} requires {nameof(CharacterStats)} reference.", this);
            if (_stamina == null) Debug.LogError($"{nameof(CharacterMotor)} requires {nameof(StaminaController)} reference.", this);
            if (_poiseController == null) Debug.LogWarning($"{nameof(CharacterMotor)} requires {nameof(PoiseController)} reference.", this);
            if (_inventory == null) Debug.LogWarning($"{nameof(CharacterMotor)} requires {nameof(InventoryController)} reference.", this);
        }
    }
}