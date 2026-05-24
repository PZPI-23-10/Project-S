using System.Collections.Generic;
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
            DamageProfile = new[]
            {
                new DamageInstance { Type = type, Amount = healthDamage }
            };
        }

        public DamageRequest(GameObject source, IReadOnlyList<DamageInstance> damageProfile, float poiseDamage, WeaponItemData weapon = null)
        {
            Source = source;
            DamageProfile = damageProfile ?? System.Array.Empty<DamageInstance>();
            HealthDamage = CalculateTotalDamage(DamageProfile);
            PoiseDamage = poiseDamage;
            Type = ResolvePrimaryType(DamageProfile);
            Weapon = weapon;
        }

        public GameObject Source { get; }
        public float HealthDamage { get; }
        public float PoiseDamage { get; }
        public DamageType Type { get; }
        public WeaponItemData Weapon { get; }
        public IReadOnlyList<DamageInstance> DamageProfile { get; }

        private static float CalculateTotalDamage(IReadOnlyList<DamageInstance> profile)
        {
            float total = 0f;
            if (profile == null)
                return total;

            for (int i = 0; i < profile.Count; i++)
            {
                if (profile[i].Amount > 0f)
                    total += profile[i].Amount;
            }

            return total;
        }

        private static DamageType ResolvePrimaryType(IReadOnlyList<DamageInstance> profile)
        {
            if (profile == null)
                return DamageType.Blunt;

            for (int i = 0; i < profile.Count; i++)
            {
                if (profile[i].Amount > 0f)
                    return profile[i].Type;
            }

            return DamageType.Blunt;
        }
    }
}
