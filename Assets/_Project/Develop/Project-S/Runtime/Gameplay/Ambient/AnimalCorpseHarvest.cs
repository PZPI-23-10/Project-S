using System;
using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Ambient
{
    public class AnimalCorpseHarvest : MonoBehaviour, IDamageReceiver
    {
        private const float GroundProbeHeight = 25f;
        private const float GroundProbeDistance = 80f;
        private const float ScriptedDeathPoseDuration = 0.5f;
        private const int GroundLayerMask = 1 << 8;

        [SerializeField] private EnemyHealth _health;
        [SerializeField] private GameObject _corpseRoot;
        [SerializeField] private Transform _poseRoot;
        [SerializeField] private Collider _hitbox;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _maxHealth = 40f;
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _healthPerBaseYield = 20f;
        [SerializeField] private float _corpseLifetimeSeconds = 300f;
        [SerializeField] private bool _scriptedDeathPose;
        [SerializeField] private bool _waitForExternalDeathPose;
        [SerializeField] private float _groundOffset = 0.04f;
        [SerializeField] private List<CorpseItemGrant> _baseYields = new List<CorpseItemGrant>();
        [SerializeField] private List<CorpseItemGrant> _completionDrops = new List<CorpseItemGrant>();
        [SerializeField] private int _soulAshReward;

        private float _baseYieldHealth;
        private bool _active;
        private bool _depleted;
        private Coroutine _deathPoseRoutine;
        private Transform _lockedPoseRoot;
        private Vector3 _lockedPosePosition;
        private Quaternion _lockedPoseRotation;
        private bool _lockFinalPose;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => Mathf.Max(1f, _maxHealth);
        public bool IsActive => _active;
        public bool IsDepleted => _depleted;
        public bool IsHitboxEnabled => _hitbox != null && _hitbox.enabled;
        public float CorpseLifetimeSeconds => _corpseLifetimeSeconds;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void LateUpdate()
        {
            if (!_lockFinalPose || _lockedPoseRoot == null)
                return;

            _lockedPoseRoot.SetPositionAndRotation(_lockedPosePosition, _lockedPoseRotation);
        }

        public void Configure(
            EnemyHealth health,
            GameObject corpseRoot,
            Transform poseRoot,
            float maxHealth,
            float healthPerBaseYield,
            IEnumerable<CorpseItemGrant> baseYields,
            IEnumerable<CorpseItemGrant> completionDrops,
            int soulAshReward,
            float corpseLifetimeSeconds,
            bool scriptedDeathPose,
            float groundOffset = 0.04f,
            bool waitForExternalDeathPose = false)
        {
            _health = health;
            _corpseRoot = corpseRoot;
            _poseRoot = poseRoot;
            _maxHealth = Mathf.Max(1f, maxHealth);
            _healthPerBaseYield = Mathf.Max(0f, healthPerBaseYield);
            _soulAshReward = Mathf.Max(0, soulAshReward);
            _corpseLifetimeSeconds = Mathf.Max(0f, corpseLifetimeSeconds);
            _scriptedDeathPose = scriptedDeathPose;
            _waitForExternalDeathPose = waitForExternalDeathPose;
            _groundOffset = Mathf.Max(0f, groundOffset);

            _baseYields = CopyGrants(baseYields);
            _completionDrops = CopyGrants(completionDrops);
            ResetCorpseState();
            EnsureReferences();

            if (isActiveAndEnabled && _health != null)
            {
                _health.Died -= OnDied;
                _health.Died += OnDied;
            }
        }

        public void ReceiveDamage(DamageRequest request)
        {
            if (!_active || _depleted)
                return;

            float damage = Mathf.Max(0f, request.HealthDamage);
            if (damage <= 0f)
                return;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            float healthRemoved = Mathf.Max(0f, previousHealth - _currentHealth);
            GrantBaseYield(healthRemoved, request.Source);

            Debug.Log($"[Animals] Harvested {name} corpse for {damage:F1} damage. HP: {_currentHealth:F1}/{MaxHealth:F1}", this);

            if (_currentHealth <= 0f)
                CompleteHarvest(request.Source);
        }

        public void ActivateCorpse()
        {
            if (_active)
                return;

            _active = true;
            _depleted = false;
            _currentHealth = MaxHealth;
            _baseYieldHealth = 0f;
            DisableLivingMovement();

            if (_scriptedDeathPose && _waitForExternalDeathPose)
                SetHitboxEnabled(false);
            else if (_scriptedDeathPose)
                StartScriptedDeathPose();
            else
                EnableCorpseHitbox();

            if (_corpseLifetimeSeconds > 0f && Application.isPlaying)
                Destroy(_corpseRoot != null ? _corpseRoot : gameObject, _corpseLifetimeSeconds);
        }

        private void OnDied(EnemyHealth health)
        {
            ActivateCorpse();
        }

        public void CompleteExternalDeathPose(bool applyScriptedPose = true)
        {
            if (!_active || _depleted || !_scriptedDeathPose)
                return;

            if (applyScriptedPose)
                StartScriptedDeathPose();
            else
                CompleteExternalDeathPoseWithoutScriptedPose();
        }

        private void GrantBaseYield(float healthRemoved, GameObject source)
        {
            if (_healthPerBaseYield <= 0f || healthRemoved <= 0f)
                return;

            _baseYieldHealth += healthRemoved;
            int thresholds = Mathf.FloorToInt(_baseYieldHealth / _healthPerBaseYield);
            if (thresholds <= 0)
                return;

            _baseYieldHealth -= thresholds * _healthPerBaseYield;

            for (int i = 0; i < _baseYields.Count; i++)
                GrantItem(_baseYields[i], thresholds, source);
        }

        private void CompleteHarvest(GameObject source)
        {
            if (_depleted)
                return;

            _depleted = true;
            _active = false;
            _lockFinalPose = false;

            var inventory = source != null ? source.GetComponentInParent<InventoryController>() : null;
            var wallet = ResolveWallet(source, inventory);

            if (wallet != null && _soulAshReward > 0)
                wallet.AddReward(_soulAshReward, source);

            for (int i = 0; i < _completionDrops.Count; i++)
                GrantItem(_completionDrops[i], 1, source, inventory);

            DestroyCorpseRoot();
        }

        private void GrantItem(CorpseItemGrant grant, int multiplier, GameObject source, InventoryController inventory = null)
        {
            if (grant == null || grant.Item == null || multiplier <= 0 || grant.Chance <= 0f)
                return;

            if (grant.Chance < 1f && UnityEngine.Random.value > grant.Chance)
                return;

            int amount = Mathf.Max(0, grant.Amount) * multiplier;
            if (amount <= 0)
                return;

            if (inventory == null && source != null)
                inventory = source.GetComponentInParent<InventoryController>();

            WorldItemDropUtility.GrantOrDrop(grant.Item, amount, inventory, transform.position, "[Animals]");
        }

        private static SoulAshWallet ResolveWallet(GameObject source, InventoryController inventory)
        {
            if (source != null)
            {
                var wallet = source.GetComponentInParent<SoulAshWallet>();
                if (wallet != null)
                    return wallet;
            }

            return inventory != null ? inventory.GetComponent<SoulAshWallet>() : null;
        }

        private void StartScriptedDeathPose()
        {
            if (_deathPoseRoutine != null)
                StopCoroutine(_deathPoseRoutine);

            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                ApplyFinalScriptedDeathPose();
                EnableCorpseHitbox();
                return;
            }

            _deathPoseRoutine = StartCoroutine(AnimateScriptedDeathPose());
        }

        private IEnumerator AnimateScriptedDeathPose()
        {
            Transform root = PoseTransform();
            if (root == null)
            {
                EnableCorpseHitbox();
                yield break;
            }

            StopPoseAnimator(root);

            Vector3 startPosition = root.position;
            Quaternion startRotation = root.rotation;
            Vector3 endPosition = SampleGround(startPosition) + Vector3.up * _groundOffset;
            Quaternion endRotation = Quaternion.AngleAxis(90f, root.forward) * root.rotation;
            float elapsed = 0f;

            while (elapsed < ScriptedDeathPoseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ScriptedDeathPoseDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                root.position = Vector3.Lerp(startPosition, endPosition, eased);
                root.rotation = Quaternion.Slerp(startRotation, endRotation, eased);
                yield return null;
            }

            root.position = endPosition;
            root.rotation = endRotation;
            LockFinalPose(root);
            _deathPoseRoutine = null;
            EnableCorpseHitbox();
        }

        private void ApplyFinalScriptedDeathPose()
        {
            Transform root = PoseTransform();
            if (root == null)
                return;

            StopPoseAnimator(root);
            Vector3 groundPosition = SampleGround(root.position);
            root.position = groundPosition + Vector3.up * _groundOffset;
            root.rotation = Quaternion.AngleAxis(90f, root.forward) * root.rotation;
            LockFinalPose(root);
        }

        private void CompleteExternalDeathPoseWithoutScriptedPose()
        {
            Transform root = PoseTransform();
            if (root != null)
            {
                StopPoseAnimator(root);
                Vector3 groundPosition = SampleGround(root.position);
                root.position = groundPosition + Vector3.up * _groundOffset;
                LockFinalPose(root);
            }

            EnableCorpseHitbox();
        }

        private Transform PoseTransform()
        {
            return _poseRoot != null ? _poseRoot : (_corpseRoot != null ? _corpseRoot.transform : transform);
        }

        private static void StopPoseAnimator(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>())
            {
                animator.speed = 0f;
                animator.applyRootMotion = false;
                animator.enabled = false;
            }
        }

        private void DisableLivingMovement()
        {
            if (_corpseRoot == null)
                return;

            var mover = _corpseRoot.GetComponent<GroundNavMeshMover>();
            if (mover != null)
            {
                mover.Stop();
                mover.enabled = false;
            }

            var agent = _corpseRoot.GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.updateUpAxis = false;
                agent.enabled = false;
            }
        }

        private void LockFinalPose(Transform root)
        {
            _lockedPoseRoot = root;
            _lockedPosePosition = root.position;
            _lockedPoseRotation = root.rotation;
            _lockFinalPose = true;
        }

        private void EnableCorpseHitbox()
        {
            if (_hitbox != null)
                _hitbox.isTrigger = true;

            SetHitboxEnabled(true);
        }

        private static Vector3 SampleGround(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeDistance, GroundLayerMask, QueryTriggerInteraction.Ignore))
                return hit.point;

            position.y = 0f;
            return position;
        }

        private void SetHitboxEnabled(bool enabled)
        {
            if (_hitbox == null)
                return;

            _hitbox.isTrigger = true;
            _hitbox.enabled = enabled;
        }

        private void DestroyCorpseRoot()
        {
            var target = _corpseRoot != null ? _corpseRoot : gameObject;
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void ResetCorpseState()
        {
            _active = false;
            _depleted = false;
            _baseYieldHealth = 0f;
            _currentHealth = MaxHealth;
            _lockFinalPose = false;
            _lockedPoseRoot = null;
        }

        private void EnsureReferences()
        {
            if (_corpseRoot == null)
                _corpseRoot = transform.root != null ? transform.root.gameObject : gameObject;

            if (_health == null)
                _health = _corpseRoot.GetComponent<EnemyHealth>();

            if (_hitbox == null)
                _hitbox = GetComponent<Collider>();

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody>();

            if (_rigidbody == null)
                _rigidbody = gameObject.AddComponent<Rigidbody>();

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        private static List<CorpseItemGrant> CopyGrants(IEnumerable<CorpseItemGrant> grants)
        {
            var result = new List<CorpseItemGrant>();
            if (grants == null)
                return result;

            foreach (var grant in grants)
            {
                if (grant == null)
                    continue;

                result.Add(new CorpseItemGrant
                {
                    Item = grant.Item,
                    Amount = grant.Amount,
                    Chance = grant.Chance
                });
            }

            return result;
        }
    }

    [Serializable]
    public class CorpseItemGrant
    {
        public ItemData Item;
        [Min(0)] public int Amount = 1;
        [Range(0f, 1f)] public float Chance = 1f;
    }
}
