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

        [Header("Attack")]
        [Min(0f)] public float AttackCooldown = 1.8f;
        [Min(0f)] public float AttackWindup = 0.45f;
        [Min(0f)] public float AttackRadius = 0.55f;
        [Min(0f)] public float HealthDamage = 12f;
        [Min(0f)] public float PoiseDamage = 8f;
        public DamageType DamageType = DamageType.Blunt;

        [Header("Death")]
        [Min(0f)] public float DestroyDelayAfterDeath = 0f;
    }
}
