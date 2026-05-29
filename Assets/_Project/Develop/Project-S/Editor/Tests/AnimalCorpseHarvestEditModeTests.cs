using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Project_S.Runtime.Gameplay.Ambient;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Movement;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Enemies;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Project_S.Editor.Tests
{
    public class AnimalCorpseHarvestEditModeTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            foreach (var pickup in Object.FindObjectsOfType<ItemPickup>())
                Object.DestroyImmediate(pickup.gameObject);

            foreach (var obj in _objects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _objects.Clear();
        }

        [Test]
        public void Corpse_GrantsBaseYieldPerTwentyDamage()
        {
            var meat = CreateItem("Grey Meat");
            var bone = CreateItem("Bone");
            var corpse = CreateCorpse(
                maxHealth: 40f,
                baseYields: new[]
                {
                    Grant(meat, 1),
                    Grant(bone, 1)
                },
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0);
            var player = CreatePlayer(8, out var inventory, out _);

            corpse.ActivateCorpse();
            corpse.ReceiveDamage(new DamageRequest(player, 19f, 0f, DamageType.Slashing));
            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(0));
            Assert.That(inventory.GetItemCount(bone), Is.EqualTo(0));

            corpse.ReceiveDamage(new DamageRequest(player, 1f, 0f, DamageType.Slashing));
            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(1));
            Assert.That(inventory.GetItemCount(bone), Is.EqualTo(1));
        }

        [Test]
        public void Corpse_GrantsCompletionLootAndSoulAshOnlyWhenDepleted()
        {
            var meat = CreateItem("Grey Meat");
            var leather = CreateItem("Leather");
            var corpse = CreateCorpse(
                maxHealth: 40f,
                baseYields: new[] { Grant(meat, 1) },
                completionDrops: new[] { Grant(leather, 2) },
                soulAsh: 5);
            var player = CreatePlayer(8, out var inventory, out var wallet);

            corpse.ActivateCorpse();
            corpse.ReceiveDamage(new DamageRequest(player, 20f, 0f, DamageType.Slashing));
            Assert.That(inventory.GetItemCount(leather), Is.EqualTo(0));
            Assert.That(wallet.Amount, Is.EqualTo(0));

            corpse.ReceiveDamage(new DamageRequest(player, 20f, 0f, DamageType.Slashing));
            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(2));
            Assert.That(inventory.GetItemCount(leather), Is.EqualTo(2));
            Assert.That(wallet.Amount, Is.EqualTo(5));
        }

        [Test]
        public void Corpse_HazardSourceDoesNotGrantPlayerSoulAsh()
        {
            var bone = CreateItem("Bone");
            CreatePlayer(8, out _, out var playerWallet);
            var hazard = new GameObject("Hazard");
            _objects.Add(hazard);
            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new[] { Grant(bone, 1) },
                soulAsh: 10);

            corpse.ActivateCorpse();
            corpse.ReceiveDamage(new DamageRequest(hazard, 20f, 0f, DamageType.Blunt));

            Assert.That(playerWallet.Amount, Is.EqualTo(0));
        }

        [Test]
        public void Corpse_SpawnsPickupWhenInventoryIsFull()
        {
            var blocker = CreateItem("Blocker", stackable: false, maxStack: 1);
            var bone = CreateItem("Bone");
            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new[] { Grant(bone, 1) },
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0);
            var player = CreatePlayer(1, out var inventory, out _);
            inventory.AddItem(blocker, 1);

            corpse.ActivateCorpse();
            corpse.ReceiveDamage(new DamageRequest(player, 20f, 0f, DamageType.Blunt));

            var pickups = Object.FindObjectsOfType<ItemPickup>();
            Assert.That(pickups, Has.Length.EqualTo(1));
            Assert.That(pickups[0].Item, Is.EqualTo(bone));
            Assert.That(pickups[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void Corpse_LifetimeIsConfiguredToFiveMinutes()
        {
            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0,
                lifetimeSeconds: 300f);

            Assert.That(corpse.CorpseLifetimeSeconds, Is.EqualTo(300f).Within(0.001f));
        }

        [Test]
        public void Corpse_UsesRootHitboxWhenActivated()
        {
            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0,
                owner: out var owner,
                scriptedDeathPose: true);
            var rootCollider = owner.GetComponent<Collider>();

            Assert.That(corpse.GetComponent<Collider>(), Is.SameAs(rootCollider));
            Assert.That(corpse.IsHitboxEnabled, Is.True);

            corpse.ActivateCorpse();

            Assert.That(corpse.IsHitboxEnabled, Is.True);
            Assert.That(rootCollider.enabled, Is.True);
            Assert.That(rootCollider.isTrigger, Is.True);
        }

        [Test]
        public void Corpse_DisablesLivingMoverWhenActivated()
        {
            var owner = new GameObject("Animal");
            _objects.Add(owner);
            owner.AddComponent<CapsuleCollider>().isTrigger = true;
            var mover = owner.AddComponent<GroundNavMeshMover>();
            var corpse = owner.AddComponent<AnimalCorpseHarvest>();
            corpse.Configure(
                null,
                owner,
                owner.transform,
                20f,
                20f,
                new CorpseItemGrant[0],
                new CorpseItemGrant[0],
                0,
                300f,
                scriptedDeathPose: false);

            corpse.ActivateCorpse();

            Assert.That(mover.enabled, Is.False);
        }

        [Test]
        public void DeadEnemyHealth_ForwardsDamageToCorpseHarvest()
        {
            var meat = CreateItem("Grey Meat");
            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new[] { Grant(meat, 1) },
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0,
                owner: out var owner);
            var health = owner.AddComponent<EnemyHealth>();
            var player = CreatePlayer(8, out var inventory, out _);

            health.SetDestroyAfterDeath(false);
            corpse.Configure(
                health,
                owner,
                owner.transform,
                20f,
                20f,
                new[] { Grant(meat, 1) },
                new CorpseItemGrant[0],
                0,
                300f,
                scriptedDeathPose: false);

            health.ReceiveDamage(new DamageRequest(player, health.MaxHealth, 0f, DamageType.Slashing));
            health.ReceiveDamage(new DamageRequest(player, 20f, 0f, DamageType.Slashing));

            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(1));
        }

        [Test]
        public void RootCollider_RemainsEnabledAfterCorpseActivation()
        {
            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0,
                owner: out var owner);
            var rootCollider = owner.GetComponent<Collider>();

            corpse.ActivateCorpse();

            Assert.That(rootCollider.enabled, Is.True);
            Assert.That(rootCollider.isTrigger, Is.True);
        }

        [Test]
        public void Corpse_DoesNotRequireChildHarvestHitbox()
        {
            CreateCorpse(
                maxHealth: 20f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0,
                owner: out var owner);

            Assert.That(owner.transform.Find("Corpse Harvest Hitbox"), Is.Null);
        }

        [Test]
        public void CharacterMotor_IgnoresCorpseHitboxForMovementCollision()
        {
            LogAssert.ignoreFailingMessages = true;

            var player = new GameObject("Player Motor");
            _objects.Add(player);
            var motor = player.AddComponent<CharacterMotor>();

            var regularObject = new GameObject("Wall");
            _objects.Add(regularObject);
            var regularCollider = regularObject.AddComponent<BoxCollider>();

            var corpse = CreateCorpse(
                maxHealth: 20f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new CorpseItemGrant[0],
                soulAsh: 0);
            var corpseCollider = corpse.GetComponent<Collider>();

            Assert.That(motor.IsColliderValidForCollisions(regularCollider), Is.True);
            Assert.That(motor.IsColliderValidForCollisions(corpseCollider), Is.False);
        }

        [Test]
        public void CharacterMotor_IgnoresEnemyHitboxDuringAttackDash()
        {
            LogAssert.ignoreFailingMessages = true;

            var player = new GameObject("Player Motor");
            _objects.Add(player);
            var motor = player.AddComponent<CharacterMotor>();

            var enemy = new GameObject("Enemy");
            _objects.Add(enemy);
            var enemyCollider = enemy.AddComponent<CapsuleCollider>();
            enemy.AddComponent<EnemyHealth>();

            Assert.That(motor.IsColliderValidForCollisions(enemyCollider), Is.True);

            motor.ForceAttackDash(10f, 1f, 0f);

            Assert.That(motor.IsColliderValidForCollisions(enemyCollider), Is.False);
        }

        [Test]
        public void EnemyHealth_CancelsPendingMeleeAttackOnDeath()
        {
            var config = CreateEnemyConfig();
            var enemy = new GameObject("Enemy");
            _objects.Add(enemy);
            var health = enemy.AddComponent<EnemyHealth>();
            var meleeAttack = enemy.AddComponent<EnemyMeleeAttack>();
            var target = new GameObject("Target");
            _objects.Add(target);

            health.Configure(config);
            meleeAttack.Configure(config);

            Assert.That(meleeAttack.TryAttack(target.transform), Is.True);
            Assert.That(meleeAttack.IsWindingUp, Is.True);

            health.ReceiveDamage(new DamageRequest(target, 999f, 0f, DamageType.Slashing));

            Assert.That(meleeAttack.IsWindingUp, Is.False);
            Assert.That(meleeAttack.enabled, Is.False);
        }

        [Test]
        public void EnemyMeleeAttack_UsesCurrentAttackProfileDamage()
        {
            var config = CreateEnemyConfig();
            config.AttackWindup = 0f;
            config.AttackCooldown = 0f;
            config.AttackRange = 2f;
            config.AttackRadius = 0.5f;
            config.HealthDamage = 5f;

            var enemy = new GameObject("Profiled Enemy");
            _objects.Add(enemy);
            var meleeAttack = enemy.AddComponent<EnemyMeleeAttack>();
            meleeAttack.Configure(config);
            meleeAttack.ConfigureAttackProfiles(new[]
            {
                new EnemyAttackProfile
                {
                    Id = "attack1",
                    AttackWindup = 0f,
                    AttackCooldown = 0f,
                    AttackRange = 2f,
                    AttackRadius = 0.5f,
                    HealthDamage = 22f,
                    PoiseDamage = 14f,
                    DamageType = DamageType.Slashing
                },
                new EnemyAttackProfile
                {
                    Id = "attack2",
                    AttackWindup = 0f,
                    AttackCooldown = 0f,
                    AttackRange = 2f,
                    AttackRadius = 0.5f,
                    HealthDamage = 30f,
                    PoiseDamage = 18f,
                    DamageType = DamageType.Slashing
                }
            });

            var target = new GameObject("Target");
            _objects.Add(target);
            target.transform.position = enemy.transform.position + Vector3.forward;
            var receiver = target.AddComponent<CaptureDamageReceiver>();

            Assert.That(meleeAttack.TryAttack(target.transform), Is.True);
            Assert.That(receiver.LastRequest.HealthDamage, Is.EqualTo(22f).Within(0.001f));
            Assert.That(receiver.LastRequest.PoiseDamage, Is.EqualTo(14f).Within(0.001f));

            Assert.That(meleeAttack.TryAttack(target.transform), Is.True);
            Assert.That(receiver.LastRequest.HealthDamage, Is.EqualTo(30f).Within(0.001f));
            Assert.That(receiver.LastRequest.PoiseDamage, Is.EqualTo(18f).Within(0.001f));
        }

        [Test]
        public void EnemyMeleeAttack_WithoutProfilesUsesEnemyConfigDamage()
        {
            var config = CreateEnemyConfig();
            config.AttackWindup = 0f;
            config.AttackCooldown = 0f;
            config.AttackRange = 2f;
            config.AttackRadius = 0.5f;
            config.HealthDamage = 17f;
            config.PoiseDamage = 9f;
            config.DamageType = DamageType.Blunt;

            var enemy = new GameObject("Classic Enemy");
            _objects.Add(enemy);
            var meleeAttack = enemy.AddComponent<EnemyMeleeAttack>();
            meleeAttack.Configure(config);

            var target = new GameObject("Target");
            _objects.Add(target);
            target.transform.position = enemy.transform.position + Vector3.forward;
            var receiver = target.AddComponent<CaptureDamageReceiver>();

            Assert.That(meleeAttack.TryAttack(target.transform), Is.True);
            Assert.That(receiver.LastRequest.HealthDamage, Is.EqualTo(17f).Within(0.001f));
            Assert.That(receiver.LastRequest.PoiseDamage, Is.EqualTo(9f).Within(0.001f));
            Assert.That(receiver.LastRequest.Type, Is.EqualTo(DamageType.Blunt));
        }

        [Test]
        public void EnemyController_KeepsRootColliderAsTriggerForCorpseHarvest()
        {
            var config = CreateEnemyConfig();
            var enemy = new GameObject("Enemy Corpse");
            _objects.Add(enemy);
            var rootCollider = enemy.AddComponent<CapsuleCollider>();
            var health = enemy.AddComponent<EnemyHealth>();
            var meleeAttack = enemy.AddComponent<EnemyMeleeAttack>();
            enemy.AddComponent<GroundNavMeshMover>();
            enemy.AddComponent<EnemyController>();
            var corpse = enemy.AddComponent<AnimalCorpseHarvest>();

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            meleeAttack.Configure(config);
            corpse.Configure(
                health,
                enemy,
                enemy.transform,
                20f,
                20f,
                new CorpseItemGrant[0],
                new CorpseItemGrant[0],
                0,
                300f,
                scriptedDeathPose: false);

            Assert.That(rootCollider.isTrigger, Is.False);

            health.ReceiveDamage(new DamageRequest(enemy, 999f, 0f, DamageType.Slashing));

            Assert.That(corpse.IsActive, Is.True);
            Assert.That(rootCollider.enabled, Is.True);
            Assert.That(rootCollider.isTrigger, Is.True);
        }

        [Test]
        public void HarpyCorpseProfile_GrantsBaseYieldAndLeatherWithoutSoulAsh()
        {
            var meat = CreateItem("Grey Meat");
            var bone = CreateItem("Bone");
            var leather = CreateItem("Leather");
            var corpse = CreateCorpse(
                maxHealth: 80f,
                baseYields: new[]
                {
                    Grant(meat, 1),
                    Grant(bone, 1)
                },
                completionDrops: new[] { Grant(leather, 1) },
                soulAsh: 0);
            var player = CreatePlayer(16, out var inventory, out var wallet);

            corpse.ActivateCorpse();
            for (int i = 0; i < 4; i++)
                corpse.ReceiveDamage(new DamageRequest(player, 20f, 0f, DamageType.Slashing));

            Assert.That(inventory.GetItemCount(meat), Is.EqualTo(4));
            Assert.That(inventory.GetItemCount(bone), Is.EqualTo(4));
            Assert.That(inventory.GetItemCount(leather), Is.EqualTo(1));
            Assert.That(wallet.Amount, Is.EqualTo(0));
        }

        [Test]
        public void HarpyCorpseProfile_ConfiguresRarePetrifiedBloodChance()
        {
            var blood = CreateItem("Petrified Blood");
            var corpse = CreateCorpse(
                maxHealth: 80f,
                baseYields: new CorpseItemGrant[0],
                completionDrops: new[] { Grant(blood, 1, 0.1f) },
                soulAsh: 0);

            var drops = GetPrivateField<List<CorpseItemGrant>>(corpse, "_completionDrops");

            Assert.That(drops, Has.Count.EqualTo(1));
            Assert.That(drops[0].Item, Is.EqualTo(blood));
            Assert.That(drops[0].Chance, Is.EqualTo(0.1f).Within(0.001f));
        }

        [Test]
        public void FlyingEnemy_KeepsCorpseKinematicForScriptedLanding()
        {
            var config = CreateEnemyConfig();
            var harpy = new GameObject("Harpy Corpse");
            _objects.Add(harpy);
            harpy.transform.position = new Vector3(0f, 12f, 0f);
            harpy.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            var deathRotation = harpy.transform.rotation;
            var collider = harpy.AddComponent<SphereCollider>();
            var rigidbody = harpy.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            var health = harpy.AddComponent<EnemyHealth>();
            var attack = harpy.AddComponent<EnemyMeleeAttack>();
            var controller = harpy.AddComponent<FlyingEnemyController>();
            var corpse = harpy.AddComponent<AnimalCorpseHarvest>();

            health.Configure(config);
            health.SetDestroyAfterDeath(false);
            attack.Configure(config);
            controller.Configure(config, null, 12f, 24f, 1.2f, 0.35f);
            corpse.Configure(
                health,
                harpy,
                harpy.transform,
                80f,
                20f,
                new CorpseItemGrant[0],
                new CorpseItemGrant[0],
                0,
                300f,
                scriptedDeathPose: true,
                groundOffset: 0.08f,
                waitForExternalDeathPose: true);

            health.ReceiveDamage(new DamageRequest(harpy, 999f, 0f, DamageType.Slashing));

            Assert.That(corpse.IsActive, Is.True);
            Assert.That(rigidbody.isKinematic, Is.True);
            Assert.That(rigidbody.useGravity, Is.False);
            Assert.That(collider.enabled, Is.False);
            Assert.That(harpy.transform.position.y, Is.EqualTo(12f).Within(0.001f));
            Assert.That(Quaternion.Angle(harpy.transform.rotation, deathRotation), Is.EqualTo(0f).Within(0.001f));

            harpy.transform.position = new Vector3(0f, 0.08f, 0f);
            corpse.CompleteExternalDeathPose(applyScriptedPose: false);

            Assert.That(collider.enabled, Is.True);
            Assert.That(collider.isTrigger, Is.True);
            Assert.That(harpy.transform.position.y, Is.EqualTo(0.08f).Within(0.001f));
            Assert.That(Quaternion.Angle(harpy.transform.rotation, deathRotation), Is.EqualTo(0f).Within(0.001f));
        }

        private AnimalCorpseHarvest CreateCorpse(
            float maxHealth,
            IEnumerable<CorpseItemGrant> baseYields,
            IEnumerable<CorpseItemGrant> completionDrops,
            int soulAsh,
            float lifetimeSeconds = 300f,
            bool scriptedDeathPose = false)
        {
            return CreateCorpse(maxHealth, baseYields, completionDrops, soulAsh, out _, lifetimeSeconds, scriptedDeathPose);
        }

        private AnimalCorpseHarvest CreateCorpse(
            float maxHealth,
            IEnumerable<CorpseItemGrant> baseYields,
            IEnumerable<CorpseItemGrant> completionDrops,
            int soulAsh,
            out GameObject owner,
            float lifetimeSeconds = 300f,
            bool scriptedDeathPose = false)
        {
            owner = new GameObject("Animal");
            _objects.Add(owner);
            var hitbox = owner.AddComponent<CapsuleCollider>();
            hitbox.isTrigger = true;
            var corpse = owner.AddComponent<AnimalCorpseHarvest>();
            corpse.Configure(
                null,
                owner,
                owner.transform,
                maxHealth,
                20f,
                baseYields,
                completionDrops,
                soulAsh,
                lifetimeSeconds,
                scriptedDeathPose);
            return corpse;
        }

        private CorpseItemGrant Grant(ItemData item, int amount, float chance = 1f)
        {
            return new CorpseItemGrant
            {
                Item = item,
                Amount = amount,
                Chance = chance
            };
        }

        private ItemData CreateItem(string itemName, bool stackable = true, int maxStack = 20)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            item.ItemName = itemName;
            item.IsStackable = stackable;
            item.MaxStack = maxStack;
            _objects.Add(item);
            return item;
        }

        private EnemyConfig CreateEnemyConfig()
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.MaxHealth = 10f;
            config.AttackWindup = 10f;
            config.AttackCooldown = 1f;
            config.AttackRange = 1f;
            config.AttackRadius = 0.5f;
            _objects.Add(config);
            return config;
        }

        private GameObject CreatePlayer(int inventorySize, out InventoryController inventory, out SoulAshWallet wallet)
        {
            var player = new GameObject("Player");
            _objects.Add(player);

            inventory = player.AddComponent<InventoryController>();
            SetPrivateField(inventory, "_inventorySize", inventorySize);
            SetPrivateField(inventory, "_slots", new ItemStack[inventorySize]);

            wallet = player.AddComponent<SoulAshWallet>();
            return player;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field {fieldName} was not found.");
            return (T)field.GetValue(target);
        }

        private sealed class CaptureDamageReceiver : MonoBehaviour, IDamageReceiver
        {
            public DamageRequest LastRequest;

            public void ReceiveDamage(DamageRequest request)
            {
                LastRequest = request;
            }
        }
    }
}
