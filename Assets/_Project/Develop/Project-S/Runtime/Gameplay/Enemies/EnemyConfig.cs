using Project_S.Runtime.Gameplay.Character.Combat;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "New Enemy Config", menuName = "Project-S/Enemies/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Identity")]
        public string DisplayName = "Enemy";

        [Header("Stats")]
        [Min(1f)] public float MaxHealth = 45f;
        [Min(0f)] public int SoulAshReward = 12;

        [Header("Movement")]
        [Min(0f)] public float MoveSpeed = 2.2f; 
        [Min(0f)] public float AggroRange = 9f;
        [Min(0f)] public float LoseTargetRange = 13f;
        [Min(0f)] public float AttackRange = 1.7f;
        [Min(0f)] public float RotationSpeed = 540f;

        [Header("NavMesh")]
        [Min(0.01f)] public float AgentRadius = 0.5f;
        [Min(0.01f)] public float AgentHeight = 2f;
        public float AgentBaseOffset = 0f;
        [Min(0f)] public float MaxStepHeight = 0.4f;
        [Range(0f, 90f)] public float MaxSlope = 45f;
        [Min(0f)] public float StoppingDistancePadding = 0.05f;
        [Min(0.02f)] public float RepathInterval = 0.2f;

        [Header("Attack")]
        [Min(0f)] public float AttackCooldown = 1.8f;
        [Min(0f)] public float AttackWindup = 0.45f;
        public bool UseAttackClipDamageMoment = false;
        [Range(0f, 1f)] public float AttackDamageMomentNormalized = 0.5f;
        [Min(0f)] public float AttackRadius = 0.55f;
        [Min(0f)] public float HealthDamage = 12f;
        [Min(0f)] public float PoiseDamage = 8f;
        public DamageType DamageType = DamageType.Blunt;

        [Header("Ranged Attack")]
        public bool UseRangedAttack = false;
        [Min(0f)] public float RangedAttackRange = 10f;
        [Min(0f)] public float RangedPreferredDistance = 8f;
        [Min(0f)] public float RangedRetreatDistance = 4f;
        [Min(0f)] public float RangedAttackCooldown = 2.4f;
        [Min(0f)] public float RangedAttackWindup = 0.55f;
        public bool UseRangedAttackClipDamageMoment = true;
        [Range(0f, 1f)] public float RangedAttackDamageMomentNormalized = 0.58f;
        [Min(0.01f)] public float RangedAttackAnimationSpeed = 1f;
        [Min(0.01f)] public float RangedProjectileSpeed = 16f;
        [Min(0.01f)] public float RangedProjectileLifetime = 4f;
        [Min(0.01f)] public float RangedProjectileRadius = 0.08f;
        [Min(0f)] public float RangedHealthDamage = 9f;
        [Min(0f)] public float RangedPoiseDamage = 4f;
        public DamageType RangedDamageType = DamageType.Piercing;

        [Header("Death")]
        [Min(0f)] public float DestroyDelayAfterDeath = 0f;
    }
}
