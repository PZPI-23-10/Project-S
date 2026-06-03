using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public class EnemyMeleeAttack : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private LayerMask _targetLayers = ~0;
        [SerializeField] private AttackSelectionMode _attackSelectionMode = AttackSelectionMode.Cycle;
        [SerializeField] private EnemyAttackProfile[] _attackProfiles = Array.Empty<EnemyAttackProfile>();

        private Transform _pendingTarget;
        private float _cooldownRemaining;
        private float _windupRemaining;
        private bool _isWindingUp;
        private int _nextAttackProfileIndex;
        private EnemyAttackProfile _currentAttackProfile;

        public bool IsWindingUp => _isWindingUp;
        public float CooldownRemaining => _cooldownRemaining;
        public float WindupDuration => Mathf.Max(0.01f, CurrentAttackWindup());
        public EnemyAttackProfile CurrentAttackProfile => _currentAttackProfile;
        public string CurrentAttackId => _currentAttackProfile != null ? _currentAttackProfile.Id : string.Empty;
        public AnimationClip CurrentAttackClip => _currentAttackProfile != null ? _currentAttackProfile.Clip : null;

        public event Action<EnemyMeleeAttack> AttackStarted;
        public event Action<EnemyMeleeAttack> AttackResolved;

        private void Awake()
        {
            if (_attackOrigin == null)
                _attackOrigin = transform;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Configure(EnemyConfig config)
        {
            _config = config;
        }

        public void ConfigureAttackProfiles(IEnumerable<EnemyAttackProfile> attackProfiles, AttackSelectionMode selectionMode = AttackSelectionMode.Cycle)
        {
            _attackProfiles = attackProfiles != null
                ? new List<EnemyAttackProfile>(attackProfiles).ToArray()
                : Array.Empty<EnemyAttackProfile>();
            _attackSelectionMode = selectionMode;
            _nextAttackProfileIndex = 0;
        }

        public void OverrideCurrentWindup(float duration)
        {
            if (!_isWindingUp)
                return;

            _windupRemaining = Mathf.Max(0f, duration);
        }

        public void OverrideCurrentWindupFromClip(float clipLength)
        {
            if (!_isWindingUp)
                return;

            float duration = Mathf.Max(0f, clipLength);
            if (_currentAttackProfile != null)
            {
                if (_currentAttackProfile.UseAttackClipDamageMoment)
                    duration *= Mathf.Clamp01(_currentAttackProfile.AttackDamageMomentNormalized);
            }
            else if (_config != null && _config.UseAttackClipDamageMoment)
            {
                duration *= Mathf.Clamp01(_config.AttackDamageMomentNormalized);
            }

            OverrideCurrentWindup(duration);
        }

        public void CancelAttack()
        {
            _pendingTarget = null;
            _windupRemaining = 0f;
            _isWindingUp = false;
            _currentAttackProfile = null;
        }

        public bool TryAttack(Transform target)
        {
            if (target == null || _config == null || _isWindingUp || _cooldownRemaining > 0f)
                return false;

            _currentAttackProfile = SelectAttackProfile();
            _pendingTarget = target;
            _windupRemaining = CurrentAttackWindup();
            _isWindingUp = true;
            AttackStarted?.Invoke(this);

            if (_windupRemaining <= 0f)
                ResolveAttack();

            return true;
        }

        public void Tick(float deltaTime)
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
            _cooldownRemaining = CurrentAttackCooldown();
            AttackResolved?.Invoke(this);

            if (_config == null || _pendingTarget == null)
                return;

            var origin = _attackOrigin != null ? _attackOrigin : transform;
            if (TryDamagePendingTarget(origin))
                return;

            Vector3 center = origin.position + origin.forward * Mathf.Max(0f, CurrentAttackRange() * 0.5f);
            float radius = Mathf.Max(0.01f, CurrentAttackRadius());
            var hits = Physics.OverlapSphere(center, radius, _targetLayers, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits)
            {
                if (hit.transform.root == transform.root)
                    continue;

                if (!hit.transform.IsChildOf(_pendingTarget) && hit.transform.root != _pendingTarget.root)
                    continue;

                var receiver = hit.GetComponentInParent<IDamageReceiver>();
                if (receiver == null)
                    continue;

                var request = CreateDamageRequest();

                receiver.ReceiveDamage(request);
                return;
            }
        }

        private bool TryDamagePendingTarget(Transform origin)
        {
            Vector3 toTarget = _pendingTarget.position - origin.position;
            toTarget.y = 0f;

            float allowedDistance = Mathf.Max(0f, CurrentAttackRange()) + Mathf.Max(0f, CurrentAttackRadius());
            if (toTarget.sqrMagnitude > allowedDistance * allowedDistance)
                return false;

            var receiver = _pendingTarget.GetComponentInParent<IDamageReceiver>();
            if (receiver == null)
                receiver = _pendingTarget.GetComponentInChildren<IDamageReceiver>();

            if (receiver == null)
                return false;

            receiver.ReceiveDamage(CreateDamageRequest());
            return true;
        }

        private EnemyAttackProfile SelectAttackProfile()
        {
            if (_attackProfiles == null || _attackProfiles.Length == 0)
                return null;

            if (_attackSelectionMode == AttackSelectionMode.Random)
            {
                var validProfiles = new List<EnemyAttackProfile>();
                foreach (var profile in _attackProfiles)
                {
                    if (profile != null && profile.Enabled)
                        validProfiles.Add(profile);
                }

                return validProfiles.Count > 0 ? validProfiles[UnityEngine.Random.Range(0, validProfiles.Count)] : null;
            }

            for (int offset = 0; offset < _attackProfiles.Length; offset++)
            {
                int index = (_nextAttackProfileIndex + offset) % _attackProfiles.Length;
                var profile = _attackProfiles[index];
                if (profile == null || !profile.Enabled)
                    continue;

                _nextAttackProfileIndex = (index + 1) % _attackProfiles.Length;
                return profile;
            }

            return null;
        }

        private DamageRequest CreateDamageRequest()
        {
            if (_currentAttackProfile != null)
            {
                return new DamageRequest(
                    gameObject,
                    Mathf.Max(0f, _currentAttackProfile.HealthDamage),
                    Mathf.Max(0f, _currentAttackProfile.PoiseDamage),
                    _currentAttackProfile.DamageType);
            }

            return new DamageRequest(
                gameObject,
                _config.HealthDamage,
                _config.PoiseDamage,
                _config.DamageType);
        }

        private float CurrentAttackWindup()
        {
            if (_currentAttackProfile != null)
                return Mathf.Max(0f, _currentAttackProfile.AttackWindup);

            return _config != null ? Mathf.Max(0f, _config.AttackWindup) : 0.45f;
        }

        private float CurrentAttackCooldown()
        {
            if (_currentAttackProfile != null)
                return Mathf.Max(0f, _currentAttackProfile.AttackCooldown);

            return _config != null ? Mathf.Max(0f, _config.AttackCooldown) : 0f;
        }

        private float CurrentAttackRadius()
        {
            if (_currentAttackProfile != null)
                return Mathf.Max(0f, _currentAttackProfile.AttackRadius);

            return _config != null ? Mathf.Max(0f, _config.AttackRadius) : 0.55f;
        }

        private float CurrentAttackRange()
        {
            if (_currentAttackProfile != null)
                return Mathf.Max(0f, _currentAttackProfile.AttackRange);

            return _config != null ? Mathf.Max(0f, _config.AttackRange) : 1.7f;
        }
    }

    public enum AttackSelectionMode
    {
        Cycle,
        Random
    }

    [Serializable]
    public class EnemyAttackProfile
    {
        public bool Enabled = true;
        public string Id = "attack";
        public AnimationClip Clip;
        [Min(0f)] public float AttackCooldown = 2f;
        [Min(0f)] public float AttackWindup = 0.45f;
        public bool UseAttackClipDamageMoment = true;
        [Range(0f, 1f)] public float AttackDamageMomentNormalized = 0.5f;
        [Min(0f)] public float AttackRange = 1.7f;
        [Min(0f)] public float AttackRadius = 0.75f;
        [Min(0f)] public float HealthDamage = 10f;
        [Min(0f)] public float PoiseDamage = 8f;
        public DamageType DamageType = DamageType.Blunt;
    }
}
