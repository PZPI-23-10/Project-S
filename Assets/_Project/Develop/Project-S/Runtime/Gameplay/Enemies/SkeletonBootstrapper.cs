using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Ambient;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Diagnostics;
using Project_S.Runtime.Gameplay.Loot;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project_S.Runtime.Gameplay.Enemies
{
    public static class SkeletonBootstrapper
    {
        private const string RunnerName = "[MVP] Dungeon Skeleton Bootstrapper";
        private const string RootName = "[MVP] Enemies";
        private const string ResourcesRoot = "Enemies/DungeonSkeletons";
        private const string AssetRoot = "Assets/DungeonCharacters/Skeletons";
        private static readonly bool AutoSpawnEnabled = false;
        private const float VisualScale = 0.82f;
        private const float AgentRadius = 0.42f;
        private const float AgentHeight = 1.75f;
        private const float CorpseLifetimeSeconds = 300f;
        private const int SoulAshReward = 15;
        private const string BonePath = "Crafting/Items/Resources/Bone";
        private static readonly Vector3 HealthBarOffset = new Vector3(0f, 1.95f, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!AutoSpawnEnabled)
                return;

            if (GameObject.Find(RunnerName) != null)
                return;

            var runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<SkeletonBootstrapRunner>();
        }

        private static bool TrySpawn()
        {
            var variants = CreateVariants();
            bool allSpawned = true;
            foreach (var variant in variants)
                allSpawned &= GameObject.Find(variant.ObjectName) != null;

            if (allSpawned)
                return true;

            var player = Object.FindFirstObjectByType<PlayerFacade>();
            if (player == null)
                return false;

            return NpcStartupDiagnostics.Time("Dungeon skeleton bootstrap spawn", () => TrySpawnWithPlayer(player, variants));
        }

        private static bool TrySpawnWithPlayer(PlayerFacade player, SkeletonVariant[] variants)
        {
            var root = GameObject.Find(RootName);
            if (root == null)
                root = new GameObject(RootName);

            foreach (var variant in variants)
            {
                if (GameObject.Find(variant.ObjectName) != null)
                    continue;

                Vector3 spawnPosition = player.transform.position
                    + player.transform.forward * variant.SpawnOffset.z
                    + player.transform.right * variant.SpawnOffset.x
                    + Vector3.up * variant.SpawnOffset.y;

                SpawnSkeleton(root.transform, spawnPosition, player.transform, variant);
            }

            return true;
        }

        private static SkeletonVariant[] CreateVariants()
        {
            return new[]
            {
                new SkeletonVariant(
                    "[MVP] Skeleton Grunt",
                    "Skeleton Grunt",
                    "Skeleton Grunt",
                    "Skeleton_A",
                    "1_one_handed",
                    "DS_onehand",
                    new Vector3(-3.2f, 0.85f, 7f),
                    52f,
                    14,
                    2.35f,
                    10f,
                    15f,
                    1.75f,
                    1.65f,
                    0.42f,
                    13f,
                    8f,
                    DamageType.Slashing,
                    false),
                new SkeletonVariant(
                    "[MVP] Skeleton Guard",
                    "Skeleton Guard",
                    "Skeleton Guard",
                    "Skeleton_B",
                    "2_shield",
                    "DS_shield",
                    new Vector3(0f, 0.85f, 8.5f),
                    72f,
                    18,
                    1.9f,
                    9f,
                    14f,
                    1.8f,
                    2.1f,
                    0.5f,
                    11f,
                    12f,
                    DamageType.Blunt,
                    false),
                new SkeletonVariant(
                    "[MVP] Skeleton Brute",
                    "Skeleton Brute",
                    "Skeleton Brute",
                    "Skeleton_TwoHanded_A",
                    "3_two_handed",
                    "DS_twohanded",
                    new Vector3(3.2f, 0.85f, 7f),
                    95f,
                    24,
                    1.75f,
                    11f,
                    16f,
                    2.05f,
                    2.8f,
                    0.68f,
                    22f,
                    18f,
                    DamageType.Blunt,
                    false),
                new SkeletonVariant(
                    "[MVP] Skeleton Archer",
                    "Skeleton Archer",
                    "Skeleton Archer",
                    "Skeleton_archer_A",
                    "4_bow",
                    "DS_bow",
                    new Vector3(5.5f, 0.85f, 11f),
                    42f,
                    20,
                    2.25f,
                    15f,
                    22f,
                    1.6f,
                    2.3f,
                    0.55f,
                    18f,
                    16f,
                    DamageType.Piercing,
                    true)
            };
        }

        private static void SpawnSkeleton(Transform parent, Vector3 position, Transform target, SkeletonVariant variant)
        {
            var config = CreateConfig(variant);
            var skeleton = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skeleton.name = variant.ObjectName;
            skeleton.transform.SetParent(parent);
            skeleton.transform.position = GroundPositionSampler.SampleNavMeshNearGround(position, 5f);
            skeleton.transform.localScale = Vector3.one;

            if (NavMesh.SamplePosition(skeleton.transform.position, out NavMeshHit navMeshHit, 0.4f, NavMesh.AllAreas))
                skeleton.transform.position = navMeshHit.position;
            else
                Debug.LogWarning($"[Skeleton] Spawn position for '{variant.DisplayName}' is not on the runtime navmesh.");

            var renderer = skeleton.GetComponent<Renderer>();
            ConfigureHitbox(skeleton, config);

            var visual = TryAttachVisual(skeleton.transform, variant);
            if (visual == null && renderer != null)
                renderer.material.color = Color.white;
            else if (renderer != null)
                renderer.enabled = false;

            var rigidbody = skeleton.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = skeleton.AddComponent<EnemyHealth>();
            var meleeAttack = skeleton.AddComponent<EnemyMeleeAttack>();
            var mover = skeleton.AddComponent<GroundNavMeshMover>();
            var controller = skeleton.AddComponent<EnemyController>();
            var worldHealthBar = skeleton.AddComponent<EnemyWorldHealthBar>();
            var lootDropper = skeleton.AddComponent<LootDropper>();
            var animationController = skeleton.AddComponent<EnemyAnimationController>();
            EnemyRangedAttack rangedAttack = null;

            var animationSet = LoadAnimationSet(variant);
            lootDropper.Configure(CreateSkeletonLootTable());

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            meleeAttack.Configure(config);
            meleeAttack.ConfigureAttackProfiles(CreateMeleeProfiles(variant, animationSet.AttackClips), AttackSelectionMode.Cycle);

            if (variant.IsRanged)
            {
                rangedAttack = skeleton.AddComponent<EnemyRangedAttack>();
                rangedAttack.Configure(config, FirstClip(animationSet.AttackClips), visual != null ? visual.transform : skeleton.transform);
            }

            mover.Configure(
                config.MoveSpeed,
                Mathf.Max(0f, config.AttackRange - config.StoppingDistancePadding),
                config.AgentRadius,
                config.AgentHeight,
                config.AgentBaseOffset,
                Mathf.Max(8f, config.MoveSpeed * 4f),
                config.RotationSpeed,
                config.RepathInterval,
                variant.IsRanged ? 45 : 50);
            mover.TryWarpToNearestNavMesh(5f);

            controller.Configure(config, target);
            animationController.Configure(
                controller,
                meleeAttack,
                health,
                visual != null ? visual.transform : null,
                visual != null ? visual.GetComponentInChildren<Animator>() : null,
                FirstClip(animationSet.DeathClips),
                animationSet.IdleClip,
                animationSet.WalkClip,
                FirstClip(animationSet.DamageClips),
                FirstClip(animationSet.AttackClips));
            animationController.ConfigureHitReactionClips(animationSet.DamageClips);
            animationController.ConfigureRangedAttack(rangedAttack, FirstClip(animationSet.AttackClips));

            CreateSkeletonCorpseHarvest(skeleton, health, visual != null ? visual.transform : skeleton.transform, variant);
            worldHealthBar.Configure(variant.DisplayName, HealthBarOffset);
        }

        private static EnemyConfig CreateConfig(SkeletonVariant variant)
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.DisplayName = variant.DisplayName;
            config.MaxHealth = variant.MaxHealth;
            config.SoulAshReward = variant.SoulAshReward;
            config.MoveSpeed = variant.MoveSpeed;
            config.AggroRange = variant.AggroRange;
            config.LoseTargetRange = variant.LoseTargetRange;
            config.AttackRange = variant.AttackRange;
            config.RotationSpeed = 540f;
            config.AgentRadius = AgentRadius;
            config.AgentHeight = AgentHeight;
            config.AgentBaseOffset = 0f;
            config.StoppingDistancePadding = 0.05f;
            config.RepathInterval = 0.2f;
            config.AttackCooldown = variant.AttackCooldown;
            config.AttackWindup = variant.AttackWindup;
            config.UseAttackClipDamageMoment = true;
            config.AttackDamageMomentNormalized = 0.45f;
            config.AttackRadius = 0.7f;
            config.HealthDamage = variant.HealthDamage;
            config.PoiseDamage = variant.PoiseDamage;
            config.DamageType = variant.DamageType;
            config.DestroyDelayAfterDeath = 60f;

            if (variant.IsRanged)
            {
                config.UseRangedAttack = true;
                config.RangedAttackRange = 12f;
                config.RangedPreferredDistance = 8.5f;
                config.RangedRetreatDistance = 4.5f;
                config.RangedAttackCooldown = 2.6f;
                config.RangedAttackWindup = 0.55f;
                config.RangedAttackDamageMomentNormalized = 0.58f;
                config.RangedProjectileSpeed = 30f;
                config.RangedProjectileLifetime = 4f;
                config.RangedProjectileRadius = 0.1f;
                config.RangedHealthDamage = variant.HealthDamage;
                config.RangedPoiseDamage = variant.PoiseDamage;
                config.RangedDamageType = DamageType.Piercing;
            }

            return config;
        }

        private static EnemyAttackProfile[] CreateMeleeProfiles(SkeletonVariant variant, AnimationClip[] attackClips)
        {
            if (variant.IsRanged || attackClips == null || attackClips.Length == 0)
                return System.Array.Empty<EnemyAttackProfile>();

            var profiles = new List<EnemyAttackProfile>();
            foreach (var clip in attackClips)
            {
                if (clip == null)
                    continue;

                profiles.Add(new EnemyAttackProfile
                {
                    Id = clip.name,
                    Clip = clip,
                    AnimationSpeed = 1f,
                    AttackCooldown = variant.AttackCooldown,
                    AttackWindup = variant.AttackWindup,
                    UseAttackClipDamageMoment = true,
                    AttackDamageMomentNormalized = 0.45f,
                    AttackRange = variant.AttackRange,
                    AttackRadius = 0.75f,
                    HealthDamage = variant.HealthDamage,
                    PoiseDamage = variant.PoiseDamage,
                    DamageType = variant.DamageType
                });
            }

            return profiles.ToArray();
        }

        private static void ConfigureHitbox(GameObject skeleton, EnemyConfig config)
        {
            foreach (var collider in skeleton.GetComponents<Collider>())
                Object.Destroy(collider);

            var hitbox = skeleton.AddComponent<CapsuleCollider>();
            hitbox.radius = config.AgentRadius;
            hitbox.height = config.AgentHeight;
            hitbox.center = new Vector3(0f, config.AgentHeight * 0.5f, 0f);
            hitbox.direction = 1;
        }

        private static GameObject TryAttachVisual(Transform parent, SkeletonVariant variant)
        {
            var visualPrefab = LoadAsset<GameObject>(
                "DungeonSkeleton",
                $"{ResourcesRoot}/Prefabs/{variant.VisualPrefabName}",
                $"{AssetRoot}/Prefabs/{variant.VisualPrefabName}.prefab");
            if (visualPrefab == null)
                return null;

            var visual = Object.Instantiate(visualPrefab, parent);
            visual.name = "VisualRoot";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * VisualScale;

            foreach (var collider in visual.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            return visual;
        }

        private static SkeletonAnimationSet LoadAnimationSet(SkeletonVariant variant)
        {
            return new SkeletonAnimationSet
            {
                IdleClip = LoadClip(variant, $"{variant.ClipPrefix}_idle_A", "Idle"),
                WalkClip = LoadClip(variant, $"{variant.ClipPrefix}_walk", "Walk"),
                AttackClips = LoadClips(variant, "attack"),
                DamageClips = LoadClips(variant, "damage"),
                DeathClips = LoadClips(variant, "death")
            };
        }

        private static AnimationClip LoadClip(SkeletonVariant variant, string clipFileName, string label)
        {
            var clips = LoadAllClips(variant, clipFileName);
            var clip = FirstClip(clips);
            if (clip == null)
                Debug.LogWarning($"[Skeleton] {label} animation '{clipFileName}' was not found for '{variant.DisplayName}'.");

            return clip;
        }

        private static AnimationClip[] LoadClips(SkeletonVariant variant, string group)
        {
            var clips = new List<AnimationClip>();
            string[] suffixes = { "A", "B", "C" };
            foreach (string suffix in suffixes)
            {
                var clip = FirstClip(LoadAllClips(variant, $"{variant.ClipPrefix}_{group}_{suffix}"));
                if (clip != null)
                    clips.Add(clip);
            }

            return clips.ToArray();
        }

        private static AnimationClip[] LoadAllClips(SkeletonVariant variant, string clipFileName)
        {
            string resourcePath = $"{ResourcesRoot}/Animation/{variant.AnimationFolder}/{clipFileName}";
            var clips = NpcStartupDiagnostics.LoadAllResources<AnimationClip>("DungeonSkeleton", resourcePath);
            if (clips != null && clips.Length > 0)
                return ValidClips(clips);

#if UNITY_EDITOR
            string assetPath = $"{AssetRoot}/Animation/{variant.AnimationFolder}/{clipFileName}.FBX";
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var editorClips = new List<AnimationClip>();
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    editorClips.Add(clip);
            }

            return editorClips.ToArray();
#else
            return System.Array.Empty<AnimationClip>();
#endif
        }

        private static AnimationClip[] ValidClips(AnimationClip[] clips)
        {
            var valid = new List<AnimationClip>();
            foreach (var clip in clips)
            {
                if (clip != null && !clip.name.StartsWith("__preview__"))
                    valid.Add(clip);
            }

            return valid.ToArray();
        }

        private static T LoadAsset<T>(string owner, string resourcePath, string editorAssetPath) where T : Object
        {
            var asset = NpcStartupDiagnostics.LoadResource<T>(owner, resourcePath);
            if (asset != null)
                return asset;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<T>(editorAssetPath);
#else
            return null;
#endif
        }

        private static LootTableData CreateSkeletonLootTable()
        {
            var bone = NpcStartupDiagnostics.LoadResource<ItemData>("SkeletonLoot", BonePath);
            if (bone == null)
            {
                Debug.LogWarning($"[Skeleton] Loot item '{BonePath}' was not found.");
                return null;
            }

            var lootTable = ScriptableObject.CreateInstance<LootTableData>();
            lootTable.SoulAshReward = SoulAshReward;
            lootTable.GuaranteedDrops.Add(new LootItemDrop
            {
                Item = bone,
                MinAmount = 1,
                MaxAmount = 1,
                Chance = 1f
            });

            return lootTable;
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

        private static void CreateSkeletonCorpseHarvest(GameObject skeleton, EnemyHealth health, Transform poseRoot, SkeletonVariant variant)
        {
            var baseYields = new List<CorpseItemGrant>();
            var completionDrops = new List<CorpseItemGrant>();

            AddGrant(baseYields, BonePath, 1, 1f);

            var corpse = skeleton.AddComponent<AnimalCorpseHarvest>();
            corpse.Configure(
                health,
                skeleton,
                poseRoot,
                variant.CorpseHealth,
                20f,
                baseYields,
                completionDrops,
                0,
                CorpseLifetimeSeconds,
                scriptedDeathPose: false);
        }

        private static void AddGrant(ICollection<CorpseItemGrant> grants, string itemPath, int amount, float chance)
        {
            var item = NpcStartupDiagnostics.LoadResource<ItemData>("SkeletonCorpse", itemPath);
            if (item == null)
            {
                Debug.LogWarning($"[Skeleton] Corpse item '{itemPath}' was not found.");
                return;
            }

            grants.Add(new CorpseItemGrant
            {
                Item = item,
                Amount = amount,
                Chance = chance
            });
        }

        private struct SkeletonAnimationSet
        {
            public AnimationClip IdleClip;
            public AnimationClip WalkClip;
            public AnimationClip[] AttackClips;
            public AnimationClip[] DamageClips;
            public AnimationClip[] DeathClips;
        }

        private readonly struct SkeletonVariant
        {
            public SkeletonVariant(
                string objectName,
                string displayName,
                string healthBarName,
                string visualPrefabName,
                string animationFolder,
                string clipPrefix,
                Vector3 spawnOffset,
                float maxHealth,
                int soulAshReward,
                float moveSpeed,
                float aggroRange,
                float loseTargetRange,
                float attackRange,
                float attackCooldown,
                float attackWindup,
                float healthDamage,
                float poiseDamage,
                DamageType damageType,
                bool isRanged)
            {
                ObjectName = objectName;
                DisplayName = displayName;
                HealthBarName = healthBarName;
                VisualPrefabName = visualPrefabName;
                AnimationFolder = animationFolder;
                ClipPrefix = clipPrefix;
                SpawnOffset = spawnOffset;
                MaxHealth = maxHealth;
                SoulAshReward = soulAshReward;
                MoveSpeed = moveSpeed;
                AggroRange = aggroRange;
                LoseTargetRange = loseTargetRange;
                AttackRange = attackRange;
                AttackCooldown = attackCooldown;
                AttackWindup = attackWindup;
                HealthDamage = healthDamage;
                PoiseDamage = poiseDamage;
                DamageType = damageType;
                IsRanged = isRanged;
                CorpseHealth = Mathf.Max(80f, maxHealth * 1.6f);
            }

            public string ObjectName { get; }
            public string DisplayName { get; }
            public string HealthBarName { get; }
            public string VisualPrefabName { get; }
            public string AnimationFolder { get; }
            public string ClipPrefix { get; }
            public Vector3 SpawnOffset { get; }
            public float MaxHealth { get; }
            public int SoulAshReward { get; }
            public float MoveSpeed { get; }
            public float AggroRange { get; }
            public float LoseTargetRange { get; }
            public float AttackRange { get; }
            public float AttackCooldown { get; }
            public float AttackWindup { get; }
            public float HealthDamage { get; }
            public float PoiseDamage { get; }
            public DamageType DamageType { get; }
            public bool IsRanged { get; }
            public float CorpseHealth { get; }
        }

        private sealed class SkeletonBootstrapRunner : MonoBehaviour
        {
            private Coroutine _spawnRoutine;

            private void OnEnable()
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            private void Start()
            {
                StartSpawnRoutine();
            }

            private void OnDisable()
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                StartSpawnRoutine();
            }

            private void StartSpawnRoutine()
            {
                if (_spawnRoutine != null)
                    StopCoroutine(_spawnRoutine);

                _spawnRoutine = StartCoroutine(SpawnWhenPlayerIsReady());
            }

            private IEnumerator SpawnWhenPlayerIsReady()
            {
                const int maxAttempts = 120;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    if (TrySpawn())
                    {
                        _spawnRoutine = null;
                        yield break;
                    }

                    yield return null;
                }

                Debug.LogWarning("[Skeleton] Player was not found, dungeon skeleton spawn skipped.");
                _spawnRoutine = null;
            }
        }
    }
}
