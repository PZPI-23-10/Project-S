using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public readonly struct DamageRequest
    {
        public DamageRequest(GameObject source, float healthDamage, float poiseDamage, DamageType type, WeaponItemData weapon = null)
        {
            Source = source;
            HealthDamage = healthDamage;
            PoiseDamage = poiseDamage;
            Type = type;
            Weapon = weapon;
        }

        public GameObject Source { get; }
        public float HealthDamage { get; }
        public float PoiseDamage { get; }
        public DamageType Type { get; }
        public WeaponItemData Weapon { get; }
    }
}
