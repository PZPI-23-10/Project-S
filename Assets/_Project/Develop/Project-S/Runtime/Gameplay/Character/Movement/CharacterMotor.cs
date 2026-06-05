using KinematicCharacterController;
using Project_S.Runtime.Gameplay.Ambient;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Respawn;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Movement
{
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public class CharacterMotor : MonoBehaviour, ICharacterController, IPlayerRespawnResettable
    {
        [SerializeField] private MovementConfig _config;
        [SerializeField] private Transform _viewRoot;
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private StaminaController _stamina;
        [SerializeField] private PoiseController _poiseController;
        [SerializeField] private InventoryController _inventory;

        [Header("Аудіо")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _jumpSound;
        [SerializeField] private AudioClip _dodgeSound;
        [SerializeField] private AudioClip _landSound;
        [SerializeField] private AudioClip _footstepSound;

        [Header("Living Creature Collision")]
        [SerializeField] private float _livingCreatureSeparationSkin = 0.04f;
        [SerializeField] private float _livingCreatureMaxDepenetrationPerFrame = 0.35f;
        [SerializeField] private int _livingCreatureDepenetrationIterations = 3;

        private KinematicCharacterMotor _motor;
        private Vector2 _moveInput;
        private Vector3 _moveInputVector;
        private Vector3 _dodgeVelocity;
        private readonly Collider[] _uncrouchOverlapBuffer = new Collider[8];
        private readonly Collider[] _livingCreatureOverlapBuffer = new Collider[8];
        private readonly RaycastHit[] _livingCreatureCastBuffer = new RaycastHit[8];
        private float _yaw;
        private float _pitch;
        private float _dodgeUntil;
        private float _dodgeCooldownUntil;
        private float _standingCapsuleRadius;
        private float _standingCapsuleHeight;
        private float _standingCapsuleYOffset;
        private Vector3 _standingViewLocalPosition;
        private bool _sprintHeld;
        private bool _crouchHeld;
        private bool _isCrouching;
        private bool _jumpRequested;
        private bool _jumpConsumed;

        private float _attackDashUntil;
        private float _attackDashSpeed;
        private float _attackDashTurnSpeed;
        private float _attackDashCurrentYaw;

        // Змінні для кроків та приземлення
        private bool _wasGrounded;
        private float _nextStepTime;

        public bool IsAttackDashing => Time.time < _attackDashUntil;

        public bool IsDodging => Time.time < _dodgeUntil;
        public bool IsGrounded => _motor != null && _motor.GroundingStatus.IsStableOnGround;

        public void ResetForRespawn()
        {
            _moveInput = Vector2.zero;
            _moveInputVector = Vector3.zero;
            _dodgeVelocity = Vector3.zero;
            _sprintHeld = false;
            _crouchHeld = false;
            _jumpRequested = false;
            _jumpConsumed = false;
            _dodgeUntil = 0f;
            _dodgeCooldownUntil = 0f;
            _attackDashUntil = 0f;
            _attackDashSpeed = 0f;
            _attackDashTurnSpeed = 0f;
            _nextStepTime = 0f;
            _wasGrounded = true;

            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
            _attackDashCurrentYaw = _yaw;

            if (_motor != null)
                _motor.BaseVelocity = Vector3.zero;

            RestoreStandingPose();
        }

        private void Awake()
        {
            _motor = GetComponent<KinematicCharacterMotor>();
            _motor.CharacterController = this;
            _yaw = transform.eulerAngles.y;
            CacheStandingPose();

            ValidateRequiredReferences();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ForceAttackDash(float speed, float duration, float turnSpeed)
        {
            _attackDashSpeed = speed;
            _attackDashTurnSpeed = turnSpeed;
            _attackDashCurrentYaw = _yaw;
            _attackDashUntil = Time.time + duration;
        }

        public void StopAttackDash()
        {
            _attackDashUntil = 0f;
        }

        public void Tick(PlayerInputSnapshot input)
        {
            UpdateView(input.Look);

            if (_poiseController != null && _poiseController.IsBroken)
            {
                _moveInput = Vector2.zero;
                _moveInputVector = Vector3.zero;
                _sprintHeld = false;
                _crouchHeld = false;
                _jumpRequested = false;
                return;
            }

            _moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            _moveInputVector = BuildMoveDirection(_moveInput);
            _sprintHeld = input.SprintHeld;
            _crouchHeld = input.CrouchHeld;

            if (input.JumpPressed)
            {
                _jumpRequested = true;
            }

            TryStartDodge(input);
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = Quaternion.Euler(0f, _yaw, 0f);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_poiseController != null && _poiseController.PendingKnockback.sqrMagnitude > 0.001f)
            {
                currentVelocity += _poiseController.ConsumeKnockback();
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

            if (IsAttackDashing)
            {
                _attackDashCurrentYaw = Mathf.MoveTowardsAngle(_attackDashCurrentYaw, _yaw, _attackDashTurnSpeed * deltaTime);
                Vector3 steerDirection = Quaternion.Euler(0f, _attackDashCurrentYaw, 0f) * Vector3.forward;
                currentVelocity = steerDirection * _attackDashSpeed;
                BlockVelocityIntoLivingCreatures(ref currentVelocity, deltaTime);
                return;
            }

            if (IsDodging)
            {
                currentVelocity = _dodgeVelocity;
                BlockVelocityIntoLivingCreatures(ref currentVelocity, deltaTime);
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
                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    var targetVelocity = _moveInputVector * GetMoveSpeed(deltaTime);
                    var velocityDiff = Vector3.ProjectOnPlane(targetVelocity - currentVelocity, Vector3.up);
                    currentVelocity += velocityDiff * _config.AirAcceleration * deltaTime;
                }

                currentVelocity += Vector3.up * _config.Gravity * deltaTime;
                currentVelocity *= 1f / (1f + _config.AirDrag * deltaTime);
            }

            TryApplyJump(ref currentVelocity);
            BlockVelocityIntoLivingCreatures(ref currentVelocity, deltaTime);
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (_crouchHeld)
            {
                EnterCrouch();
            }

            DepenetrateFromLivingCreatures();
        }

        public void PostGroundingUpdate(float deltaTime) { }

        public void AfterCharacterUpdate(float deltaTime)
        {
            bool isGroundedNow = _motor.GroundingStatus.IsStableOnGround;

            // ==========================================
            // ЗВУК ПРИЗЕМЛЕННЯ
            // ==========================================
            if (isGroundedNow && !_wasGrounded)
            {
                if (_landSound != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(_landSound);
                }
            }

            // ==========================================
            // ЗВУКИ КРОКІВ (Таймер)
            // ==========================================
            if (isGroundedNow && _moveInputVector.sqrMagnitude > 0.01f)
            {
                if (Time.time >= _nextStepTime)
                {
                    if (_footstepSound != null && _audioSource != null)
                    {
                        _audioSource.pitch = Random.Range(0.85f, 1.15f);
                        _audioSource.PlayOneShot(_footstepSound, 0.4f); // 0.4f робить кроки тихішими
                    }

                    _nextStepTime = Time.time + (_sprintHeld ? 0.3f : 0.5f);
                }
            }

            _wasGrounded = isGroundedNow;

            if (isGroundedNow)
            {
                _jumpConsumed = false;
            }

            if (_isCrouching && !_crouchHeld)
            {
                TryExitCrouch();
            }

            DepenetrateFromLivingCreatures();
            UpdateCrouchView(deltaTime);
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (coll == null || coll.isTrigger)
                return false;

            var corpseHarvest = coll.GetComponentInParent<AnimalCorpseHarvest>();
            if (corpseHarvest != null && corpseHarvest.IsActive)
                return false;

            return true;
        }
        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

        private void UpdateView(Vector2 look)
        {
            if (_config == null) return;
            if (Time.timeScale == 0f) return;

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

            if (_inventory != null && _inventory.GetCurrentWeight() > _inventory.GetMaxWeight())
            {
                return;
            }

            if (!input.DodgePressed || Time.time < _dodgeCooldownUntil || !_motor.GroundingStatus.IsStableOnGround)
                return;

            if (!_stamina.Spend(_config.DodgeStaminaCost))
                return;

            if (_dodgeSound != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.15f);
                _audioSource.PlayOneShot(_dodgeSound);
            }

            var direction = _moveInputVector;

            if (direction.sqrMagnitude <= 0.01f)
            {
                direction = Quaternion.Euler(0f, _yaw, 0f) * Vector3.forward;
            }

            _dodgeVelocity = direction.normalized * _config.DodgeSpeed;
            _dodgeUntil = Time.time + _config.DodgeDuration;
            _dodgeCooldownUntil = Time.time + _config.DodgeCooldown;
        }

        private void BlockVelocityIntoLivingCreatures(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_motor == null || _motor.Capsule == null)
                return;

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);
            float speed = horizontalVelocity.magnitude;
            if (speed <= 0.001f)
                return;

            GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius);
            Vector3 direction = horizontalVelocity / speed;
            int layerMask = _motor.CollidableLayers;

            int overlapCount = Physics.OverlapCapsuleNonAlloc(
                point1,
                point2,
                radius,
                _livingCreatureOverlapBuffer,
                layerMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider hit = _livingCreatureOverlapBuffer[i];
                if (!IsBlockingLivingCreature(hit))
                    continue;

                Vector3 normal = TryGetLivingCreaturePenetrationNormal(hit, out Vector3 penetrationNormal)
                    ? penetrationNormal
                    : Vector3.ProjectOnPlane(transform.position - hit.transform.position, _motor.CharacterUp);
                if (TryBlockVelocityWithNormal(ref currentVelocity, normal))
                    return;
            }

            float castDistance = Mathf.Max(0.05f, speed * Mathf.Max(0f, deltaTime) + 0.12f);
            int hitCount = Physics.CapsuleCastNonAlloc(
                point1,
                point2,
                radius,
                direction,
                _livingCreatureCastBuffer,
                castDistance,
                layerMask,
                QueryTriggerInteraction.Ignore);

            int closestIndex = -1;
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _livingCreatureCastBuffer[i];
                if (hit.collider == null || hit.distance >= closestDistance)
                    continue;

                if (!IsBlockingLivingCreature(hit.collider))
                    continue;

                closestIndex = i;
                closestDistance = hit.distance;
            }

            if (closestIndex < 0)
                return;

            TryBlockVelocityWithNormal(ref currentVelocity, _livingCreatureCastBuffer[closestIndex].normal);
        }

        private void DepenetrateFromLivingCreatures()
        {
            if (_motor == null || _motor.Capsule == null)
                return;

            int iterations = Mathf.Max(0, _livingCreatureDepenetrationIterations);
            float maxCorrection = Mathf.Max(0f, _livingCreatureMaxDepenetrationPerFrame);
            float appliedCorrection = 0f;

            for (int iteration = 0; iteration < iterations && appliedCorrection < maxCorrection; iteration++)
            {
                GetCapsuleWorldPoints(_motor.TransientPosition, _motor.TransientRotation, out Vector3 point1, out Vector3 point2, out float radius);
                int overlapCount = Physics.OverlapCapsuleNonAlloc(
                    point1,
                    point2,
                    radius,
                    _livingCreatureOverlapBuffer,
                    _motor.CollidableLayers,
                    QueryTriggerInteraction.Ignore);

                Vector3 correction = Vector3.zero;
                for (int i = 0; i < overlapCount; i++)
                {
                    Collider hit = _livingCreatureOverlapBuffer[i];
                    if (!IsBlockingLivingCreature(hit))
                        continue;

                    if (!TryGetLivingCreaturePenetration(hit, out Vector3 direction, out float distance))
                        continue;

                    float correctionDistance = distance + Mathf.Max(0f, _livingCreatureSeparationSkin);
                    correction += direction * correctionDistance;
                }

                correction = Vector3.ProjectOnPlane(correction, _motor.CharacterUp);
                float correctionMagnitude = correction.magnitude;
                if (correctionMagnitude <= 0.0001f)
                    return;

                float remainingCorrection = maxCorrection - appliedCorrection;
                if (correctionMagnitude > remainingCorrection)
                {
                    correction = correction / correctionMagnitude * remainingCorrection;
                    correctionMagnitude = remainingCorrection;
                }

                _motor.SetTransientPosition(_motor.TransientPosition + correction);
                appliedCorrection += correctionMagnitude;
            }
        }

        private bool TryGetLivingCreaturePenetrationNormal(Collider hit, out Vector3 normal)
        {
            normal = Vector3.zero;
            if (!TryGetLivingCreaturePenetration(hit, out Vector3 direction, out _))
                return false;

            normal = direction;
            return true;
        }

        private bool TryGetLivingCreaturePenetration(Collider hit, out Vector3 direction, out float distance)
        {
            direction = Vector3.zero;
            distance = 0f;

            if (hit == null || _motor == null || _motor.Capsule == null)
                return false;

            if (!Physics.ComputePenetration(
                    _motor.Capsule,
                    _motor.TransientPosition,
                    _motor.TransientRotation,
                    hit,
                    hit.transform.position,
                    hit.transform.rotation,
                    out Vector3 penetrationDirection,
                    out float penetrationDistance))
                return false;

            penetrationDirection = Vector3.ProjectOnPlane(penetrationDirection, _motor.CharacterUp);
            if (penetrationDirection.sqrMagnitude <= 0.0001f)
                return false;

            direction = penetrationDirection.normalized;
            distance = Mathf.Max(0f, penetrationDistance);
            return true;
        }

        private bool TryBlockVelocityWithNormal(ref Vector3 currentVelocity, Vector3 normal)
        {
            normal = Vector3.ProjectOnPlane(normal, _motor.CharacterUp);
            if (normal.sqrMagnitude <= 0.0001f)
                return false;

            normal.Normalize();
            float inwardSpeed = Vector3.Dot(currentVelocity, normal);
            if (inwardSpeed >= 0f)
                return false;

            currentVelocity -= normal * inwardSpeed;
            return true;
        }

        private bool IsBlockingLivingCreature(Collider coll)
        {
            if (coll == null || coll.isTrigger || coll.transform.root == transform.root)
                return false;

            var corpseHarvest = coll.GetComponentInParent<AnimalCorpseHarvest>();
            if (corpseHarvest != null && corpseHarvest.IsActive)
                return false;

            var health = coll.GetComponentInParent<EnemyHealth>();
            return health != null && !health.IsDead;
        }

        private void GetCapsuleWorldPoints(out Vector3 point1, out Vector3 point2, out float radius)
        {
            GetCapsuleWorldPoints(_motor.TransientPosition, _motor.TransientRotation, out point1, out point2, out radius);
        }

        private void GetCapsuleWorldPoints(Vector3 motorPosition, Quaternion motorRotation, out Vector3 point1, out Vector3 point2, out float radius)
        {
            CapsuleCollider capsule = _motor.Capsule;
            radius = Mathf.Max(0.01f, capsule.radius);
            float halfSegment = Mathf.Max(0f, capsule.height * 0.5f - radius);
            Vector3 center = motorPosition + (motorRotation * capsule.center);
            Vector3 axis = motorRotation * Vector3.up;

            point1 = center + axis * halfSegment;
            point2 = center - axis * halfSegment;
        }

        private void TryApplyJump(ref Vector3 currentVelocity)
        {
            if (!_jumpRequested || _config == null) return;

            _jumpRequested = false;

            if (_jumpConsumed || !_motor.GroundingStatus.IsStableOnGround) return;

            if (_stamina != null && !_stamina.Spend(15f)) return;

            if (_jumpSound != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.9f, 1.1f);
                _audioSource.PlayOneShot(_jumpSound);
            }

            _motor.ForceUnground(0.1f);
            currentVelocity += Vector3.up * Mathf.Sqrt(_config.JumpHeight * -2f * _config.Gravity) -
                               Vector3.Project(currentVelocity, _motor.CharacterUp);
            _jumpConsumed = true;
        }

        private float GetMoveSpeed(float deltaTime)
        {
            if (_stats == null || _config == null || _stamina == null) return 5f;

            bool hasMovementInput = _moveInput.sqrMagnitude > 0.01f;
            bool isOverweight = false;
            float weightMultiplier = 1f;

            if (_inventory != null)
            {
                isOverweight = _inventory.GetCurrentWeight() > _inventory.GetMaxWeight();
                weightMultiplier = _inventory.GetWeightPenaltyMultiplier();
            }

            var canSprint = hasMovementInput && _sprintHeld && !_isCrouching && weightMultiplier > 0f && _stamina.Spend(_config.SprintStaminaCostPerSecond * deltaTime);

            float baseSpeed = canSprint ? _stats.Get(StatType.SprintSpeed) : _stats.Get(StatType.MoveSpeed);

            if (_isCrouching)
            {
                baseSpeed *= Mathf.Clamp01(_config.CrouchSpeedMultiplier);
            }

            return baseSpeed * weightMultiplier;
        }

        private Vector3 BuildMoveDirection(Vector2 move)
        {
            var rotation = Quaternion.Euler(0f, _yaw, 0f);
            return rotation * new Vector3(move.x, 0f, move.y);
        }

        private void CacheStandingPose()
        {
            if (_motor != null && _motor.Capsule != null)
            {
                _standingCapsuleRadius = _motor.Capsule.radius;
                _standingCapsuleHeight = _motor.Capsule.height;
                _standingCapsuleYOffset = _motor.Capsule.center.y;
            }

            if (_viewRoot != null)
            {
                _standingViewLocalPosition = _viewRoot.localPosition;
            }
        }

        private void EnterCrouch()
        {
            if (_isCrouching || _config == null || _motor == null || _motor.Capsule == null)
                return;

            float crouchHeight = GetCrouchCapsuleHeight();
            _motor.SetCapsuleDimensions(_standingCapsuleRadius, crouchHeight, GetCrouchCapsuleYOffset(crouchHeight));
            _isCrouching = true;
        }

        private void TryExitCrouch()
        {
            if (_config == null || _motor == null || _motor.Capsule == null)
                return;

            _motor.SetCapsuleDimensions(_standingCapsuleRadius, _standingCapsuleHeight, _standingCapsuleYOffset);

            int overlaps = _motor.CharacterOverlap(
                _motor.TransientPosition,
                _motor.TransientRotation,
                _uncrouchOverlapBuffer,
                _motor.CollidableLayers,
                QueryTriggerInteraction.Ignore);

            if (overlaps > 0)
            {
                float crouchHeight = GetCrouchCapsuleHeight();
                _motor.SetCapsuleDimensions(_standingCapsuleRadius, crouchHeight, GetCrouchCapsuleYOffset(crouchHeight));
                return;
            }

            _isCrouching = false;
        }

        private void UpdateCrouchView(float deltaTime)
        {
            if (_viewRoot == null || _config == null)
                return;

            Vector3 targetPosition = _standingViewLocalPosition;

            if (_isCrouching)
            {
                targetPosition.y -= Mathf.Max(0f, _standingCapsuleHeight - GetCrouchCapsuleHeight()) * _config.CrouchViewHeightMultiplier;
            }

            float transitionSpeed = Mathf.Max(0f, _config.CrouchTransitionSpeed);
            float t = transitionSpeed <= 0f ? 1f : 1f - Mathf.Exp(-transitionSpeed * deltaTime);
            _viewRoot.localPosition = Vector3.Lerp(_viewRoot.localPosition, targetPosition, t);
        }

        private float GetCrouchCapsuleHeight()
        {
            if (_config == null)
                return _standingCapsuleHeight;

            float minHeight = (_standingCapsuleRadius * 2f) + 0.01f;
            return Mathf.Clamp(_config.CrouchHeight, minHeight, _standingCapsuleHeight);
        }

        private float GetCrouchCapsuleYOffset(float crouchHeight)
        {
            return _standingCapsuleYOffset - ((_standingCapsuleHeight - crouchHeight) * 0.5f);
        }

        private void RestoreStandingPose()
        {
            if (_motor != null && _motor.Capsule != null && _standingCapsuleHeight > 0f)
            {
                _motor.SetCapsuleDimensions(_standingCapsuleRadius, _standingCapsuleHeight, _standingCapsuleYOffset);
            }

            _isCrouching = false;

            if (_viewRoot != null)
            {
                _viewRoot.localPosition = _standingViewLocalPosition;
                _viewRoot.localRotation = Quaternion.identity;
            }
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

