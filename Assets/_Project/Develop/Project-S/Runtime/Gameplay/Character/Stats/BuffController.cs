using System;
using System.Collections.Generic;
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

        private readonly List<ActiveTimedBuff> _activeBuffs = new List<ActiveTimedBuff>();
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

        public bool HasActiveBuff(TimedBuffType type)
        {
            RemoveExpired();
            return _activeBuffs.Exists(x => x.Type == type);
        }

        public int GetActiveCount(TimedBuffCategory category)
        {
            RemoveExpired();
            return _activeBuffs.FindAll(x => x.Category == category).Count;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _manualTime += deltaTime;
            RemoveExpired();
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

            while (_activeBuffs.FindAll(x => x.Category == category).Count >= limit)
            {
                int removeIndex = _activeBuffs.FindIndex(x => x.Category == category);
                if (removeIndex < 0)
                    return;

                _activeBuffs.RemoveAt(removeIndex);
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
            if (removed > 0)
                Changed?.Invoke();
        }

        private float Now => Application.isPlaying ? Time.time : _manualTime;
    }
}
