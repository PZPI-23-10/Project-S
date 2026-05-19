using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Phylactery
{
    public class PhylacteryController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private PhylacteryConfig _config;
        [SerializeField] private Light _light;

        public float Charge => ResolveStats() != null ? _stats.Get(StatType.PhylacteryCharge) : 0f;
        public float MaxCharge => ResolveStats() != null ? _stats.Get(StatType.MaxPhylacteryCharge) : 0f;
        public float NormalizedCharge => MaxCharge <= 0f ? 0f : Mathf.Clamp01(Charge / MaxCharge);
        public PhylacteryConfig Config => _config;

        public bool TrySpend(float amount)
        {
            if (ResolveStats() == null)
                return false;

            if (Charge < amount)
                return false;

            _stats.Add(StatType.PhylacteryCharge, -amount);
            UpdateLight();
            return true;
        }

        public void Restore(float amount)
        {
            if (ResolveStats() == null)
                return;

            _stats.Add(StatType.PhylacteryCharge, amount);
            UpdateLight();
        }

        private void Update()
        {
            if (_config != null && _config.PassiveChargeDrainPerSecond > 0f && ResolveStats() != null)
                _stats.Add(StatType.PhylacteryCharge, -_config.PassiveChargeDrainPerSecond * Time.deltaTime);

            UpdateLight();
        }

        private CharacterStats ResolveStats()
        {
            if (_stats == null)
                _stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();

            return _stats;
        }

        private void UpdateLight()
        {
            if (_light == null || _config == null)
                return;

            _light.intensity = Mathf.Lerp(_config.LightMinIntensity, _config.LightMaxIntensity, NormalizedCharge);
            _light.range = Mathf.Lerp(_config.LightMinRange, _config.LightMaxRange, NormalizedCharge);
        }
    }
}
