using System.IO;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Loot;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Editor
{
    public static class GhoulZombieBossPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/GhoulZombieBoss.prefab";
        private const string ConfigPath = "Assets/_Project/Resources/Enemies/GhoulZombie/GhoulZombieBossConfig.asset";
        private const string VisualPrefabPath = "Assets/_GhoulZombie/Ghoul.prefab";
        private const string LootPath = "Assets/_Project/Resources/Loot/ToughEnemyLoot.asset";
        private const string AnimationPath = "Assets/_GhoulZombie/Ghoul@Animations.fbx";
        private static readonly Vector3 VisualLocalPosition = new Vector3(0f, -0.15f, 0f);
        private static readonly Vector3 HealthBarOffset = new Vector3(0f, 2f, 0f);

        [MenuItem("Project-S/Enemies/Rebuild Ghoul Zombie Boss Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

            EnsureAnimationImportSettings();

            var config = LoadOrCreateConfig();
            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
            var lootTable = AssetDatabase.LoadAssetAtPath<LootTableData>(LootPath);

            var attackProfiles = CreateAttackProfiles();
            var root = new GameObject("GhoulZombieBoss");
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            ConfigureHitbox(root);

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            var navMeshAgent = root.AddComponent<NavMeshAgent>();
            navMeshAgent.enabled = true;

            var health = root.AddComponent<EnemyHealth>();
            var attack = root.AddComponent<EnemyMeleeAttack>();
            var mover = root.AddComponent<GroundNavMeshMover>();
            var controller = root.AddComponent<EnemyController>();
            var healthBar = root.AddComponent<EnemyWorldHealthBar>();
            var lootDropper = root.AddComponent<LootDropper>();
            var animationController = root.AddComponent<EnemyAnimationController>();
            var setup = root.AddComponent<GhoulZombieBossSetup>();

            Transform visualRoot = null;
            Animator animator = null;
            if (visualPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                visual.name = "VisualRoot";
                visual.transform.localPosition = VisualLocalPosition;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                visualRoot = visual.transform;
                animator = visual.GetComponentInChildren<Animator>();

                foreach (var collider in visual.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                foreach (var legacyAnimation in visual.GetComponentsInChildren<Animation>())
                    legacyAnimation.enabled = false;

                if (animator == null)
                    animator = visual.AddComponent<Animator>();

                if (animator != null)
                {
                    animator.applyRootMotion = false;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }
            }

            var deathClip = LoadClip("Death");
            var idleClip = LoadClip("Idle");
            var walkClip = LoadClip("Run");
            var hitReactionClips = new AnimationClip[0];

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(config);
            attack.ConfigureAttackProfiles(attackProfiles, AttackSelectionMode.Cycle);
            mover.Configure(
                config.MoveSpeed,
                config.AttackRange - config.StoppingDistancePadding,
                config.AgentRadius,
                config.AgentHeight,
                config.AgentBaseOffset,
                Mathf.Max(8f, config.MoveSpeed * 4f),
                config.RotationSpeed,
                config.RepathInterval,
                30);
            controller.Configure(config, null);
            healthBar.Configure(config.DisplayName, HealthBarOffset);
            lootDropper.Configure(lootTable);
            animationController.Configure(
                controller,
                attack,
                health,
                visualRoot,
                animator,
                deathClip,
                idleClip,
                walkClip,
                null,
                null);
            animationController.ConfigureHitReactionClips(hitReactionClips);
            animationController.ConfigureGroundAnchoring(false);
            setup.Configure(
                config,
                lootTable,
                visualPrefab,
                visualRoot,
                animator,
                deathClip,
                idleClip,
                walkClip,
                hitReactionClips,
                attackProfiles,
                AttackSelectionMode.Cycle,
                HealthBarOffset);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Ghoul Zombie] Boss prefab rebuilt at {PrefabPath}.");
        }

        private static EnemyConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.DisplayName = "Ghoul Zombie";
            config.MaxHealth = 700f;
            config.SoulAshReward = 90;
            config.MoveSpeed = 4.4f;
            config.AggroRange = 18f;
            config.LoseTargetRange = 28f;
            config.AttackRange = 2.5f;
            config.RotationSpeed = 780f;
            config.AgentRadius = 0.9f;
            config.AgentHeight = 3f;
            config.AgentBaseOffset = 0f;
            config.MaxStepHeight = 0.45f;
            config.MaxSlope = 45f;
            config.StoppingDistancePadding = 0.1f;
            config.RepathInterval = 0.18f;
            config.AttackCooldown = 2.25f;
            config.AttackWindup = 0.5f;
            config.UseAttackClipDamageMoment = true;
            config.AttackDamageMomentNormalized = 0.5f;
            config.AttackRadius = 1.1f;
            config.HealthDamage = 26f;
            config.PoiseDamage = 18f;
            config.DamageType = DamageType.Slashing;
            config.DestroyDelayAfterDeath = 0f;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static EnemyAttackProfile[] CreateAttackProfiles()
        {
            return new[]
            {
                Profile("attack1", "Attack1", 13f, 9f, DamageType.Slashing, 2.1f, 0.45f, 0.48f, 2.45f, 1.05f, 1.25f, 2, 0.18f),
                Profile("attack2", "Attack2", 40f, 30f, DamageType.Slashing, 2.55f, 0.55f, 0.54f, 2.65f, 1.15f, 1.2f, 1, 0.12f)
            };
        }

        private static EnemyAttackProfile Profile(
            string id,
            string clipName,
            float healthDamage,
            float poiseDamage,
            DamageType damageType,
            float cooldown,
            float windup,
            float damageMoment,
            float range,
            float radius,
            float animationSpeed,
            int damageApplicationCount,
            float damageApplicationInterval)
        {
            return new EnemyAttackProfile
            {
                Enabled = true,
                Id = id,
                Clip = LoadClip(clipName),
                AnimationSpeed = animationSpeed,
                AttackCooldown = cooldown,
                AttackWindup = windup,
                UseAttackClipDamageMoment = true,
                AttackDamageMomentNormalized = damageMoment,
                AttackRange = range,
                AttackRadius = radius,
                HealthDamage = healthDamage,
                PoiseDamage = poiseDamage,
                DamageType = damageType,
                DamageApplicationCount = damageApplicationCount,
                DamageApplicationInterval = damageApplicationInterval
            };
        }

        private static AnimationClip LoadClip(string clipName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(AnimationPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null && clip.name == clipName);
        }

        private static void EnsureAnimationImportSettings()
        {
            var importer = AssetImporter.GetAtPath(AnimationPath) as ModelImporter;
            if (importer == null)
                return;

            if (importer.importAnimation && importer.animationType == ModelImporterAnimationType.Generic)
                return;

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.SaveAndReimport();
        }

        private static void ConfigureHitbox(GameObject root)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 1.6f;
            collider.height = 6f;
            collider.center = new Vector3(0f, 1.5f, 0f);
            collider.direction = 1;
        }
    }
}
