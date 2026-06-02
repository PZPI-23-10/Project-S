using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public enum TimedBuffType
    {
        None,
        AttackDamage,
        SoulAshReward,
        AttackSpeed,
        StaminaCost,
        MaxHealth
    }

    public enum TimedBuffCategory
    {
        None,
        Food,
        Healing,
        Potion,
        Weapon,
        Debuff
    }

    public class BuffController : MonoBehaviour
    {
        private class ActiveTimedBuff
        {
            public TimedBuffType Type;
            public TimedBuffCategory Category;
            public float Multiplier;
            public float EndsAt;
        }

        private class ActiveDamageConversion
        {
            public DamageConversionSource Source;
            public DamageType FromType;
            public DamageType ToType;
            public TimedBuffCategory Category;
            public float SourceFraction;
            public float ConvertedDamageFraction;
            public float EndsAt;
        }

        private readonly List<ActiveTimedBuff> _activeBuffs = new List<ActiveTimedBuff>();
        private readonly List<ActiveDamageConversion> _activeDamageConversions = new List<ActiveDamageConversion>();
        private float _manualTime;

        public event Action Changed;

        public float AttackDamageMultiplier => GetMultiplier(TimedBuffType.AttackDamage);
        public float SoulAshRewardMultiplier => GetMultiplier(TimedBuffType.SoulAshReward);
        public float AttackSpeedMultiplier => GetMultiplier(TimedBuffType.AttackSpeed);
        public float StaminaCostMultiplier => GetMultiplier(TimedBuffType.StaminaCost);
        public float MaxHealthMultiplier => GetMultiplier(TimedBuffType.MaxHealth);

        public void ApplyBuff(TimedBuffType type, TimedBuffCategory category, float multiplier, float durationSeconds)
        {
            if (type == TimedBuffType.None || durationSeconds <= 0f || multiplier < 0f)
                return;

            RemoveExpired();
            EnforceCategoryLimitBeforeAdd(category);

            _activeBuffs.Add(new ActiveTimedBuff
            {
                Type = type,
                Category = category,
                Multiplier = multiplier,
                EndsAt = Now + durationSeconds
            });

            Changed?.Invoke();
        }

        public void ApplyDamageConversion(DamageConversionEffect effect)
        {
            if (effect == null || !effect.IsValid())
                return;

            RemoveExpired();
            EnforceCategoryLimitBeforeAdd(effect.Category);

            _activeDamageConversions.Add(new ActiveDamageConversion
            {
                Source = effect.Source,
                FromType = effect.FromType,
                ToType = effect.ToType,
                Category = effect.Category,
                SourceFraction = Mathf.Clamp01(effect.SourceFraction),
                ConvertedDamageFraction = Mathf.Max(0f, effect.ConvertedDamageFraction),
                EndsAt = Now + effect.DurationSeconds
            });

            Changed?.Invoke();
        }

        public List<DamageInstance> ModifyDamageProfile(IReadOnlyList<DamageInstance> baseProfile)
        {
            RemoveExpired();

            var modified = CopyPositiveDamage(baseProfile);
            if (modified.Count == 0)
                return modified;

            float attackMultiplier = AttackDamageMultiplier;
            if (!Mathf.Approximately(attackMultiplier, 1f))
            {
                for (int i = 0; i < modified.Count; i++)
                {
                    var damage = modified[i];
                    damage.Amount *= attackMultiplier;
                    modified[i] = damage;
                }
            }

            for (int i = 0; i < _activeDamageConversions.Count; i++)
                ApplyConversion(modified, _activeDamageConversions[i]);

            MergeDamageByType(modified);
            return modified;
        }

        public bool HasActiveBuff(TimedBuffType type)
        {
            RemoveExpired();
            return _activeBuffs.Exists(x => x.Type == type);
        }

        public int GetActiveCount(TimedBuffCategory category)
        {
            RemoveExpired();
            return CountCategory(category);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _manualTime += deltaTime;
            RemoveExpired();
        }

        public void ClearWeaponBuffs()
        {
            int removed = _activeBuffs.RemoveAll(x => x.Category == TimedBuffCategory.Weapon);
            removed += _activeDamageConversions.RemoveAll(x => x.Category == TimedBuffCategory.Weapon);

            if (removed > 0)
            {
                Changed?.Invoke();
                Debug.Log("<color=orange>[BuffController]</color> Всі бафи зброї очищено!");
            }
        }

        private void Update()
        {
            RemoveExpired();
        }

        private float GetMultiplier(TimedBuffType type)
        {
            RemoveExpired();

            float multiplier = 1f;
            foreach (var buff in _activeBuffs)
            {
                if (buff.Type == type)
                    multiplier *= buff.Multiplier;
            }

            return multiplier;
        }

        private void EnforceCategoryLimitBeforeAdd(TimedBuffCategory category)
        {
            int limit = GetCategoryLimit(category);
            if (limit <= 0)
                return;

            while (CountCategory(category) >= limit)
            {
                if (!RemoveFirstCategory(category))
                    return;
            }
        }

        private static int GetCategoryLimit(TimedBuffCategory category)
        {
            return category switch
            {
                TimedBuffCategory.Food => 1,
                TimedBuffCategory.Healing => 1,
                TimedBuffCategory.Potion => 2,
                TimedBuffCategory.Weapon => 1,
                _ => 0
            };
        }

        private void RemoveExpired()
        {
            float now = Now;
            int removed = _activeBuffs.RemoveAll(x => x.EndsAt <= now);
            removed += _activeDamageConversions.RemoveAll(x => x.EndsAt <= now);
            if (removed > 0)
                Changed?.Invoke();
        }

        private int CountCategory(TimedBuffCategory category)
        {
            if (category == TimedBuffCategory.None)
                return 0;

            int count = 0;
            for (int i = 0; i < _activeBuffs.Count; i++)
            {
                if (_activeBuffs[i].Category == category)
                    count++;
            }

            for (int i = 0; i < _activeDamageConversions.Count; i++)
            {
                if (_activeDamageConversions[i].Category == category)
                    count++;
            }

            return count;
        }

        private bool RemoveFirstCategory(TimedBuffCategory category)
        {
            int buffIndex = _activeBuffs.FindIndex(x => x.Category == category);
            if (buffIndex >= 0)
            {
                _activeBuffs.RemoveAt(buffIndex);
                return true;
            }

            int conversionIndex = _activeDamageConversions.FindIndex(x => x.Category == category);
            if (conversionIndex >= 0)
            {
                _activeDamageConversions.RemoveAt(conversionIndex);
                return true;
            }

            return false;
        }

        private static List<DamageInstance> CopyPositiveDamage(IReadOnlyList<DamageInstance> profile)
        {
            var result = new List<DamageInstance>();
            if (profile == null)
                return result;

            for (int i = 0; i < profile.Count; i++)
            {
                if (profile[i].Amount > 0f)
                    result.Add(profile[i]);
            }

            return result;
        }

        private static void ApplyConversion(List<DamageInstance> profile, ActiveDamageConversion conversion)
        {
            int sourceCount = profile.Count;
            for (int i = 0; i < sourceCount; i++)
            {
                var damage = profile[i];
                if (!ConversionMatches(conversion, damage.Type) || damage.Amount <= 0f)
                    continue;

                float originalAmount = damage.Amount;
                float convertedSourceAmount = originalAmount * conversion.SourceFraction;
                damage.Amount = Mathf.Max(0f, originalAmount - convertedSourceAmount);
                profile[i] = damage;

                float addedDamage = originalAmount * conversion.ConvertedDamageFraction;
                if (addedDamage > 0f)
                {
                    profile.Add(new DamageInstance
                    {
                        Type = conversion.ToType,
                        Amount = addedDamage
                    });
                }
            }
        }

        private static bool ConversionMatches(ActiveDamageConversion conversion, DamageType type)
        {
            return conversion.Source == DamageConversionSource.Physical
                ? DamageConversionEffect.IsPhysical(type)
                : type == conversion.FromType;
        }

        private static void MergeDamageByType(List<DamageInstance> profile)
        {
            for (int i = 0; i < profile.Count; i++)
            {
                var damage = profile[i];
                if (damage.Amount <= 0f)
                {
                    profile.RemoveAt(i);
                    i--;
                    continue;
                }

                for (int j = i + 1; j < profile.Count; j++)
                {
                    if (profile[j].Type != damage.Type)
                        continue;

                    damage.Amount += profile[j].Amount;
                    profile[i] = damage;
                    profile.RemoveAt(j);
                    j--;
                }
            }
        }

        private float Now => Application.isPlaying ? Time.time : _manualTime;
    }
}
