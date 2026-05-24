using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    [Serializable]
    public class StatModifier
    {
        public StatType Type;
        public float Additive;
        public float Multiplier = 1f;
    }

    public class CharacterStats : MonoBehaviour
    {
        [SerializeField] private List<Stat> _stats = new();

        private readonly Dictionary<StatType, Stat> _statsByType = new();
        private readonly Dictionary<object, List<StatModifier>> _modifiersBySource = new();

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

        public float GetRaw(StatType type)
        {
            return _statsByType.TryGetValue(type, out var stat) ? stat.CurrentValue : 0f;
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
                value = ApplyModifiers(type, stat.CurrentValue);
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
            Changed?.Invoke(type, ApplyModifiers(type, stat.CurrentValue));
        }

        public void Add(StatType type, float delta)
        {
            Set(type, GetRaw(type) + delta);
        }

        public void SetModifiers(object source, IEnumerable<StatModifier> modifiers)
        {
            if (source == null)
                return;

            var changedTypes = new HashSet<StatType>();
            if (_modifiersBySource.TryGetValue(source, out var existing))
            {
                foreach (var modifier in existing)
                {
                    if (modifier != null)
                        changedTypes.Add(modifier.Type);
                }
            }

            var next = modifiers?
                .Where(x => x != null)
                .Select(x => new StatModifier
                {
                    Type = x.Type,
                    Additive = x.Additive,
                    Multiplier = Mathf.Approximately(x.Multiplier, 0f) ? 1f : x.Multiplier
                })
                .ToList();

            if (next == null || next.Count == 0)
                _modifiersBySource.Remove(source);
            else
                _modifiersBySource[source] = next;

            if (next != null)
            {
                foreach (var modifier in next)
                    changedTypes.Add(modifier.Type);
            }

            NotifyChanged(changedTypes);
        }

        public void ClearModifiers(object source)
        {
            if (source == null || !_modifiersBySource.TryGetValue(source, out var existing))
                return;

            _modifiersBySource.Remove(source);

            var changedTypes = new HashSet<StatType>();
            foreach (var modifier in existing)
            {
                if (modifier != null)
                    changedTypes.Add(modifier.Type);
            }

            NotifyChanged(changedTypes);
        }

        private float ApplyModifiers(StatType type, float value)
        {
            float additive = 0f;
            float multiplier = 1f;

            foreach (var modifierList in _modifiersBySource.Values)
            {
                foreach (var modifier in modifierList)
                {
                    if (modifier == null || modifier.Type != type)
                        continue;

                    additive += modifier.Additive;
                    if (!Mathf.Approximately(modifier.Multiplier, 0f))
                        multiplier *= modifier.Multiplier;
                }
            }

            return (value + additive) * multiplier;
        }

        private void NotifyChanged(IEnumerable<StatType> statTypes)
        {
            foreach (var type in statTypes)
                Changed?.Invoke(type, Get(type));
        }
    }
}
