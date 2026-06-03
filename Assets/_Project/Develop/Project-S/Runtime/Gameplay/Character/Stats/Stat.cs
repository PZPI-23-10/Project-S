using System;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    [Serializable]
    public class Stat
    {
        [SerializeField] private StatType _type;
        [SerializeField] private float _baseValue;
        [SerializeField] private float _minValue;
        [SerializeField] private float _maxValue = 100f;

        private float _currentValue;

        public StatType Type => _type;
        public float BaseValue => _baseValue;
        public float CurrentValue => _currentValue;
        public float MinValue => _minValue;
        public float MaxValue => _maxValue;
        public float NormalizedValue => GetNormalizedValue(_maxValue);

        public void Initialize()
        {
            _currentValue = Mathf.Clamp(_baseValue, _minValue, _maxValue);
        }

        public void Set(float value)
        {
            Set(value, _maxValue);
        }

        public void Set(float value, float maxValue)
        {
            float effectiveMax = Mathf.Max(_minValue, maxValue);
            _currentValue = Mathf.Clamp(value, _minValue, effectiveMax);
        }

        public void Add(float delta)
        {
            Set(_currentValue + delta);
        }

        public void Add(float delta, float maxValue)
        {
            Set(_currentValue + delta, maxValue);
        }

        public void AddMaxValue(float delta)
        {
            SetMaxValue(_maxValue + delta);
        }

        public void SetMaxValue(float maxValue)
        {
            _maxValue = Mathf.Max(_minValue, maxValue);
            Set(_currentValue);
        }

        public float GetNormalizedValue(float maxValue)
        {
            float effectiveMax = Mathf.Max(_minValue, maxValue);
            return Mathf.Approximately(effectiveMax, _minValue)
                ? 0f
                : Mathf.InverseLerp(_minValue, effectiveMax, _currentValue);
        }
    }
}
