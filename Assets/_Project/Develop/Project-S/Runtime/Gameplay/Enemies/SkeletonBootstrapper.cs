using System.Collections;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Player;
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
        private const string SkeletonVisualPath = "Enemies/Skeleton/SkeletonVisual";
        private const string SkeletonAnimatorPath = "Enemies/Skeleton/SkeletonAnimator";

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

            var root = GameObject.Find(RootName);
            if (root == null)
                root = new GameObject(RootName);

            var config = CreateSkeletonConfig();

            Vector3 spawnPosition = player.transform.position
                + player.transform.forward * 6f
                + player.transform.right * 1.25f
                + Vector3.up * 0.55f;

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
            config.AgentRadius = 0.5f;
            config.AgentHeight = 2f;
            config.AgentBaseOffset = 0f;
            config.MaxStepHeight = 0.4f;
            config.MaxSlope = 45f;
            config.StoppingDistancePadding = 0.05f;
            config.RepathInterval = 0.2f;
            config.AttackCooldown = 1.8f;
            config.AttackWindup = 0.45f;
            config.AttackRadius = 0.65f;
            config.HealthDamage = 12f;
            config.PoiseDamage = 8f;
            config.DamageType = DamageType.Blunt;
            return config;
        }

        private static void SpawnSkeleton(Transform parent, Vector3 position, Transform target, EnemyConfig config)
        {
            var skeleton = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skeleton.name = SkeletonName;
            skeleton.transform.SetParent(parent);
            skeleton.transform.position = position;
            skeleton.transform.localScale = Vector3.one * 1.1f;

            if (NavMesh.SamplePosition(position, out NavMeshHit navMeshHit, 5f, NavMesh.AllAreas))
                skeleton.transform.position = navMeshHit.position;
            else
                Debug.LogWarning("[Skeleton] Spawn position is not on the runtime navmesh. Movement will stay disabled until a navmesh point is available.");

            var renderer = skeleton.GetComponent<Renderer>();
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

            var lootTable = Resources.Load<LootTableData>("Loot/BasicEnemyLoot");
            lootDropper.Configure(lootTable);

            health.Configure(config);
            attack.Configure(config);
            mover.Configure(config.MoveSpeed, config.AttackRange - config.StoppingDistancePadding, config.AgentRadius, config.AgentHeight, config.AgentBaseOffset, 12f, config.RotationSpeed, config.RepathInterval, 50);
            mover.TryWarpToNearestNavMesh(5f);
            controller.Configure(config, target);
            animationController.Configure(controller, attack, health, visual != null ? visual.transform : null, visual != null ? visual.GetComponentInChildren<Animator>() : null);
            worldHealthBar.Configure("Скелет", new Vector3(0f, 1.45f, 0f));
        }

        private static GameObject TryAttachVisual(Transform parent)
        {
            var visualPrefab = Resources.Load<GameObject>(SkeletonVisualPath);
            if (visualPrefab == null)
                return null;

            var visual = Object.Instantiate(visualPrefab, parent);
            visual.name = "VisualRoot";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * 8f;

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

            var controller = Resources.Load<RuntimeAnimatorController>(SkeletonAnimatorPath);
            if (controller != null)
                animator.runtimeAnimatorController = controller;

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
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
