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
        public float NormalizedValue => Mathf.Approximately(_maxValue, _minValue)
            ? 0f
            : Mathf.InverseLerp(_minValue, _maxValue, _currentValue);

        public void Initialize()
        {
            _currentValue = Mathf.Clamp(_baseValue, _minValue, _maxValue);
        }

        public void Set(float value)
        {
            _currentValue = Mathf.Clamp(value, _minValue, _maxValue);
        }

        public void Add(float delta)
        {
            Set(_currentValue + delta);
        }
    }
}
