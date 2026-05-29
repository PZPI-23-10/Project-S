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
    public static class MinotaurBossPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/MinotaurBoss.prefab";
        private const string ConfigPath = "Assets/_Project/Resources/Enemies/Minotaur/MinotaurBossConfig.asset";
        private const string VisualPrefabPath = "Assets/minotaur1/Prefab/minotaur1.prefab";
        private const string LootPath = "Assets/_Project/Resources/Loot/ToughEnemyLoot.asset";

        [MenuItem("Project-S/Enemies/Rebuild Minotaur Boss Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

            var config = LoadOrCreateConfig();
            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
            var lootTable = AssetDatabase.LoadAssetAtPath<LootTableData>(LootPath);

            var attackProfiles = CreateAttackProfiles();
            var root = new GameObject("MinotaurBoss");
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
            var setup = root.AddComponent<MinotaurBossSetup>();

            Transform visualRoot = null;
            Animator animator = null;
            if (visualPrefab != null)
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, root.transform);
                visual.name = "VisualRoot";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                visualRoot = visual.transform;
                animator = visual.GetComponentInChildren<Animator>();

                foreach (var collider in visual.GetComponentsInChildren<Collider>())
                    collider.enabled = false;

                if (animator != null)
                {
                    animator.applyRootMotion = false;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                }
            }

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
            healthBar.Configure(config.DisplayName, new Vector3(0f, 3.4f, 0f));
            lootDropper.Configure(lootTable);
            animationController.Configure(
                controller,
                attack,
                health,
                visualRoot,
                animator,
                LoadClip("Assets/minotaur1/Animations/minotaur1@death.fbx"),
                LoadClip("Assets/minotaur1/Animations/minotaur1@idle.fbx"),
                LoadClip("Assets/minotaur1/Animations/minotaur1@run.fbx"),
                LoadClip("Assets/minotaur1/Animations/minotaur1@hit_1.FBX"),
                null);
            animationController.ConfigureHitReactionClips(new[]
            {
                LoadClip("Assets/minotaur1/Animations/minotaur1@hit_1.FBX"),
                LoadClip("Assets/minotaur1/Animations/minotaur1@hit_2.fbx")
            });
            setup.Configure(
                config,
                lootTable,
                visualPrefab,
                visualRoot,
                animator,
                LoadClip("Assets/minotaur1/Animations/minotaur1@death.fbx"),
                LoadClip("Assets/minotaur1/Animations/minotaur1@idle.fbx"),
                LoadClip("Assets/minotaur1/Animations/minotaur1@run.fbx"),
                new[]
                {
                    LoadClip("Assets/minotaur1/Animations/minotaur1@hit_1.FBX"),
                    LoadClip("Assets/minotaur1/Animations/minotaur1@hit_2.fbx")
                },
                attackProfiles,
                AttackSelectionMode.Cycle,
                new Vector3(0f, 3.4f, 0f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Minotaur] Boss prefab rebuilt at {PrefabPath}.");
        }

        private static EnemyConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.DisplayName = "\u041c\u0438\u043d\u043e\u0442\u0430\u0432\u0440";
            config.MaxHealth = 650f;
            config.SoulAshReward = 80;
            config.MoveSpeed = 2.8f;
            config.AggroRange = 18f;
            config.LoseTargetRange = 28f;
            config.AttackRange = 2.4f;
            config.RotationSpeed = 620f;
            config.AgentRadius = 0.85f;
            config.AgentHeight = 3.2f;
            config.AgentBaseOffset = 0f;
            config.MaxStepHeight = 0.45f;
            config.MaxSlope = 45f;
            config.StoppingDistancePadding = 0.1f;
            config.RepathInterval = 0.18f;
            config.AttackCooldown = 2.4f;
            config.AttackWindup = 0.55f;
            config.UseAttackClipDamageMoment = true;
            config.AttackDamageMomentNormalized = 0.48f;
            config.AttackRadius = 1.05f;
            config.HealthDamage = 30f;
            config.PoiseDamage = 20f;
            config.DamageType = DamageType.Slashing;
            config.DestroyDelayAfterDeath = 0f;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static EnemyAttackProfile[] CreateAttackProfiles()
        {
            return new[]
            {
                Profile("attack1", "Assets/minotaur1/Animations/minotaur1@attack1.fbx", 22f, 14f, DamageType.Slashing, 2.1f, 0.42f, 0.44f, 2.4f, 0.95f),
                Profile("attack2", "Assets/minotaur1/Animations/minotaur1@attack2.fbx", 30f, 18f, DamageType.Slashing, 2.4f, 0.48f, 0.48f, 2.5f, 1.05f),
                Profile("attack3", "Assets/minotaur1/Animations/minotaur1@attack3.fbx", 42f, 28f, DamageType.Slashing, 3.1f, 0.62f, 0.55f, 2.65f, 1.25f),
                Profile("attack4_kick", "Assets/minotaur1/Animations/minotaur1@attack4_kick.fbx", 26f, 22f, DamageType.Blunt, 2.2f, 0.4f, 0.46f, 2.25f, 0.9f),
                Profile("attack5_kick", "Assets/minotaur1/Animations/minotaur1@attack5_kick.fbx", 34f, 30f, DamageType.Blunt, 2.7f, 0.52f, 0.5f, 2.35f, 1f)
            };
        }

        private static EnemyAttackProfile Profile(
            string id,
            string clipPath,
            float healthDamage,
            float poiseDamage,
            DamageType damageType,
            float cooldown,
            float windup,
            float damageMoment,
            float range,
            float radius)
        {
            return new EnemyAttackProfile
            {
                Enabled = true,
                Id = id,
                Clip = LoadClip(clipPath),
                AttackCooldown = cooldown,
                AttackWindup = windup,
                UseAttackClipDamageMoment = true,
                AttackDamageMomentNormalized = damageMoment,
                AttackRange = range,
                AttackRadius = radius,
                HealthDamage = healthDamage,
                PoiseDamage = poiseDamage,
                DamageType = damageType
            };
        }

        private static AnimationClip LoadClip(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => clip != null && !clip.name.StartsWith("__preview__"));
        }

        private static void ConfigureHitbox(GameObject root)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 0.85f;
            collider.height = 3.2f;
            collider.center = new Vector3(0f, 1.6f, 0f);
            collider.direction = 1;
        }
    }
}
