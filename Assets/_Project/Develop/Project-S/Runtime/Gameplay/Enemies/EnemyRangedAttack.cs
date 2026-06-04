using System;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Diagnostics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project_S.Runtime.Gameplay.Enemies
{
    public class EnemyRangedAttack : MonoBehaviour
    {
        private const string ProjectileResourcePath = "Enemies/Projectiles/Arrow";
        private const string ProjectileEditorPath = "Assets/Free medieval weapons/Prefabs/Arrow.prefab";
        private const float AimHeightFallback = 1.0f;
        private const float CameraAimLowerOffset = 0.18f;
        private const float HorizontalAimJitter = 0.18f;
        private const float VerticalAimJitter = 0.12f;
        private const float AngularSpreadDegrees = 2.2f;

        [SerializeField] private EnemyConfig _config;
        [SerializeField] private Transform _projectileOrigin;
        [SerializeField] private LayerMask _targetLayers = ~0;
        [SerializeField] private AnimationClip _attackClip;

        private Transform _pendingTarget;
        private float _cooldownRemaining;
        private float _windupRemaining;
        private bool _isWindingUp;

        public bool IsWindingUp => _isWindingUp;
        public float CooldownRemaining => _cooldownRemaining;
        public float WindupDuration => Mathf.Max(0.01f, _config != null ? _config.RangedAttackWindup : 0.55f);
        public AnimationClip CurrentAttackClip => _attackClip;
        public float CurrentAttackAnimationSpeed => _config != null ? Mathf.Max(0.01f, _config.RangedAttackAnimationSpeed) : 1f;

        public event Action<EnemyRangedAttack> AttackStarted;
        public event Action<EnemyRangedAttack> AttackResolved;

        private void Awake()
        {
            if (_projectileOrigin == null)
                _projectileOrigin = transform;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(EnemyConfig config, AnimationClip attackClip = null, Transform projectileOrigin = null)
        {
            _config = config;
            _attackClip = attackClip;

            if (projectileOrigin != null)
                _projectileOrigin = projectileOrigin;
            else if (_projectileOrigin == null)
                _projectileOrigin = transform;
        }

        public void OverrideCurrentWindupFromClip(float clipLength)
        {
            if (!_isWindingUp || _config == null)
                return;

            float duration = Mathf.Max(0f, clipLength) / CurrentAttackAnimationSpeed;
            if (_config.UseRangedAttackClipDamageMoment)
                duration *= Mathf.Clamp01(_config.RangedAttackDamageMomentNormalized);

            _windupRemaining = Mathf.Max(0f, duration);
        }

        public void CancelAttack()
        {
            _pendingTarget = null;
            _windupRemaining = 0f;
            _isWindingUp = false;
        }

        public bool TryAttack(Transform target)
        {
            if (target == null || _config == null || !_config.UseRangedAttack || _isWindingUp || _cooldownRemaining > 0f)
                return false;

            _pendingTarget = target;
            _windupRemaining = WindupDuration;
            _isWindingUp = true;
            AttackStarted?.Invoke(this);

            if (_windupRemaining <= 0f)
                ResolveAttack();

            return true;
        }

        private void Tick(float deltaTime)
        {
            deltaTime = Mathf.Max(0f, deltaTime);

            if (_cooldownRemaining > 0f)
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);

            if (!_isWindingUp)
                return;

            _windupRemaining -= deltaTime;
            if (_windupRemaining <= 0f)
                ResolveAttack();
        }

        private void ResolveAttack()
        {
            _isWindingUp = false;
            _cooldownRemaining = _config != null ? Mathf.Max(0f, _config.RangedAttackCooldown) : 0f;
            AttackResolved?.Invoke(this);
            SpawnProjectile();
        }

        private void SpawnProjectile()
        {
            if (_config == null || _pendingTarget == null)
                return;

            Transform origin = _projectileOrigin != null ? _projectileOrigin : transform;
            Vector3 start = origin.position + Vector3.up * 1.35f + origin.forward * 0.55f;
            Vector3 target = ResolveAimPoint(origin);
            Vector3 direction = target - start;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = origin.forward;

            direction.Normalize();
            direction = ApplyRandomSpread(direction);

            var projectileObject = CreateProjectileObject(direction);
            projectileObject.name = $"{name}_Projectile";
            projectileObject.transform.position = start;
            projectileObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            ConfigureProjectileColliders(projectileObject, Mathf.Max(0.01f, _config.RangedProjectileRadius));

            var rigidbody = projectileObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rigidbody.velocity = direction * Mathf.Max(0.01f, _config.RangedProjectileSpeed);

            var projectile = projectileObject.AddComponent<EnemyProjectile>();
            projectile.Configure(
                gameObject,
                _targetLayers,
                Mathf.Max(0f, _config.RangedHealthDamage),
                Mathf.Max(0f, _config.RangedPoiseDamage),
                _config.RangedDamageType,
                Mathf.Max(0.01f, _config.RangedProjectileLifetime),
                Mathf.Max(0.01f, _config.RangedProjectileRadius),
                _pendingTarget);
        }

        private Vector3 ResolveAimPoint(Transform origin)
        {
            Vector3 target = _pendingTarget.position + Vector3.up * AimHeightFallback;

            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.transform.root == _pendingTarget.root)
                target = mainCamera.transform.position - Vector3.up * CameraAimLowerOffset;

            Vector3 right = origin != null ? origin.right : transform.right;
            target += right * UnityEngine.Random.Range(-HorizontalAimJitter, HorizontalAimJitter);
            target += Vector3.up * UnityEngine.Random.Range(-VerticalAimJitter, VerticalAimJitter);
            return target;
        }

        private static Vector3 ApplyRandomSpread(Vector3 direction)
        {
            float yaw = UnityEngine.Random.Range(-AngularSpreadDegrees, AngularSpreadDegrees);
            float pitch = UnityEngine.Random.Range(-AngularSpreadDegrees, AngularSpreadDegrees);
            return Quaternion.Euler(pitch, yaw, 0f) * direction;
        }

        private static GameObject CreateProjectileObject(Vector3 direction)
        {
            var prefab = NpcStartupDiagnostics.LoadResource<GameObject>("EnemyProjectile", ProjectileResourcePath);
#if UNITY_EDITOR
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectileEditorPath);
#endif

            if (prefab != null)
                return Instantiate(prefab);

            var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            projectileObject.transform.localScale = new Vector3(0.1f, 0.45f, 0.1f);
            projectileObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            return projectileObject;
        }

        private static void ConfigureProjectileColliders(GameObject projectileObject, float radius)
        {
            var rootCollider = projectileObject.GetComponent<Collider>();
            if (rootCollider == null)
            {
                var capsule = projectileObject.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.radius = radius;
                capsule.height = 0.75f;
                rootCollider = capsule;
            }

            foreach (var collider in projectileObject.GetComponentsInChildren<Collider>())
                collider.isTrigger = true;
        }
    }
}
