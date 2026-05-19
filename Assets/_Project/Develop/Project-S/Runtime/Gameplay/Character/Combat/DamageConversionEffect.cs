using System;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Stats;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public enum DamageConversionSource
    {
        SingleType,
        Physical
    }

    [Serializable]
    public class DamageConversionEffect
    {
        public DamageConversionSource Source = DamageConversionSource.Physical;
        public DamageType FromType = DamageType.Slashing;
        public DamageType ToType = DamageType.Lightning;
        public TimedBuffCategory Category = TimedBuffCategory.Weapon;
        [Range(0f, 1f)] public float SourceFraction = 0.3f;
        [Min(0f)] public float ConvertedDamageFraction = 0.5f;
        [Min(0f)] public float DurationSeconds = 120f;

        public bool Matches(DamageType type)
        {
            return Source == DamageConversionSource.Physical
                ? IsPhysical(type)
                : type == FromType;
        }

        public bool IsValid()
        {
            return DurationSeconds > 0f
                && SourceFraction > 0f
                && ConvertedDamageFraction > 0f;
        }

        public static bool IsPhysical(DamageType type)
        {
            return type == DamageType.Piercing
                || type == DamageType.Blunt
                || type == DamageType.Slashing;
        }
    }
}
