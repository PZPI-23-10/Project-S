using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class CharacterStats : MonoBehaviour
    {
        [SerializeField] private List<Stat> _stats = new();

        private readonly Dictionary<StatType, Stat> _statsByType = new();

        public event Action<StatType, float> Changed;

        private void Awake()
        {
            _statsByType.Clear();

            foreach (var stat in _stats)
            {
                stat.Initialize();
                _statsByType[stat.Type] = stat;
            }
        }

        public float Get(StatType type)
        {
            return TryGet(type, out var value) ? value : 0f;
        }

        public bool TryGetStat(StatType type, out Stat stat)
        {
            return _statsByType.TryGetValue(type, out stat);
        }

        public float GetNormalized(StatType type)
        {
            return TryGetStat(type, out var stat) ? stat.NormalizedValue : 0f;
        }

        public float GetMin(StatType type)
        {
            return TryGetStat(type, out var stat) ? stat.MinValue : 0f;
        }

        public float GetMax(StatType type)
        {
            return TryGetStat(type, out var stat) ? stat.MaxValue : 0f;
        }

        public bool TryGet(StatType type, out float value)
        {
            if (_statsByType.TryGetValue(type, out var stat))
            {
                value = stat.CurrentValue;
                return true;
            }

            value = 0f;
            return false;
        }

        public void Set(StatType type, float value)
        {
            if (!_statsByType.TryGetValue(type, out var stat))
            {
                Debug.LogWarning($"Stat {type} is not configured on {name}.", this);
                return;
            }

            stat.Set(value);
            Changed?.Invoke(type, stat.CurrentValue);
        }

        public void Add(StatType type, float delta)
        {
            Set(type, Get(type) + delta);
        }
    }
}
