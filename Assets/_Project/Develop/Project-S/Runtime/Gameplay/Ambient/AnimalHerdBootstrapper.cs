using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Diagnostics;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Ambient
{
    public static class AnimalHerdBootstrapper
    {
        private const string RunnerName = "[MVP] Animal Herd Bootstrapper";
        private const string AmbientRootName = "[MVP] Ambient";
        private const string HerdRootName = "[MVP] Animal Herds";
        private static readonly bool AutoSpawnEnabled = false;

        private const float MinSpawnDistance = 20f;
        private const float MaxSpawnDistance = 40f;
        private const float DeathDestroyDelay = 8f;
        private const float CorpseLifetimeSeconds = 300f;
        private const float CorpseHealthPerBaseYield = 20f;

        private const string GreyMeatPath = "Crafting/Items/Consumables/GreyMeat";
        private const string BonePath = "Crafting/Items/Resources/Bone";
        private const string LeatherPath = "Crafting/Items/Resources/Leather";
        private const string PetrifiedBloodPath = "Crafting/Items/Resources/PetrifiedBlood";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (!AutoSpawnEnabled)
                return;

            if (GameObject.Find(RunnerName) != null)
                return;

            var runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<AnimalHerdBootstrapRunner>();
        }

        private static bool TrySpawn()
        {
            if (GameObject.Find(HerdRootName) != null)
                return true;

            var player = Object.FindFirstObjectByType<PlayerFacade>();
            if (player == null)
                return false;

            return NpcStartupDiagnostics.Time("Animal herd bootstrap spawn", () => TrySpawnWithPlayer(player));
        }

        private static bool TrySpawnWithPlayer(PlayerFacade player)
        {
            var ambientRoot = GameObject.Find(AmbientRootName);
            if (ambientRoot == null)
                ambientRoot = new GameObject(AmbientRootName);

            var herdRoot = new GameObject(HerdRootName);
            herdRoot.transform.SetParent(ambientRoot.transform);

            SpawnGroup(herdRoot.transform, player.transform, AnimalDefaults.Deer(), 2, -30f);
            SpawnGroup(herdRoot.transform, player.transform, AnimalDefaults.Horse(), 1, 35f);
            SpawnGroup(herdRoot.transform, player.transform, AnimalDefaults.Chicken(), 4, 95f);
            SpawnBoar(herdRoot.transform, player.transform, 150f);

            return true;
        }

        private static void SpawnGroup(
            Transform parent,
            Transform player,
            AnimalSpawnDefinition definition,
            int count,
            float angleOffset)
        {
            var prefab = NpcStartupDiagnostics.LoadResource<GameObject>("AnimalHerd", definition.PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Animals] Prefab '{definition.PrefabPath}' was not found.");
                return;
            }

            Vector3 herdCenter = FindHerdCenter(player, angleOffset);
            var herd = new GameObject(definition.HerdName);
            herd.transform.SetParent(parent);
            herd.transform.position = herdCenter;

            for (int index = 0; index < count; index++)
                SpawnAnimal(prefab, herd.transform, player, herdCenter, definition, index);
        }

        private static Vector3 FindHerdCenter(Transform player, float angleOffset)
        {
            float angle = angleOffset + Random.Range(-18f, 18f);
            float distance = Random.Range(MinSpawnDistance, MaxSpawnDistance);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * player.forward;
            return NeutralAnimalController.SampleGround(player.position + direction.normalized * distance);
        }

        private static void SpawnAnimal(
            GameObject prefab,
            Transform parent,
            Transform player,
            Vector3 herdCenter,
            AnimalSpawnDefinition definition,
            int index)
        {
            Vector2 offset = Random.insideUnitCircle * definition.HerdRadius * 0.7f;
            Vector3 spawnPosition = SampleNavMeshPosition(
                NeutralAnimalController.SampleGround(herdCenter + new Vector3(offset.x, 0f, offset.y)),
                definition.HerdRadius);
            var animal = Object.Instantiate(prefab, spawnPosition, RandomYaw(), parent);
            animal.name = $"{definition.DisplayName} {index + 1}";

            DisableImportedDemoComponents(animal);
            ConfigurePhysics(animal, definition);
            ConfigureAnimator(animal);

            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.DisplayName = definition.DisplayName;
            config.MaxHealth = definition.MaxHealth;
            config.SoulAshReward = 0;
            config.DestroyDelayAfterDeath = DeathDestroyDelay;

            var health = animal.AddComponent<EnemyHealth>();
            var mover = animal.AddComponent<GroundNavMeshMover>();
            var controller = animal.AddComponent<NeutralAnimalController>();
            var worldHealthBar = animal.AddComponent<EnemyWorldHealthBar>();

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            mover.Configure(definition.WalkSpeed, 0.15f, definition.ColliderRadius, definition.ColliderHeight, 0f, 8f, 540f, 0.25f, 60);
            mover.TryWarpToNearestNavMesh(definition.HerdRadius);
            controller.Configure(
                player,
                health,
                herdCenter,
                definition.HerdRadius,
                definition.WalkSpeed,
                definition.RunSpeed,
                definition.ScareRadius);
            worldHealthBar.Configure(definition.UiName, definition.HealthBarOffset);
            CreateCorpseHarvest(animal, health, definition, ResolveAnimalPoseRoot(animal), scriptedDeathPose: true);
        }

        private static void DisableImportedDemoComponents(GameObject animal)
        {
            foreach (var behaviour in animal.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;
        }

        private static void ConfigurePhysics(GameObject animal, AnimalSpawnDefinition definition)
        {
            foreach (var collider in animal.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            foreach (var existingRigidbody in animal.GetComponentsInChildren<Rigidbody>())
            {
                existingRigidbody.isKinematic = true;
                existingRigidbody.useGravity = false;
                existingRigidbody.velocity = Vector3.zero;
                existingRigidbody.angularVelocity = Vector3.zero;
            }

            var hitCollider = animal.AddComponent<CapsuleCollider>();
            hitCollider.isTrigger = true;
            hitCollider.radius = definition.ColliderRadius;
            hitCollider.height = definition.ColliderHeight;
            hitCollider.center = new Vector3(0f, definition.ColliderHeight * 0.5f, 0f);

            var rigidbody = animal.GetComponent<Rigidbody>();
            if (rigidbody == null)
                rigidbody = animal.AddComponent<Rigidbody>();

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        private static void ConfigureAnimator(GameObject animal)
        {
            var animator = animal.GetComponentInChildren<Animator>();
            if (animator == null)
                return;

            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private static Transform ResolveAnimalPoseRoot(GameObject animal)
        {
            var animator = animal.GetComponentInChildren<Animator>();
            return animator != null ? animator.transform : animal.transform;
        }

        private static Quaternion RandomYaw()
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        private static Vector3 SampleNavMeshPosition(Vector3 position, float searchRadius)
        {
            return GroundPositionSampler.SampleNavMeshNearGround(position, searchRadius);
        }

        private readonly struct AnimalSpawnDefinition
        {
            public AnimalSpawnDefinition(
                string displayName,
                string herdName,
                string prefabPath,
                float maxHealth,
                float walkSpeed,
                float runSpeed,
                float scareRadius,
                float herdRadius,
                float colliderRadius,
                float colliderHeight,
                string uiName,
                Vector3 healthBarOffset,
                AnimalCorpseDefinition corpse)
            {
                DisplayName = displayName;
                HerdName = herdName;
                PrefabPath = prefabPath;
                MaxHealth = maxHealth;
                WalkSpeed = walkSpeed;
                RunSpeed = runSpeed;
                ScareRadius = scareRadius;
                HerdRadius = herdRadius;
                ColliderRadius = colliderRadius;
                ColliderHeight = colliderHeight;
                UiName = uiName;
                HealthBarOffset = healthBarOffset;
                Corpse = corpse;
            }

            public string DisplayName { get; }
            public string HerdName { get; }
            public string PrefabPath { get; }
            public float MaxHealth { get; }
            public float WalkSpeed { get; }
            public float RunSpeed { get; }
            public float ScareRadius { get; }
            public float HerdRadius { get; }
            public float ColliderRadius { get; }
            public float ColliderHeight { get; }
            public string UiName { get; }
            public Vector3 HealthBarOffset { get; }
            public AnimalCorpseDefinition Corpse { get; }
        }

        private static class AnimalDefaults
        {
            public static AnimalSpawnDefinition Horse()
            {
                return new AnimalSpawnDefinition(
                    "Horse",
                    "Horse Herd",
                    "Ambient/Animals/Horse_001",
                    60f,
                    1.4f,
                    5f,
                    7f,
                    10f,
                    0.8f,
                    2.2f,
                    "Horse",
                    new Vector3(0f, 2.45f, 0f),
                    AnimalCorpseDefinition.Horse());
            }

            public static AnimalSpawnDefinition Deer()
            {
                return new AnimalSpawnDefinition(
                    "Deer",
                    "Deer Herd",
                    "Ambient/Animals/Deer_001",
                    35f,
                    1.2f,
                    6f,
                    9f,
                    12f,
                    0.55f,
                    1.7f,
                    "Deer",
                    new Vector3(0f, 1.9f, 0f),
                    AnimalCorpseDefinition.Deer());
            }

            public static AnimalSpawnDefinition Chicken()
            {
                return new AnimalSpawnDefinition(
                    "Chicken",
                    "Chicken Flock",
                    "Ambient/Animals/Chicken_001",
                    10f,
                    0.8f,
                    3f,
                    4f,
                    5f,
                    0.22f,
                    0.55f,
                    "Chicken",
                    new Vector3(0f, 0.8f, 0f),
                    AnimalCorpseDefinition.Chicken());
            }
        }

        private static void SpawnBoar(Transform parent, Transform player, float angleOffset)
        {
            var prefab = NpcStartupDiagnostics.LoadResource<GameObject>("AnimalHerd", "Ambient/Animals/BoarPrefab");
            if (prefab == null)
            {
                Debug.LogWarning("[Animals] Prefab 'Ambient/Animals/BoarPrefab' was not found.");
                return;
            }

            Vector3 homeCenter = FindHerdCenter(player, angleOffset);
            var boarGroup = new GameObject("Boar Range");
            boarGroup.transform.SetParent(parent);
            boarGroup.transform.position = homeCenter;

            var boar = new GameObject("Wild Boar 1");
            boar.transform.SetParent(boarGroup.transform);
            boar.transform.SetPositionAndRotation(SampleNavMeshPosition(homeCenter, 9f), RandomYaw());

            var visual = Object.Instantiate(prefab, boar.transform);
            visual.name = "VisualRoot";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            DisableImportedDemoComponents(visual);
            AdjustBoarVisualHeight(visual);
            ConfigureAnimator(visual);
            ConfigurePhysics(boar, new AnimalSpawnDefinition(
                "WildBoar",
                "Boar Range",
                "Ambient/Animals/BoarPrefab",
                55f,
                1.1f,
                4.8f,
                0f,
                9f,
                0.55f,
                1.15f,
                "Boar",
                new Vector3(0f, 1.35f, 0f),
                AnimalCorpseDefinition.Boar()));
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.DisplayName = "WildBoar";
            config.MaxHealth = 55f;
            config.SoulAshReward = 0;
            config.MoveSpeed = 4.8f;
            config.AttackRange = 1.55f;
            config.RotationSpeed = 620f;
            config.AttackCooldown = 1.8f;
            config.AttackWindup = 0.42f;
            config.AttackRadius = 0.75f;
            config.HealthDamage = 13f;
            config.PoiseDamage = 7f;
            config.DamageType = DamageType.Blunt;
            config.DestroyDelayAfterDeath = DeathDestroyDelay;

            var health = boar.AddComponent<EnemyHealth>();
            var attack = boar.AddComponent<EnemyMeleeAttack>();
            var mover = boar.AddComponent<GroundNavMeshMover>();
            var controller = boar.AddComponent<RetaliatingBoarController>();
            var worldHealthBar = boar.AddComponent<EnemyWorldHealthBar>();

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(config);
            mover.Configure(1.1f, Mathf.Max(0.1f, config.AttackRange - 0.05f), 0.55f, 1.15f, 0f, 12f, config.RotationSpeed, 0.18f, 45);
            mover.TryWarpToNearestNavMesh(9f);
            controller.Configure(
                player,
                health,
                attack,
                homeCenter,
                9f,
                1.1f,
                4.8f,
                config.AttackRange);
            CreateCorpseHarvest(boar, health, AnimalCorpseDefinition.Boar(), visual.transform, scriptedDeathPose: false, 0.55f, 1.15f);
            worldHealthBar.Configure("Boar", new Vector3(0f, 1.35f, 0f));
        }

        private static void CreateCorpseHarvest(
            GameObject owner,
            EnemyHealth health,
            AnimalSpawnDefinition definition,
            Transform poseRoot,
            bool scriptedDeathPose)
        {
            CreateCorpseHarvest(
                owner,
                health,
                definition.Corpse,
                poseRoot,
                scriptedDeathPose,
                definition.ColliderRadius,
                definition.ColliderHeight);
        }

        private static void CreateCorpseHarvest(
            GameObject owner,
            EnemyHealth health,
            AnimalCorpseDefinition definition,
            Transform poseRoot,
            bool scriptedDeathPose,
            float colliderRadius,
            float colliderHeight)
        {
            if (!definition.IsValid)
                return;

            var corpse = owner.AddComponent<AnimalCorpseHarvest>();
            corpse.Configure(
                health,
                owner,
                poseRoot,
                definition.MaxHealth,
                CorpseHealthPerBaseYield,
                CreateGrants(definition.BaseYields),
                CreateGrants(definition.CompletionDrops),
                definition.SoulAshReward,
                CorpseLifetimeSeconds,
                scriptedDeathPose);
        }

        private static IEnumerable<CorpseItemGrant> CreateGrants(IEnumerable<CorpseDropDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                var item = NpcStartupDiagnostics.LoadResource<ItemData>("AnimalCorpse", definition.ItemPath);
                if (item == null)
                {
                    Debug.LogWarning($"[Animals] Corpse item '{definition.ItemPath}' was not found.");
                    continue;
                }

                yield return new CorpseItemGrant
                {
                    Item = item,
                    Amount = definition.Amount,
                    Chance = definition.Chance
                };
            }
        }

        private readonly struct AnimalCorpseDefinition
        {
            public AnimalCorpseDefinition(
                float maxHealth,
                IReadOnlyList<CorpseDropDefinition> baseYields,
                IReadOnlyList<CorpseDropDefinition> completionDrops,
                int soulAshReward)
            {
                MaxHealth = maxHealth;
                BaseYields = baseYields;
                CompletionDrops = completionDrops;
                SoulAshReward = soulAshReward;
            }

            public float MaxHealth { get; }
            public IReadOnlyList<CorpseDropDefinition> BaseYields { get; }
            public IReadOnlyList<CorpseDropDefinition> CompletionDrops { get; }
            public int SoulAshReward { get; }
            public bool IsValid => MaxHealth > 0f;

            public static AnimalCorpseDefinition Boar()
            {
                return new AnimalCorpseDefinition(
                    120f,
                    new[]
                    {
                        new CorpseDropDefinition(GreyMeatPath, 1),
                        new CorpseDropDefinition(BonePath, 1)
                    },
                    new[]
                    {
                        new CorpseDropDefinition(BonePath, 6),
                        new CorpseDropDefinition(LeatherPath, 2),
                        new CorpseDropDefinition(PetrifiedBloodPath, 1, 0.05f)
                    },
                    15);
            }

            public static AnimalCorpseDefinition Chicken()
            {
                return new AnimalCorpseDefinition(
                    40f,
                    new[]
                    {
                        new CorpseDropDefinition(GreyMeatPath, 1),
                        new CorpseDropDefinition(BonePath, 1)
                    },
                    new[]
                    {
                        new CorpseDropDefinition(GreyMeatPath, 1),
                        new CorpseDropDefinition(BonePath, 1)
                    },
                    1);
            }

            public static AnimalCorpseDefinition Deer()
            {
                return new AnimalCorpseDefinition(
                    120f,
                    new[]
                    {
                        new CorpseDropDefinition(GreyMeatPath, 1),
                        new CorpseDropDefinition(BonePath, 1)
                    },
                    new[]
                    {
                        new CorpseDropDefinition(GreyMeatPath, 6),
                        new CorpseDropDefinition(LeatherPath, 4)
                    },
                    5);
            }

            public static AnimalCorpseDefinition Horse()
            {
                return new AnimalCorpseDefinition(
                    200f,
                    new[]
                    {
                        new CorpseDropDefinition(BonePath, 1)
                    },
                    new[]
                    {
                        new CorpseDropDefinition(BonePath, 2),
                        new CorpseDropDefinition(LeatherPath, 1)
                    },
                    10);
            }
        }

        private readonly struct CorpseDropDefinition
        {
            public CorpseDropDefinition(string itemPath, int amount, float chance = 1f)
            {
                ItemPath = itemPath;
                Amount = amount;
                Chance = chance;
            }

            public string ItemPath { get; }
            public int Amount { get; }
            public float Chance { get; }
        }

        private static void AdjustBoarVisualHeight(GameObject boar)
        {
            var visualRoot = boar.transform.Find("Boar");
            if (visualRoot == null)
                return;

            Vector3 localPosition = visualRoot.localPosition;
            localPosition.y = 0.05f;
            visualRoot.localPosition = localPosition;
        }

        private sealed class AnimalHerdBootstrapRunner : MonoBehaviour
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

                Debug.LogWarning("[Animals] Player was not found, animal herd spawn skipped.");
                _spawnRoutine = null;
            }
        }
    }
}
