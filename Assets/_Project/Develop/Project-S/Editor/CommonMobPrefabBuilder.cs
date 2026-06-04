using System.Collections.Generic;
using System.IO;
using System.Linq;
using Project_S.Runtime.Gameplay.Ambient;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Loot;
using Project_S.Runtime.Gameplay.Navigation;
using Project_S.Runtime.Gameplay.Spawning;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Editor
{
    public static class CommonMobPrefabBuilder
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs/Mobs";
        private const string ConfigRoot = "Assets/_Project/Resources/Enemies/Common";
        private const string SpawnRoot = "Assets/_Project/Resources/Spawning";
        private const string SkeletonVisualRoot = "Assets/_Project/Resources/Enemies/DungeonSkeletons/Prefabs";
        private const string SkeletonAnimationRoot = "Assets/DungeonCharacters/Skeletons/Animation";
        private const string HarpyVisualPath = "Assets/_Project/Resources/Enemies/Harpy/HarpyVisual.prefab";
        private const string HarpyAnimatorPath = "Assets/_Project/Resources/Enemies/Harpy/HarpyAnimator.controller";
        private const string BoarMaterialPath = "Assets/_Project/ThirdParty/WildBoar/WildBoar/Materials/MaterialBoar.mat";
        private const string BasicLootPath = "Assets/_Project/Resources/Loot/BasicEnemyLoot.asset";
        private const string BonePath = "Assets/_Project/Resources/Crafting/Items/Resources/Bone.asset";
        private const string GreyMeatPath = "Assets/_Project/Resources/Crafting/Items/Consumables/GreyMeat.asset";
        private const string LeatherPath = "Assets/_Project/Resources/Crafting/Items/Resources/Leather.asset";
        private const string PetrifiedBloodPath = "Assets/_Project/Resources/Crafting/Items/Resources/PetrifiedBlood.asset";

        [MenuItem("Project-S/Enemies/Rebuild Common Mob Prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(ConfigRoot);
            Directory.CreateDirectory($"{SpawnRoot}/Definitions");
            Directory.CreateDirectory($"{SpawnRoot}/Tables");

            var definitions = new List<MobSpawnDefinition>();
            definitions.Add(BuildSkeleton(SkeletonVariant.Grunt()));
            definitions.Add(BuildSkeleton(SkeletonVariant.Guard()));
            definitions.Add(BuildSkeleton(SkeletonVariant.Brute()));
            definitions.Add(BuildSkeleton(SkeletonVariant.Archer()));
            definitions.Add(BuildHarpy());
            definitions.Add(BuildAnimal(AnimalVariant.Deer()));
            definitions.Add(BuildAnimal(AnimalVariant.Horse()));
            definitions.Add(BuildAnimal(AnimalVariant.Chicken()));
            definitions.Add(BuildBoar());
            definitions.Add(BuildSparrow());

            BuildTables(definitions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Mob Prefabs] Common mob prefabs and spawn assets rebuilt.");
        }

        private static MobSpawnDefinition BuildSkeleton(SkeletonVariant variant)
        {
            var config = LoadOrCreateConfig($"{ConfigRoot}/{variant.AssetName}Config.asset");
            config.DisplayName = variant.DisplayName;
            config.MaxHealth = variant.MaxHealth;
            config.SoulAshReward = variant.SoulAshReward;
            config.MoveSpeed = variant.MoveSpeed;
            config.AggroRange = variant.AggroRange;
            config.LoseTargetRange = variant.LoseTargetRange;
            config.AttackRange = variant.AttackRange;
            config.RotationSpeed = 540f;
            config.AgentRadius = 0.42f;
            config.AgentHeight = 1.75f;
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
            else
            {
                config.UseRangedAttack = false;
            }

            EditorUtility.SetDirty(config);

            var root = CreateRoot(variant.AssetName);
            ConfigureCapsule(root, config.AgentRadius, config.AgentHeight, new Vector3(0f, config.AgentHeight * 0.5f, 0f));
            ConfigureRigidbody(root);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = config.AgentRadius;
            agent.height = config.AgentHeight;

            var health = root.AddComponent<EnemyHealth>();
            var melee = root.AddComponent<EnemyMeleeAttack>();
            var mover = root.AddComponent<GroundNavMeshMover>();
            var controller = root.AddComponent<EnemyController>();
            var healthBar = root.AddComponent<EnemyWorldHealthBar>();
            var loot = root.AddComponent<LootDropper>();
            var animation = root.AddComponent<EnemyAnimationController>();
            EnemyRangedAttack ranged = null;

            var visual = AttachVisual(root.transform, $"{SkeletonVisualRoot}/{variant.VisualPrefabName}.prefab", Vector3.zero, Quaternion.identity, Vector3.one * 0.82f);
            var animator = visual != null ? visual.GetComponentInChildren<Animator>() : null;
            var idleClip = LoadSkeletonClip(variant.AnimationFolder, $"{variant.ClipPrefix}_idle_A");
            var walkClip = LoadSkeletonClip(variant.AnimationFolder, $"{variant.ClipPrefix}_walk");
            var attackClips = LoadSkeletonClips(variant.AnimationFolder, variant.ClipPrefix, "attack");
            var damageClips = LoadSkeletonClips(variant.AnimationFolder, variant.ClipPrefix, "damage");
            var deathClip = FirstClip(LoadSkeletonClips(variant.AnimationFolder, variant.ClipPrefix, "death"));
            var attackProfiles = CreateSkeletonMeleeProfiles(variant, attackClips);

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            melee.Configure(config);
            melee.ConfigureAttackProfiles(attackProfiles, AttackSelectionMode.Cycle);
            if (variant.IsRanged)
            {
                ranged = root.AddComponent<EnemyRangedAttack>();
                ranged.Configure(config, FirstClip(attackClips), visual != null ? visual.transform : root.transform);
            }

            mover.Configure(config.MoveSpeed, config.AttackRange - config.StoppingDistancePadding, config.AgentRadius, config.AgentHeight, config.AgentBaseOffset, Mathf.Max(8f, config.MoveSpeed * 4f), config.RotationSpeed, config.RepathInterval, variant.IsRanged ? 45 : 50);
            controller.Configure(config, null);
            controller.ConfigureHomeArea(Vector3.zero, 12f, true);
            healthBar.Configure(variant.DisplayName, new Vector3(0f, 1.95f, 0f));
            loot.Configure(AssetDatabase.LoadAssetAtPath<LootTableData>(BasicLootPath));
            animation.Configure(controller, melee, health, visual != null ? visual.transform : null, animator, deathClip, idleClip, walkClip, FirstClip(damageClips), FirstClip(attackClips));
            animation.ConfigureHitReactionClips(damageClips);
            animation.ConfigureRangedAttack(ranged, FirstClip(attackClips));
            AddCorpseHarvest(root, health, visual != null ? visual.transform : root.transform, 20f, 300f, Mathf.Max(80f, variant.MaxHealth * 1.6f), new[] { Grant(BonePath, 1) }, null, 0, false);

            return SaveMobPrefabAndDefinition(root, $"{PrefabRoot}/{variant.AssetName}.prefab", variant.DisplayName, config.AgentRadius, 3f);
        }

        private static MobSpawnDefinition BuildHarpy()
        {
            var config = LoadOrCreateConfig($"{ConfigRoot}/HarpyConfig.asset");
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
            EditorUtility.SetDirty(config);

            var root = CreateRoot("Harpy");
            var collider = root.AddComponent<SphereCollider>();
            collider.radius = 2.6f;
            collider.center = new Vector3(0f, 1f, 0f);
            ConfigureRigidbody(root);

            var health = root.AddComponent<EnemyHealth>();
            var attack = root.AddComponent<EnemyMeleeAttack>();
            var controller = root.AddComponent<FlyingEnemyController>();
            var healthBar = root.AddComponent<EnemyWorldHealthBar>();
            var loot = root.AddComponent<LootDropper>();
            var animation = root.AddComponent<HarpyAnimationController>();

            var visual = AttachVisual(root.transform, HarpyVisualPath, Vector3.zero, Quaternion.identity, Vector3.one);
            var animator = visual != null ? visual.GetComponentInChildren<Animator>() : null;
            var animatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HarpyAnimatorPath);
            if (animator != null && animatorController != null)
                animator.runtimeAnimatorController = animatorController;

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(config);
            controller.Configure(config, null, 12f, 24f, 1.2f, 0.35f);
            controller.ConfigureHomeArea(Vector3.zero, 24f);
            healthBar.Configure("Harpy", new Vector3(0f, 2.2f, 0f));
            loot.Configure(AssetDatabase.LoadAssetAtPath<LootTableData>(BasicLootPath));
            animation.Configure(controller, attack, health, animator);
            AddCorpseHarvest(root, health, root.transform, 20f, 300f, 80f, new[] { Grant(GreyMeatPath, 1), Grant(BonePath, 1) }, new[] { Grant(LeatherPath, 1), Grant(PetrifiedBloodPath, 1, 0.1f) }, 0, true);

            return SaveMobPrefabAndDefinition(root, $"{PrefabRoot}/Harpy.prefab", "Harpy", 2.6f, 3f);
        }

        private static MobSpawnDefinition BuildAnimal(AnimalVariant variant)
        {
            var config = LoadOrCreateConfig($"{ConfigRoot}/{variant.AssetName}Config.asset");
            config.DisplayName = variant.DisplayName;
            config.MaxHealth = variant.MaxHealth;
            config.SoulAshReward = 0;
            config.DestroyDelayAfterDeath = 8f;
            EditorUtility.SetDirty(config);

            var root = CreateRoot(variant.AssetName);
            var visual = AttachVisual(root.transform, variant.VisualPrefabPath, Vector3.zero, Quaternion.identity, Vector3.one);
            ConfigureCapsule(root, variant.ColliderRadius, variant.ColliderHeight, new Vector3(0f, variant.ColliderHeight * 0.5f, 0f), true);
            ConfigureRigidbody(root);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = variant.ColliderRadius;
            agent.height = variant.ColliderHeight;

            var health = root.AddComponent<EnemyHealth>();
            var mover = root.AddComponent<GroundNavMeshMover>();
            var controller = root.AddComponent<NeutralAnimalController>();
            var healthBar = root.AddComponent<EnemyWorldHealthBar>();

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            mover.Configure(variant.WalkSpeed, 0.15f, variant.ColliderRadius, variant.ColliderHeight, 0f, Mathf.Max(8f, variant.RunSpeed * 4f), 540f, 0.25f, 60);
            controller.Configure(null, health, Vector3.zero, variant.HomeRadius, variant.WalkSpeed, variant.RunSpeed, variant.ScareRadius);
            healthBar.Configure(variant.DisplayName, variant.HealthBarOffset);
            AddCorpseHarvest(root, health, visual != null ? visual.transform : root.transform, 20f, 300f, variant.CorpseHealth, variant.BaseYields, variant.CompletionDrops, variant.SoulAshReward, true);

            return SaveMobPrefabAndDefinition(root, $"{PrefabRoot}/{variant.AssetName}.prefab", variant.DisplayName, variant.ColliderRadius, 3f);
        }

        private static MobSpawnDefinition BuildBoar()
        {
            var config = LoadOrCreateConfig($"{ConfigRoot}/BoarConfig.asset");
            config.DisplayName = "Boar";
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
            config.DestroyDelayAfterDeath = 8f;
            config.AgentRadius = 0.55f;
            config.AgentHeight = 1.15f;
            config.RepathInterval = 0.18f;
            EditorUtility.SetDirty(config);

            var root = CreateRoot("Boar");
            var visual = AttachVisual(root.transform, "Assets/_Project/ThirdParty/WildBoar/WildBoar/Prefabs/BoarPrefab.prefab", Vector3.zero, Quaternion.identity, Vector3.one);
            AdjustBoarVisualHeight(visual);
            ForceMaterial(visual, BoarMaterialPath);
            ConfigureCapsule(root, 0.55f, 1.15f, new Vector3(0f, 0.575f, 0f), true);
            ConfigureRigidbody(root);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.55f;
            agent.height = 1.15f;

            var health = root.AddComponent<EnemyHealth>();
            var attack = root.AddComponent<EnemyMeleeAttack>();
            var mover = root.AddComponent<GroundNavMeshMover>();
            var controller = root.AddComponent<RetaliatingBoarController>();
            var healthBar = root.AddComponent<EnemyWorldHealthBar>();

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(config);
            mover.Configure(1.1f, Mathf.Max(0.1f, config.AttackRange - 0.05f), 0.55f, 1.15f, 0f, 12f, config.RotationSpeed, config.RepathInterval, 45);
            controller.Configure(null, health, attack, Vector3.zero, 9f, 1.1f, 4.8f, config.AttackRange);
            healthBar.Configure("Boar", new Vector3(0f, 1.35f, 0f));
            AddCorpseHarvest(root, health, visual != null ? visual.transform : root.transform, 20f, 300f, 120f, new[] { Grant(GreyMeatPath, 1), Grant(BonePath, 1) }, new[] { Grant(BonePath, 6), Grant(LeatherPath, 2), Grant(PetrifiedBloodPath, 1, 0.05f) }, 15, false);

            return SaveMobPrefabAndDefinition(root, $"{PrefabRoot}/Boar.prefab", "Boar", 0.55f, 3f);
        }

        private static MobSpawnDefinition BuildSparrow()
        {
            var root = CreateRoot("Sparrow");
            AttachVisual(root.transform, "Assets/_Project/Resources/Ambient/Sparrow/Sparrow.prefab", Vector3.zero, Quaternion.identity, Vector3.one);
            var controller = root.AddComponent<SparrowAmbientController>();
            controller.Configure(null, Vector3.zero, 6f, 0.8f, 5.5f, 5f, 6f, 10f);
            return SaveMobPrefabAndDefinition(root, $"{PrefabRoot}/Sparrow.prefab", "Sparrow", 0.15f, 0f);
        }

        private static void BuildTables(IReadOnlyList<MobSpawnDefinition> definitions)
        {
            var byName = definitions
                .Where(definition => definition != null)
                .ToDictionary(definition => definition.Prefab != null ? definition.Prefab.name : definition.name);
            ConfigureTable($"{SpawnRoot}/Tables/RuinsSkeletons.asset",
                Entry(byName, "SkeletonGrunt", 70f),
                Entry(byName, "SkeletonGuard", 20f),
                Entry(byName, "SkeletonBrute", 10f),
                Entry(byName, "SkeletonArcher", 12f));

            ConfigureTable($"{SpawnRoot}/Tables/AerialPredators.asset",
                Entry(byName, "Harpy", 1f));

            ConfigureTable($"{SpawnRoot}/Tables/Wildlife.asset",
                Entry(byName, "Deer", 35f),
                Entry(byName, "Horse", 12f),
                Entry(byName, "Chicken", 45f),
                Entry(byName, "Boar", 8f));

            ConfigureTable($"{SpawnRoot}/Tables/AmbientBirds.asset",
                Entry(byName, "Sparrow", 1f));
        }

        private static MobSpawnDefinition SaveMobPrefabAndDefinition(GameObject root, string prefabPath, string displayName, float clearanceRadius, float slotReleaseDelay)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            string definitionPath = $"{SpawnRoot}/Definitions/{Path.GetFileNameWithoutExtension(prefabPath)}Spawn.asset";
            var definition = AssetDatabase.LoadAssetAtPath<MobSpawnDefinition>(definitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MobSpawnDefinition>();
                AssetDatabase.CreateAsset(definition, definitionPath);
            }

            definition.ConfigureExistingPrefab(displayName, prefab, clearanceRadius, slotReleaseDelay);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void ConfigureTable(string path, params MobSpawnEntry[] entries)
        {
            var table = AssetDatabase.LoadAssetAtPath<MobSpawnTable>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<MobSpawnTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            table.ConfigureEntries(entries.Where(entry => entry.Definition != null).ToArray());
            EditorUtility.SetDirty(table);
        }

        private static MobSpawnEntry Entry(IReadOnlyDictionary<string, MobSpawnDefinition> definitions, string key, float weight)
        {
            definitions.TryGetValue(key, out MobSpawnDefinition definition);
            return new MobSpawnEntry { Definition = definition, Weight = weight };
        }

        private static GameObject CreateRoot(string name)
        {
            var root = new GameObject(name);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static GameObject AttachVisual(Transform parent, string prefabPath, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Mob Prefabs] Visual prefab was not found: {prefabPath}");
                return null;
            }

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            visual.name = "VisualRoot";
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;

            foreach (var collider in visual.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            foreach (var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled = false;

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                animator.enabled = true;
            }

            return visual;
        }

        private static void ForceMaterial(GameObject visual, string materialPath)
        {
            if (visual == null)
                return;

            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Debug.LogWarning($"[Mob Prefabs] Material was not found: {materialPath}");
                return;
            }

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
        }

        private static void AdjustBoarVisualHeight(GameObject visual)
        {
            if (visual == null)
                return;

            var boarModel = visual.transform.Find("Boar");
            var target = boarModel != null ? boarModel : visual.transform;
            var localPosition = target.localPosition;
            localPosition.y = 0.05f;
            target.localPosition = localPosition;
        }

        private static void ConfigureCapsule(GameObject root, float radius, float height, Vector3 center, bool trigger = false)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = radius;
            collider.height = height;
            collider.center = center;
            collider.direction = 1;
            collider.isTrigger = trigger;
        }

        private static void ConfigureRigidbody(GameObject root)
        {
            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
        }

        private static EnemyConfig LoadOrCreateConfig(string path)
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (config != null)
                return config;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            config = ScriptableObject.CreateInstance<EnemyConfig>();
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static EnemyAttackProfile[] CreateSkeletonMeleeProfiles(SkeletonVariant variant, AnimationClip[] attackClips)
        {
            if (variant.IsRanged || attackClips == null || attackClips.Length == 0)
                return new EnemyAttackProfile[0];

            return attackClips
                .Where(clip => clip != null)
                .Select(clip => new EnemyAttackProfile
                {
                    Enabled = true,
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
                })
                .ToArray();
        }

        private static AnimationClip LoadSkeletonClip(string folder, string clipFileName)
        {
            return FirstClip(LoadAllClips($"{SkeletonAnimationRoot}/{folder}", clipFileName));
        }

        private static AnimationClip[] LoadSkeletonClips(string folder, string prefix, string group)
        {
            var result = new List<AnimationClip>();
            foreach (string suffix in new[] { "A", "B", "C" })
            {
                var clip = LoadSkeletonClip(folder, $"{prefix}_{group}_{suffix}");
                if (clip != null)
                    result.Add(clip);
            }

            return result.ToArray();
        }

        private static AnimationClip[] LoadAllClips(string searchFolder, string clipFileName)
        {
            if (!Directory.Exists(searchFolder))
                return new AnimationClip[0];

            string[] guids = AssetDatabase.FindAssets(clipFileName, new[] { searchFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileNameWithoutExtension(path).Equals(clipFileName))
                    continue;

                return AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .Where(clip => clip != null && !clip.name.StartsWith("__preview__"))
                    .ToArray();
            }

            return new AnimationClip[0];
        }

        private static AnimationClip FirstClip(AnimationClip[] clips)
        {
            if (clips == null)
                return null;

            return clips.FirstOrDefault(clip => clip != null);
        }

        private static void AddCorpseHarvest(
            GameObject root,
            EnemyHealth health,
            Transform poseRoot,
            float healthPerBaseYield,
            float corpseLifetime,
            float corpseHealth,
            IEnumerable<CorpseItemGrant> baseYields,
            IEnumerable<CorpseItemGrant> completionDrops,
            int soulAshReward,
            bool scriptedDeathPose)
        {
            var corpse = root.AddComponent<AnimalCorpseHarvest>();
            corpse.Configure(health, root, poseRoot, corpseHealth, healthPerBaseYield, baseYields, completionDrops, soulAshReward, corpseLifetime, scriptedDeathPose);
        }

        private static CorpseItemGrant Grant(string itemPath, int amount, float chance = 1f)
        {
            return new CorpseItemGrant
            {
                Item = AssetDatabase.LoadAssetAtPath<ItemData>(itemPath),
                Amount = amount,
                Chance = chance
            };
        }

        private readonly struct SkeletonVariant
        {
            private SkeletonVariant(string assetName, string displayName, string visualPrefabName, string animationFolder, string clipPrefix, float maxHealth, int soulAshReward, float moveSpeed, float aggroRange, float loseTargetRange, float attackRange, float attackCooldown, float attackWindup, float healthDamage, float poiseDamage, DamageType damageType, bool isRanged)
            {
                AssetName = assetName;
                DisplayName = displayName;
                VisualPrefabName = visualPrefabName;
                AnimationFolder = animationFolder;
                ClipPrefix = clipPrefix;
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
            }

            public string AssetName { get; }
            public string DisplayName { get; }
            public string VisualPrefabName { get; }
            public string AnimationFolder { get; }
            public string ClipPrefix { get; }
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

            public static SkeletonVariant Grunt() => new SkeletonVariant("SkeletonGrunt", "Skeleton Grunt", "Skeleton_A", "1_one_handed", "DS_onehand", 52f, 14, 2.35f, 10f, 15f, 1.75f, 1.65f, 0.42f, 13f, 8f, DamageType.Slashing, false);
            public static SkeletonVariant Guard() => new SkeletonVariant("SkeletonGuard", "Skeleton Guard", "Skeleton_B", "2_shield", "DS_shield", 72f, 18, 1.9f, 9f, 14f, 1.8f, 2.1f, 0.5f, 11f, 12f, DamageType.Blunt, false);
            public static SkeletonVariant Brute() => new SkeletonVariant("SkeletonBrute", "Skeleton Brute", "Skeleton_TwoHanded_A", "3_two_handed", "DS_twohanded", 95f, 24, 1.75f, 11f, 16f, 2.05f, 2.8f, 0.68f, 22f, 18f, DamageType.Blunt, false);
            public static SkeletonVariant Archer() => new SkeletonVariant("SkeletonArcher", "Skeleton Archer", "Skeleton_archer_A", "4_bow", "DS_bow", 42f, 20, 2.25f, 15f, 22f, 1.6f, 2.3f, 0.55f, 18f, 16f, DamageType.Piercing, true);
        }

        private readonly struct AnimalVariant
        {
            private AnimalVariant(string assetName, string displayName, string visualPrefabPath, float maxHealth, float walkSpeed, float runSpeed, float scareRadius, float homeRadius, float colliderRadius, float colliderHeight, Vector3 healthBarOffset, float corpseHealth, int soulAshReward, CorpseItemGrant[] baseYields, CorpseItemGrant[] completionDrops)
            {
                AssetName = assetName;
                DisplayName = displayName;
                VisualPrefabPath = visualPrefabPath;
                MaxHealth = maxHealth;
                WalkSpeed = walkSpeed;
                RunSpeed = runSpeed;
                ScareRadius = scareRadius;
                HomeRadius = homeRadius;
                ColliderRadius = colliderRadius;
                ColliderHeight = colliderHeight;
                HealthBarOffset = healthBarOffset;
                CorpseHealth = corpseHealth;
                SoulAshReward = soulAshReward;
                BaseYields = baseYields;
                CompletionDrops = completionDrops;
            }

            public string AssetName { get; }
            public string DisplayName { get; }
            public string VisualPrefabPath { get; }
            public float MaxHealth { get; }
            public float WalkSpeed { get; }
            public float RunSpeed { get; }
            public float ScareRadius { get; }
            public float HomeRadius { get; }
            public float ColliderRadius { get; }
            public float ColliderHeight { get; }
            public Vector3 HealthBarOffset { get; }
            public float CorpseHealth { get; }
            public int SoulAshReward { get; }
            public CorpseItemGrant[] BaseYields { get; }
            public CorpseItemGrant[] CompletionDrops { get; }

            public static AnimalVariant Deer() => new AnimalVariant("Deer", "Deer", "Assets/_Project/Resources/Ambient/Animals/Deer_001.prefab", 35f, 1.2f, 6f, 9f, 12f, 0.55f, 1.7f, new Vector3(0f, 1.9f, 0f), 120f, 5, new[] { Grant(GreyMeatPath, 1), Grant(BonePath, 1) }, new[] { Grant(GreyMeatPath, 6), Grant(LeatherPath, 4) });
            public static AnimalVariant Horse() => new AnimalVariant("Horse", "Horse", "Assets/_Project/Resources/Ambient/Animals/Horse_001.prefab", 60f, 1.4f, 5f, 7f, 10f, 0.8f, 2.2f, new Vector3(0f, 2.45f, 0f), 200f, 10, new[] { Grant(BonePath, 1) }, new[] { Grant(BonePath, 2), Grant(LeatherPath, 1) });
            public static AnimalVariant Chicken() => new AnimalVariant("Chicken", "Chicken", "Assets/_Project/Resources/Ambient/Animals/Chicken_001.prefab", 10f, 0.8f, 3f, 4f, 5f, 0.22f, 0.55f, new Vector3(0f, 0.8f, 0f), 40f, 1, new[] { Grant(GreyMeatPath, 1), Grant(BonePath, 1) }, new[] { Grant(GreyMeatPath, 1), Grant(BonePath, 1) });
        }
    }
}
