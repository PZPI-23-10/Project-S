using Project_S.Runtime.Gameplay.Loot;
using Project_S.Runtime.Gameplay.Navigation;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyMeleeAttack))]
    [RequireComponent(typeof(GroundNavMeshMover))]
    [RequireComponent(typeof(EnemyController))]
    [RequireComponent(typeof(EnemyWorldHealthBar))]
    [RequireComponent(typeof(LootDropper))]
    [RequireComponent(typeof(EnemyAnimationController))]
    public class MinotaurBossSetup : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private LootTableData _lootTable;
        [SerializeField] private GameObject _visualPrefab;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _deathClip;
        [SerializeField] private AnimationClip _idleClip;
        [SerializeField] private AnimationClip _walkClip;
        [SerializeField] private AnimationClip[] _hitReactionClips;
        [SerializeField] private AttackSelectionMode _attackSelectionMode = AttackSelectionMode.Cycle;
        [SerializeField] private EnemyAttackProfile[] _attackProfiles;
        [SerializeField] private Vector3 _healthBarOffset = new Vector3(0f, 3.4f, 0f);

        private void Awake()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                QueueEditorVisualRefresh();
#endif
                return;
            }

            EnsureVisualInstance();
            Apply();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                QueueEditorVisualRefresh();
#endif
        }

        private void OnValidate()
        {
            ResolveVisualReferences();
            Apply();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                QueueEditorVisualRefresh();
#endif
        }

        public void Configure(
            EnemyConfig config,
            LootTableData lootTable,
            GameObject visualPrefab,
            Transform visualRoot,
            Animator animator,
            AnimationClip deathClip,
            AnimationClip idleClip,
            AnimationClip walkClip,
            AnimationClip[] hitReactionClips,
            EnemyAttackProfile[] attackProfiles,
            AttackSelectionMode attackSelectionMode,
            Vector3 healthBarOffset)
        {
            _config = config;
            _lootTable = lootTable;
            _visualPrefab = visualPrefab;
            _visualRoot = visualRoot;
            _animator = animator;
            _deathClip = deathClip;
            _idleClip = idleClip;
            _walkClip = walkClip;
            _hitReactionClips = hitReactionClips;
            _attackProfiles = attackProfiles;
            _attackSelectionMode = attackSelectionMode;
            _healthBarOffset = healthBarOffset;

            Apply();
        }

        private void Apply()
        {
            if (_config == null)
                return;

            ResolveVisualReferences();

            var health = GetComponent<EnemyHealth>();
            var attack = GetComponent<EnemyMeleeAttack>();
            var mover = GetComponent<GroundNavMeshMover>();
            var controller = GetComponent<EnemyController>();
            var healthBar = GetComponent<EnemyWorldHealthBar>();
            var lootDropper = GetComponent<LootDropper>();
            var animationController = GetComponent<EnemyAnimationController>();

            health.Configure(_config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(_config);
            attack.ConfigureAttackProfiles(_attackProfiles, _attackSelectionMode);
            mover.Configure(
                _config.MoveSpeed,
                Mathf.Max(0f, _config.AttackRange - _config.StoppingDistancePadding),
                _config.AgentRadius,
                _config.AgentHeight,
                _config.AgentBaseOffset,
                Mathf.Max(8f, _config.MoveSpeed * 4f),
                _config.RotationSpeed,
                _config.RepathInterval,
                30);
            controller.Configure(_config, null);
            healthBar.Configure(_config.DisplayName, _healthBarOffset);
            lootDropper.Configure(_lootTable);
            animationController.Configure(
                controller,
                attack,
                health,
                _visualRoot,
                _animator,
                _deathClip,
                _idleClip,
                _walkClip,
                FirstClip(_hitReactionClips),
                null);
            animationController.ConfigureHitReactionClips(_hitReactionClips);
        }

        private void EnsureVisualInstance()
        {
            ResolveVisualReferences();

            if (_visualPrefab == null)
                return;

            if (_visualRoot == null)
            {
                var visualRootObject = new GameObject("VisualRoot");
                visualRootObject.transform.SetParent(transform, false);
                _visualRoot = visualRootObject.transform;
            }

            if (_visualRoot.GetComponentInChildren<Animator>() != null)
            {
                ConfigureVisual();
                return;
            }

            var visual = InstantiateVisualPrefab(_visualRoot);
            visual.name = "minotaur1";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            ConfigureVisual();
        }

        private void ResolveVisualReferences()
        {
            if (_visualRoot == null)
                _visualRoot = transform.Find("VisualRoot");

            if (_animator == null && _visualRoot != null)
                _animator = _visualRoot.GetComponentInChildren<Animator>();

            ConfigureVisual();
        }

        private void ConfigureVisual()
        {
            if (_visualRoot == null)
                return;

            foreach (var collider in _visualRoot.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            if (_animator == null)
                _animator = _visualRoot.GetComponentInChildren<Animator>();

            if (_animator == null)
                return;

            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static AnimationClip FirstClip(AnimationClip[] clips)
        {
            if (clips == null)
                return null;

            foreach (var clip in clips)
            {
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private GameObject InstantiateVisualPrefab(Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var editorVisual = PrefabUtility.InstantiatePrefab(_visualPrefab, parent) as GameObject;
                if (editorVisual != null)
                    return editorVisual;
            }
#endif

            return Instantiate(_visualPrefab, parent);
        }

#if UNITY_EDITOR
        private void QueueEditorVisualRefresh()
        {
            EditorApplication.delayCall -= RefreshEditorVisual;
            EditorApplication.delayCall += RefreshEditorVisual;
        }

        private void RefreshEditorVisual()
        {
            if (this == null || Application.isPlaying)
                return;

            if (PrefabUtility.IsPartOfPrefabAsset(this))
                return;

            EnsureVisualInstance();
            Apply();
        }
#endif
    }
}
