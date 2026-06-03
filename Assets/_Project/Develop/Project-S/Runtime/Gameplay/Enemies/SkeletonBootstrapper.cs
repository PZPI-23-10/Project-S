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

namespace Project_S.Runtime.Gameplay.Enemies
{
    public static class SkeletonBootstrapper
    {
        private const string RunnerName = "[MVP] Skeleton Bootstrapper";
        private const string RootName = "[MVP] Enemies";
        private const string SkeletonName = "[MVP] Skeleton";
        private const string SkeletonVisualPath = "Enemies/Skeleton/KBH_Skel";
        private const string SkeletonAnimatorPath = "Enemies/Skeleton/SkeletonAnimator";
        private const string SkeletonDeathAnimationPath = "Enemies/Skeleton/Skeleton_Death";
        private const string SkeletonIdleAnimationPath = "Enemies/Skeleton/Zombie Idle";
        private const string SkeletonWalkAnimationPath = "Enemies/Skeleton/Zombie Walk";
        private const string SkeletonHitReactionAnimationPath = "Enemies/Skeleton/Zombie Reaction Hit";
        private const string SkeletonAttackAnimationPath = "Enemies/Skeleton/Zombie Punching";
        private const float SkeletonAgentRadius = 0.65f;
        private const float SkeletonAgentHeight = 2.35f;
        private const float SkeletonAgentBaseOffset = 0f;
        private const float SkeletonStoppingDistancePadding = 0.05f;
        private const float SkeletonRepathInterval = 0.2f;
        private const float SkeletonVisualScale = 9.2f;
        private const float SkeletonDestroyDelayAfterDeath = 60f;
        private const float SkeletonCorpseHealth = 100f;
        private const float CorpseHealthPerBaseYield = 20f;
        private const float CorpseLifetimeSeconds = 300f;
        private const string BonePath = "Crafting/Items/Resources/Bone";
        private const string LeatherPath = "Crafting/Items/Resources/Leather";
        private static readonly Vector3 SkeletonHealthBarOffset = new Vector3(0f, 2.6f, 0f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (GameObject.Find(RunnerName) != null)
                return;

            var runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<SkeletonBootstrapRunner>();
        }

        private static bool TrySpawn()
        {
            if (GameObject.Find(SkeletonName) != null)
                return true;

            var player = Object.FindFirstObjectByType<PlayerFacade>();
            if (player == null)
                return false;

            return NpcStartupDiagnostics.Time("Skeleton bootstrap spawn", () => TrySpawnWithPlayer(player));
        }

        private static bool TrySpawnWithPlayer(PlayerFacade player)
        {
            var root = GameObject.Find(RootName);
            if (root == null)
                root = new GameObject(RootName);

            var config = CreateSkeletonConfig();

            Vector3 spawnPosition = player.transform.position
                + player.transform.forward * 6f
                + player.transform.right * 1.25f
                + Vector3.up * 0.85f;

            SpawnSkeleton(root.transform, spawnPosition, player.transform, config);
            return true;
        }

        private static EnemyConfig CreateSkeletonConfig()
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.DisplayName = "Skeleton";
            config.MaxHealth = 45f;
            config.SoulAshReward = 12;
            config.MoveSpeed = 2.2f;
            config.AggroRange = 9f;
            config.LoseTargetRange = 13f;
            config.AttackRange = 1.7f;
            config.RotationSpeed = 540f;
            config.AttackCooldown = 1.8f;
            config.AttackWindup = 0.45f;
            config.UseAttackClipDamageMoment = true;
            config.AttackDamageMomentNormalized = 0.38f;
            config.AttackRadius = 0.65f;
            config.HealthDamage = 12f;
            config.PoiseDamage = 8f;
            config.DamageType = DamageType.Blunt;
            config.DestroyDelayAfterDeath = SkeletonDestroyDelayAfterDeath;
            return config;
        }

