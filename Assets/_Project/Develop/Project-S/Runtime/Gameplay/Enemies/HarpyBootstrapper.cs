using System.Collections;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Loot;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Enemies
{
    public static class HarpyBootstrapper
    {
        private const string RunnerName = "[MVP] Harpy Bootstrapper";
        private const string RootName = "[MVP] Enemies";
        private const string HarpyName = "[MVP] Harpy";
        private const string HarpyVisualPath = "Enemies/Harpy/HarpyVisual";
        private const string HarpyAnimatorPath = "Enemies/Harpy/HarpyAnimator";

        private const float HoverHeight = 12f;
        private const float HoverRadius = 24f;
        private const float DiveStopHeight = 1.2f;
        private const float RetreatDistanceThreshold = 0.35f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (GameObject.Find(RunnerName) != null)
                return;

            var runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<HarpyBootstrapRunner>();
        }

        private static bool TrySpawn()
        {
            if (GameObject.Find(HarpyName) != null)
                return true;

            var player = Object.FindFirstObjectByType<PlayerFacade>();
            if (player == null)
                return false;

            var root = GameObject.Find(RootName);
            if (root == null)
                root = new GameObject(RootName);

            var config = CreateHarpyConfig();

            Vector3 spawnPosition = player.transform.position
                + player.transform.forward * 22f
                - player.transform.right * 10f
                + Vector3.up * HoverHeight;

            SpawnHarpy(root.transform, spawnPosition, player.transform, config);
            return true;
        }

        private static EnemyConfig CreateHarpyConfig()
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.DisplayName = "Harpy";
            config.MaxHealth = 35f;
            config.SoulAshReward = 16;
            config.MoveSpeed = 7f;
            config.AggroRange = 35f;
            config.LoseTargetRange = 48f;
            config.AttackRange = 1.8f;
            config.RotationSpeed = 720f;
            config.AttackCooldown = 5.5f;
            config.AttackWindup = 0.35f;
            config.AttackRadius = 0.75f;
            config.HealthDamage = 10f;
            config.PoiseDamage = 6f;
            config.DamageType = DamageType.Blunt;
            config.DestroyDelayAfterDeath = 30f;
            return config;
        }

        private static void SpawnHarpy(Transform parent, Vector3 position, Transform target, EnemyConfig config)
        {
            var harpy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            harpy.name = HarpyName;
            harpy.transform.SetParent(parent);
            harpy.transform.position = position;
            harpy.transform.localScale = Vector3.one * 0.95f;

            var hitCollider = harpy.GetComponent<SphereCollider>();
            if (hitCollider != null)
            {
                hitCollider.radius = 2.6f;
                hitCollider.center = new Vector3(0f, 1f, 0f);
            }

            var renderer = harpy.GetComponent<Renderer>();
            var visual = TryAttachVisual(harpy.transform);
            if (visual == null && renderer != null)
                renderer.material.color = new Color(0.55f, 0.95f, 1f, 1f);
            else if (renderer != null)
                renderer.enabled = false;

            var rigidbody = harpy.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var health = harpy.AddComponent<EnemyHealth>();
            var attack = harpy.AddComponent<EnemyMeleeAttack>();
            var controller = harpy.AddComponent<FlyingEnemyController>();
            var worldHealthBar = harpy.AddComponent<EnemyWorldHealthBar>();
            var lootDropper = harpy.AddComponent<LootDropper>();
            var animationController = harpy.AddComponent<HarpyAnimationController>();

            var lootTable = Resources.Load<LootTableData>("Loot/BasicEnemyLoot");
            lootDropper.Configure(lootTable);

            health.Configure(config);
            attack.Configure(config);
            controller.Configure(config, target, HoverHeight, HoverRadius, DiveStopHeight, RetreatDistanceThreshold);
            animationController.Configure(controller, attack, health, visual != null ? visual.GetComponentInChildren<Animator>() : null);
            worldHealthBar.Configure("Гарпия", new Vector3(0f, 2.2f, 0f));
        }

        private static GameObject TryAttachVisual(Transform parent)
        {
            var visualPrefab = Resources.Load<GameObject>(HarpyVisualPath);
            if (visualPrefab == null)
                return null;

            var visual = Object.Instantiate(visualPrefab, parent);
            visual.name = "VisualRoot";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            ConfigureVisual(visual);
            return visual;
        }

        private static void ConfigureVisual(GameObject visual)
        {
            foreach (var collider in visual.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            ConfigureAlphaCutoutMaterials(visual);

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            var animatorController = Resources.Load<RuntimeAnimatorController>(HarpyAnimatorPath);
            if (animatorController != null)
                animator.runtimeAnimatorController = animatorController;

            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
        }

        private static void ConfigureAlphaCutoutMaterials(GameObject visual)
        {
            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (material == null || !material.name.Contains("Feathers"))
                        continue;

                    material.EnableKeyword("_ALPHATEST_ON");
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

                    if (material.HasProperty("_AlphaClip"))
                        material.SetFloat("_AlphaClip", 1f);

                    if (material.HasProperty("_Cutoff"))
                        material.SetFloat("_Cutoff", 0.28f);

                    if (material.HasProperty("_Cull"))
                        material.SetFloat("_Cull", 0f);
                }
            }
        }

        private sealed class HarpyBootstrapRunner : MonoBehaviour
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

                Debug.LogWarning("[Harpy] Player was not found, harpy spawn skipped.");
                _spawnRoutine = null;
            }
        }
    }
}
