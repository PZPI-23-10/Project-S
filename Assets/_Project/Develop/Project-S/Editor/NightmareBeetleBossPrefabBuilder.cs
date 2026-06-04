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
    public static class NightmareBeetleBossPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/NightmareBeetleBoss.prefab";
        private const string ConfigPath = "Assets/_Project/Resources/Enemies/NightmareBeetle/NightmareBeetleBossConfig.asset";
        private const string VisualPrefabPath = "Assets/Nightmare Beetle/prefab/Nightmare Beetle.prefab";
        private const string LootPath = "Assets/_Project/Resources/Loot/ToughEnemyLoot.asset";
        private const string AnimationPath = "Assets/Nightmare Beetle/animation/";

        [MenuItem("Project-S/Enemies/Rebuild Nightmare Beetle Boss Prefab")]
        public static void Build()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

            var config = LoadOrCreateConfig();
            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
            var lootTable = AssetDatabase.LoadAssetAtPath<LootTableData>(LootPath);

            var attackProfiles = CreateAttackProfiles();
            var root = new GameObject("NightmareBeetleBoss");
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
            var setup = root.AddComponent<NightmareBeetleBossSetup>();

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

            var deathClip = LoadClip(AnimationPath + "bug@death.fbx");
            var idleClip = LoadClip(AnimationPath + "bug@idle1.fbx");
            var walkClip = LoadClip(AnimationPath + "bug@run.fbx");
            var hitReactionClips = new[]
            {
                LoadClip(AnimationPath + "bug@gethit.fbx")
            };

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
            healthBar.Configure(config.DisplayName, new Vector3(0f, 2.8f, 0f));
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
                FirstClip(hitReactionClips),
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
                new Vector3(0f, 2.8f, 0f));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Nightmare Beetle] Boss prefab rebuilt at {PrefabPath}.");
        }

        private static EnemyConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.DisplayName = "Nightmare Beetle";
            config.MaxHealth = 750f;
            config.SoulAshReward = 95;
            config.MoveSpeed = 2.6f;
            config.AggroRange = 18f;
            config.LoseTargetRange = 28f;
            config.AttackRange = 10f;
            config.RotationSpeed = 600f;
            config.AgentRadius = 1.1f;
            config.AgentHeight = 2.4f;
            config.AgentBaseOffset = 0f;
            config.MaxStepHeight = 0.45f;
            config.MaxSlope = 45f;
            config.StoppingDistancePadding = 0.1f;
            config.RepathInterval = 0.18f;
            config.AttackCooldown = 2.2f;
            config.AttackWindup = 0.5f;
            config.UseAttackClipDamageMoment = true;
            config.AttackDamageMomentNormalized = 0.5f;
            config.AttackRadius = 10f;
            config.HealthDamage = 32f;
            config.PoiseDamage = 24f;
            config.DamageType = DamageType.Blunt;
            config.DestroyDelayAfterDeath = 0f;
            EditorUtility.SetDirty(config);
            return config;
        }

        private static EnemyAttackProfile[] CreateAttackProfiles()
        {
            return new[]
            {
                Profile("atack1", AnimationPath + "bug@atack1.fbx", 28f, 18f, DamageType.Blunt, 2f, 0.42f, 0.46f, 10f, 10f, 1.8f),
                Profile("atack2", AnimationPath + "bug@atack2.fbx", 38f, 26f, DamageType.Slashing, 2.35f, 0.5f, 0.52f, 10f, 10f, 1.75f),
                Profile("atack3", AnimationPath + "bug@atack3.fbx", 46f, 34f, DamageType.Blunt, 2.7f, 0.6f, 0.58f, 10f, 10f, 1.65f)
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
            float radius,
            float animationSpeed)
        {
            return new EnemyAttackProfile
            {
                Enabled = true,
                Id = id,
                Clip = LoadClip(clipPath),
                AnimationSpeed = animationSpeed,
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

        private static void ConfigureHitbox(GameObject root)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = 1.6f;
            collider.height = 3.2f;
            collider.center = new Vector3(0f, 1.6f, 0f);
            collider.direction = 1;
            collider.isTrigger = true;
        }
    }
}