        private static void SpawnSkeleton(Transform parent, Vector3 position, Transform target, EnemyConfig config)
        {
            var skeleton = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skeleton.name = SkeletonName;
            skeleton.transform.SetParent(parent);
            skeleton.transform.position = position;
            skeleton.transform.localScale = Vector3.one * 1.1f;

            skeleton.transform.position = GroundPositionSampler.SampleNavMeshNearGround(position, 5f);

            if (!NavMesh.SamplePosition(skeleton.transform.position, out NavMeshHit navMeshCheck, 0.25f, NavMesh.AllAreas))
                Debug.LogWarning("[Skeleton] Spawn position is not on the runtime navmesh. Movement will stay disabled until a navmesh point is available.");
            else
                skeleton.transform.position = navMeshCheck.position;

            var renderer = skeleton.GetComponent<Renderer>();
            ConfigureHitbox(skeleton);

            var visual = TryAttachVisual(skeleton.transform);
            if (visual == null && renderer != null)
            {
                renderer.material.color = Color.white;
            }
            else if (renderer != null)
            {
                renderer.enabled = false;
            }

            var rigidbody = skeleton.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = skeleton.AddComponent<EnemyHealth>();
            var attack = skeleton.AddComponent<EnemyMeleeAttack>();
            var mover = skeleton.AddComponent<GroundNavMeshMover>();
            var controller = skeleton.AddComponent<EnemyController>();
            var worldHealthBar = skeleton.AddComponent<EnemyWorldHealthBar>();
            var lootDropper = skeleton.AddComponent<LootDropper>();
            var animationController = skeleton.AddComponent<EnemyAnimationController>();

            var lootTable = NpcStartupDiagnostics.LoadResource<LootTableData>("Skeleton", "Loot/BasicEnemyLoot");
            lootDropper.Configure(lootTable);

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(config);
            mover.Configure(
                config.MoveSpeed,
                config.AttackRange - SkeletonStoppingDistancePadding,
                SkeletonAgentRadius,
                SkeletonAgentHeight,
                SkeletonAgentBaseOffset,
                12f,
                config.RotationSpeed,
                SkeletonRepathInterval,
                50);
            mover.TryWarpToNearestNavMesh(5f);
            controller.Configure(config, target);
            animationController.Configure(
                controller,
                attack,
                health,
                visual != null ? visual.transform : null,
                visual != null ? visual.GetComponentInChildren<Animator>() : null,
                LoadSkeletonClip(SkeletonDeathAnimationPath, "Skeleton_Death", "Death"),
                LoadSkeletonClip(SkeletonIdleAnimationPath, "Zombie Idle", "Idle"),
                LoadSkeletonClip(SkeletonWalkAnimationPath, "Zombie Walk", "Walk"),
                LoadSkeletonClip(SkeletonHitReactionAnimationPath, "Zombie Reaction Hit", "Hit reaction"),
                LoadSkeletonClip(SkeletonAttackAnimationPath, "Zombie Punching1", "Attack"));
            CreateSkeletonCorpseHarvest(skeleton, health, visual != null ? visual.transform : skeleton.transform);
            worldHealthBar.Configure("Скелет", SkeletonHealthBarOffset);
        }

        private static void CreateSkeletonCorpseHarvest(GameObject skeleton, EnemyHealth health, Transform poseRoot)
        {
            var baseYields = new List<CorpseItemGrant>();
            var completionDrops = new List<CorpseItemGrant>();

            var bone = NpcStartupDiagnostics.LoadResource<ItemData>("SkeletonCorpse", BonePath);
            if (bone != null)
            {
                baseYields.Add(new CorpseItemGrant
                {
                    Item = bone,
                    Amount = 1
                });
            }

            var leather = NpcStartupDiagnostics.LoadResource<ItemData>("SkeletonCorpse", LeatherPath);
            if (leather != null)
            {
                completionDrops.Add(new CorpseItemGrant
                {
                    Item = leather,
                    Amount = 1
                });
            }

            var corpse = skeleton.AddComponent<AnimalCorpseHarvest>();
            corpse.Configure(
                health,
                skeleton,
                poseRoot,
                SkeletonCorpseHealth,
                CorpseHealthPerBaseYield,
                baseYields,
                completionDrops,
                0,
                CorpseLifetimeSeconds,
                scriptedDeathPose: false);
        }

        private static void ConfigureHitbox(GameObject skeleton)
        {
            foreach (var collider in skeleton.GetComponents<Collider>())
                Object.Destroy(collider);

            var hitbox = skeleton.AddComponent<CapsuleCollider>();
            hitbox.radius = SkeletonAgentRadius;
            hitbox.height = SkeletonAgentHeight;
            hitbox.center = new Vector3(0f, SkeletonAgentHeight * 0.5f, 0f);
            hitbox.direction = 1;
        }

        private static GameObject TryAttachVisual(Transform parent)
        {
            var visualPrefab = NpcStartupDiagnostics.LoadResource<GameObject>("Skeleton", SkeletonVisualPath);
            if (visualPrefab == null)
                return null;

            var visual = Object.Instantiate(visualPrefab, parent);
            visual.name = "VisualRoot";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * SkeletonVisualScale;

            ConfigureAnimator(visual);

            foreach (var collider in visual.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            return visual;
        }

        private static void ConfigureAnimator(GameObject visual)
        {
            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            var controller = NpcStartupDiagnostics.LoadResource<RuntimeAnimatorController>("Skeleton", SkeletonAnimatorPath);
            if (controller != null)
                animator.runtimeAnimatorController = controller;

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static AnimationClip LoadSkeletonClip(string path, string preferredClipName, string label)
        {
            var clips = NpcStartupDiagnostics.LoadAllResources<AnimationClip>("Skeleton", path);
            foreach (var clip in clips)
            {
                if (clip == null || clip.name.StartsWith("__preview__"))
                    continue;

                if (clip.name == preferredClipName)
                    return clip;
            }

            foreach (var clip in clips)
            {
                if (clip == null || clip.name.StartsWith("__preview__"))
                    continue;

                return clip;
            }

            Debug.LogWarning($"[Skeleton] {label} animation '{path}' was not found.");
            return null;
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

                Debug.LogWarning("[Skeleton] Player was not found, skeleton spawn skipped.");
                _spawnRoutine = null;
            }
        }
    }
}
