using System;
using Project_S.Runtime.Gameplay.Ambient;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Loot;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Spawning
{
    public enum MobSpawnBehaviour
    {
        ConfiguredPrefab,
        GroundEnemy,
        FlyingEnemy,
        NeutralAnimal,
        RetaliatingBoar,
        AmbientSparrow
    }

    [CreateAssetMenu(fileName = "New Mob Spawn Definition", menuName = "Project-S/Spawning/Mob Spawn Definition")]
    public class MobSpawnDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Mob";
        [SerializeField] private MobSpawnBehaviour _behaviour = MobSpawnBehaviour.ConfiguredPrefab;

        [Header("Prefab")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private bool _usePrefabAsRoot = true;
        [SerializeField] private Vector3 _visualLocalPosition = Vector3.zero;
        [SerializeField] private Vector3 _visualLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 _visualLocalScale = Vector3.one;
        [SerializeField] private bool _disablePrefabBehaviours = false;
        [SerializeField] private bool _disablePrefabColliders = false;

        [Header("Combat")]
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField] private LootTableData _lootTable;
        [SerializeField] private EnemyAttackProfile[] _attackProfiles = Array.Empty<EnemyAttackProfile>();
        [SerializeField] private AttackSelectionMode _attackSelectionMode = AttackSelectionMode.Cycle;

        [Header("Animation")]
        [SerializeField] private AnimationClip _idleClip;
        [SerializeField] private AnimationClip _walkClip;
        [SerializeField] private AnimationClip _attackClip;
        [SerializeField] private AnimationClip _deathClip;
        [SerializeField] private AnimationClip[] _hitReactionClips = Array.Empty<AnimationClip>();
        [SerializeField] private RuntimeAnimatorController _animatorController;

        [Header("Hitbox")]
        [SerializeField] private float _colliderRadius = 0.45f;
        [SerializeField] private float _colliderHeight = 1.8f;
        [SerializeField] private Vector3 _colliderCenter = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private Vector3 _healthBarOffset = new Vector3(0f, 2f, 0f);

        [Header("Animal Movement")]
        [SerializeField] private float _walkSpeed = 1.1f;
        [SerializeField] private float _runSpeed = 4.5f;
        [SerializeField] private float _scareRadius = 6f;

        [Header("Flying Movement")]
        [SerializeField] private float _hoverHeight = 12f;
        [SerializeField] private float _diveStopHeight = 1.2f;
        [SerializeField] private float _retreatDistanceThreshold = 0.35f;

        [Header("Lifecycle")]
        [SerializeField] private bool _destroyAfterDeath = false;
        [SerializeField] private float _slotReleaseDelayAfterDeath = 3f;

        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? name : _displayName;
        public GameObject Prefab => _prefab;
        public MobSpawnBehaviour Behaviour => _behaviour;
        public EnemyConfig EnemyConfig => _enemyConfig;
        public float SlotReleaseDelayAfterDeath => Mathf.Max(0f, _slotReleaseDelayAfterDeath);
        public float SpawnClearanceRadius => Mathf.Max(0.1f, _colliderRadius);

#if UNITY_EDITOR
        public void ConfigureExistingPrefab(string displayName, GameObject prefab, float spawnClearanceRadius, float slotReleaseDelayAfterDeath)
        {
            _displayName = displayName;
            _behaviour = MobSpawnBehaviour.ConfiguredPrefab;
            _prefab = prefab;
            _usePrefabAsRoot = true;
            _disablePrefabBehaviours = false;
            _disablePrefabColliders = false;
            _enemyConfig = null;
            _colliderRadius = Mathf.Max(0.1f, spawnClearanceRadius);
            _slotReleaseDelayAfterDeath = Mathf.Max(0f, slotReleaseDelayAfterDeath);
        }
#endif

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent, Transform player, Vector3 homeCenter, float homeRadius)
        {
            if (_prefab == null)
                return null;

            GameObject mob = CreateMobObject(position, rotation, parent);
            if (mob == null)
                return null;

            mob.name = DisplayName;
            if (_behaviour != MobSpawnBehaviour.ConfiguredPrefab || !_usePrefabAsRoot)
                ConfigureCommonVisuals(mob);

            switch (_behaviour)
            {
                case MobSpawnBehaviour.GroundEnemy:
                    ConfigureGroundEnemy(mob, player, homeCenter, homeRadius);
                    break;
                case MobSpawnBehaviour.FlyingEnemy:
                    ConfigureFlyingEnemy(mob, player, homeCenter, homeRadius);
                    break;
                case MobSpawnBehaviour.NeutralAnimal:
                    ConfigureNeutralAnimal(mob, player, homeCenter, homeRadius);
                    break;
                case MobSpawnBehaviour.RetaliatingBoar:
                    ConfigureRetaliatingBoar(mob, player, homeCenter, homeRadius);
                    break;
                case MobSpawnBehaviour.AmbientSparrow:
                    ConfigureSparrow(mob, player, homeCenter, homeRadius);
                    break;
                default:
                    ConfigureExistingComponents(mob, player, homeCenter, homeRadius);
                    break;
            }

            return mob;
        }

        private GameObject CreateMobObject(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (_usePrefabAsRoot)
                return Instantiate(_prefab, position, rotation, parent);

            var root = new GameObject(DisplayName);
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(position, rotation);

            var visual = Instantiate(_prefab, root.transform);
            visual.name = "VisualRoot";
            visual.transform.localPosition = _visualLocalPosition;
            visual.transform.localRotation = Quaternion.Euler(_visualLocalEulerAngles);
            visual.transform.localScale = _visualLocalScale;
            return root;
        }

        private void ConfigureCommonVisuals(GameObject mob)
        {
            if (_disablePrefabBehaviours)
            {
                foreach (var behaviour in mob.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.gameObject == mob)
                        continue;

                    behaviour.enabled = false;
                }
            }

            if (_disablePrefabColliders)
            {
                foreach (var collider in mob.GetComponentsInChildren<Collider>())
                {
                    if (collider.gameObject == mob)
                        continue;

                    collider.enabled = false;
                }
            }

            var animator = mob.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                if (_animatorController != null)
                    animator.runtimeAnimatorController = _animatorController;
            }
        }

        private void ConfigureGroundEnemy(GameObject mob, Transform player, Vector3 homeCenter, float homeRadius)
        {
            var config = ResolveConfig();
            ConfigureHitbox(mob, config);
            ConfigureRigidbody(mob);

            var health = Ensure<EnemyHealth>(mob);
            var meleeAttack = Ensure<EnemyMeleeAttack>(mob);
            var mover = Ensure<GroundNavMeshMover>(mob);
            var controller = Ensure<EnemyController>(mob);
            var healthBar = Ensure<EnemyWorldHealthBar>(mob);
            var animationController = Ensure<EnemyAnimationController>(mob);

            ConfigureLoot(mob);
            health.Configure(config);
            health.SetDestroyAfterDeath(_destroyAfterDeath);
            meleeAttack.Configure(config);
            meleeAttack.ConfigureAttackProfiles(_attackProfiles, _attackSelectionMode);
            mover.Configure(config.MoveSpeed, Mathf.Max(0f, config.AttackRange - config.StoppingDistancePadding), config.AgentRadius, config.AgentHeight, config.AgentBaseOffset, Mathf.Max(8f, config.MoveSpeed * 4f), config.RotationSpeed, config.RepathInterval, 50);
            mover.TryWarpToNearestNavMesh(Mathf.Max(2f, homeRadius));
            controller.Configure(config, player);
            controller.ConfigureHomeArea(homeCenter, homeRadius, true);

            animationController.Configure(controller, meleeAttack, health, ResolveVisualRoot(mob), mob.GetComponentInChildren<Animator>(), _deathClip, _idleClip, _walkClip, FirstClip(_hitReactionClips), _attackClip);
            animationController.ConfigureHitReactionClips(_hitReactionClips);
            healthBar.Configure(DisplayName, _healthBarOffset);
        }

        private void ConfigureFlyingEnemy(GameObject mob, Transform player, Vector3 homeCenter, float homeRadius)
        {
            var config = ResolveConfig();
            ConfigureRigidbody(mob);

            var collider = mob.GetComponent<SphereCollider>();
            if (collider == null)
                collider = mob.AddComponent<SphereCollider>();
            collider.radius = Mathf.Max(0.1f, _colliderRadius);
            collider.center = _colliderCenter;

            var health = Ensure<EnemyHealth>(mob);
            var attack = Ensure<EnemyMeleeAttack>(mob);
            var controller = Ensure<FlyingEnemyController>(mob);
            var healthBar = Ensure<EnemyWorldHealthBar>(mob);
            var animationController = Ensure<HarpyAnimationController>(mob);

            ConfigureLoot(mob);
            health.Configure(config);
            health.SetDestroyAfterDeath(_destroyAfterDeath);
            attack.Configure(config);
            controller.Configure(config, player, _hoverHeight, Mathf.Max(0.5f, homeRadius), _diveStopHeight, _retreatDistanceThreshold);
            controller.ConfigureHomeArea(homeCenter, Mathf.Max(0.5f, homeRadius));
            animationController.Configure(controller, attack, health, mob.GetComponentInChildren<Animator>());
            healthBar.Configure(DisplayName, _healthBarOffset);
        }

        private void ConfigureNeutralAnimal(GameObject mob, Transform player, Vector3 homeCenter, float homeRadius)
        {
            var config = ResolveConfig();
            ConfigureHitbox(mob, config);
            ConfigureRigidbody(mob);

            var health = Ensure<EnemyHealth>(mob);
            var mover = Ensure<GroundNavMeshMover>(mob);
            var controller = Ensure<NeutralAnimalController>(mob);
            var healthBar = Ensure<EnemyWorldHealthBar>(mob);

            health.Configure(config);
            health.SetDestroyAfterDeath(_destroyAfterDeath);
            mover.Configure(_walkSpeed, 0.15f, _colliderRadius, _colliderHeight, 0f, Mathf.Max(8f, _runSpeed * 4f), 540f, 0.25f, 60);
            mover.TryWarpToNearestNavMesh(Mathf.Max(2f, homeRadius));
            controller.Configure(player, health, homeCenter, homeRadius, _walkSpeed, _runSpeed, _scareRadius);
            healthBar.Configure(DisplayName, _healthBarOffset);
        }

        private void ConfigureRetaliatingBoar(GameObject mob, Transform player, Vector3 homeCenter, float homeRadius)
        {
            var config = ResolveConfig();
            ConfigureHitbox(mob, config);
            ConfigureRigidbody(mob);

            var health = Ensure<EnemyHealth>(mob);
            var attack = Ensure<EnemyMeleeAttack>(mob);
            var mover = Ensure<GroundNavMeshMover>(mob);
            var controller = Ensure<RetaliatingBoarController>(mob);
            var healthBar = Ensure<EnemyWorldHealthBar>(mob);

            health.Configure(config);
            health.SetDestroyAfterDeath(_destroyAfterDeath);
            attack.Configure(config);
            mover.Configure(_walkSpeed, Mathf.Max(0.1f, config.AttackRange - 0.05f), _colliderRadius, _colliderHeight, 0f, Mathf.Max(10f, _runSpeed * 4f), config.RotationSpeed, config.RepathInterval, 45);
            mover.TryWarpToNearestNavMesh(Mathf.Max(2f, homeRadius));
            controller.Configure(player, health, attack, homeCenter, homeRadius, _walkSpeed, _runSpeed, config.AttackRange);
            healthBar.Configure(DisplayName, _healthBarOffset);
        }

        private void ConfigureSparrow(GameObject mob, Transform player, Vector3 homeCenter, float homeRadius)
        {
            foreach (var collider in mob.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            if (mob.GetComponent<NavMeshAgent>() == null)
                mob.AddComponent<NavMeshAgent>();

            var mover = Ensure<GroundNavMeshMover>(mob);
            mover.Configure(_walkSpeed, 0.12f, Mathf.Max(0.01f, _colliderRadius), 0.35f, 0f, Mathf.Max(8f, _walkSpeed * 4f), 540f, 0.25f, 70);
            mover.TryWarpToNearestNavMesh(Mathf.Max(2f, homeRadius));

            var controller = Ensure<SparrowAmbientController>(mob);
            controller.Configure(player, homeCenter, homeRadius, _walkSpeed, _runSpeed, _scareRadius, 6f, 10f);
        }

        private void ConfigureExistingComponents(GameObject mob, Transform player, Vector3 homeCenter, float homeRadius)
        {
            StabilizeExistingNavMeshSpawn(mob, homeRadius);

            var health = mob.GetComponent<EnemyHealth>();
            if (health != null)
            {
                if (_enemyConfig != null)
                    health.Configure(_enemyConfig);

                health.SetDestroyAfterDeath(_destroyAfterDeath);
            }

            var meleeAttack = mob.GetComponent<EnemyMeleeAttack>();
            if (meleeAttack != null && _enemyConfig != null)
                meleeAttack.Configure(_enemyConfig);

            var rangedAttack = mob.GetComponent<EnemyRangedAttack>();
            if (rangedAttack != null && _enemyConfig != null)
                rangedAttack.Configure(_enemyConfig, _attackClip);

            var controller = mob.GetComponent<EnemyController>();
            if (controller != null)
            {
                if (_enemyConfig != null)
                    controller.Configure(_enemyConfig, player);

                controller.ConfigureHomeArea(homeCenter, homeRadius, true);
            }

            var flyingController = mob.GetComponent<FlyingEnemyController>();
            if (flyingController != null)
            {
                if (_enemyConfig != null)
                    flyingController.Configure(_enemyConfig, player, _hoverHeight, Mathf.Max(0.5f, homeRadius), _diveStopHeight, _retreatDistanceThreshold);

                flyingController.ConfigureHomeArea(homeCenter, Mathf.Max(0.5f, homeRadius));
            }

            var neutralAnimal = mob.GetComponent<NeutralAnimalController>();
            if (neutralAnimal != null)
                neutralAnimal.ConfigureSpawnContext(player, homeCenter, homeRadius);

            var boar = mob.GetComponent<RetaliatingBoarController>();
            if (boar != null)
                boar.ConfigureSpawnContext(player, homeCenter, homeRadius);

            var sparrow = mob.GetComponent<SparrowAmbientController>();
            if (sparrow != null)
                sparrow.Configure(player, homeCenter, homeRadius, _walkSpeed, _runSpeed, _scareRadius, 6f, 10f);

            ConfigureLoot(mob);
        }

        private static void StabilizeExistingNavMeshSpawn(GameObject mob, float searchRadius)
        {
            if (mob == null)
                return;

            float radius = Mathf.Max(2f, searchRadius);
            var mover = mob.GetComponent<GroundNavMeshMover>();
            if (mover != null && mover.TryWarpToNearestNavMesh(radius))
                return;

            var agent = mob.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
                return;

            Vector3 sampleOrigin = GroundPositionSampler.SampleNavMeshNearGround(mob.transform.position, radius);
            if (!NavMesh.SamplePosition(sampleOrigin, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return;

            agent.Warp(hit.position);
        }

        private EnemyConfig ResolveConfig()
        {
            if (_enemyConfig != null)
                return _enemyConfig;

            var config = CreateInstance<EnemyConfig>();
            config.DisplayName = DisplayName;
            config.AgentRadius = Mathf.Max(0.01f, _colliderRadius);
            config.AgentHeight = Mathf.Max(0.01f, _colliderHeight);
            return config;
        }

        private void ConfigureHitbox(GameObject mob, EnemyConfig config)
        {
            foreach (var collider in mob.GetComponents<Collider>())
                Destroy(collider);

            var capsule = mob.AddComponent<CapsuleCollider>();
            capsule.radius = Mathf.Max(0.01f, _colliderRadius);
            capsule.height = Mathf.Max(0.01f, _colliderHeight);
            capsule.center = _colliderCenter;
            capsule.direction = 1;

            var agent = mob.GetComponent<NavMeshAgent>();
            if (agent == null)
                agent = mob.AddComponent<NavMeshAgent>();

            agent.radius = config != null ? Mathf.Max(0.01f, config.AgentRadius) : Mathf.Max(0.01f, _colliderRadius);
            agent.height = config != null ? Mathf.Max(0.01f, config.AgentHeight) : Mathf.Max(0.01f, _colliderHeight);
            agent.baseOffset = config != null ? config.AgentBaseOffset : 0f;
        }

        private static void ConfigureRigidbody(GameObject mob)
        {
            var rigidbody = mob.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = mob.AddComponent<Rigidbody>();

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        private void ConfigureLoot(GameObject mob)
        {
            if (_lootTable == null)
                return;

            var lootDropper = Ensure<LootDropper>(mob);
            lootDropper.Configure(_lootTable);
        }

        private static T Ensure<T>(GameObject mob) where T : Component
        {
            var component = mob.GetComponent<T>();
            return component != null ? component : mob.AddComponent<T>();
        }

        private static Transform ResolveVisualRoot(GameObject mob)
        {
            var visualRoot = mob.transform.Find("VisualRoot");
            if (visualRoot != null)
                return visualRoot;

            var animator = mob.GetComponentInChildren<Animator>();
            return animator != null ? animator.transform : mob.transform;
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
    }
}
