using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Phylactery
{
    public class PhylacteryController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private PhylacteryConfig _config;
        [SerializeField] private Light _light;

        public float Charge => _stats.Get(StatType.PhylacteryCharge);
        public float MaxCharge => _stats.Get(StatType.MaxPhylacteryCharge);
        public float NormalizedCharge => MaxCharge <= 0f ? 0f : Mathf.Clamp01(Charge / MaxCharge);

        public bool TrySpend(float amount)
        {
            if (Charge < amount)
                return false;

            _stats.Add(StatType.PhylacteryCharge, -amount);
            UpdateLight();
            return true;
        }

        public void Restore(float amount)
        {
            _stats.Add(StatType.PhylacteryCharge, amount);
            UpdateLight();
        }

        private void Update()
        {
            if (_config.PassiveChargeDrainPerSecond > 0f)
                _stats.Add(StatType.PhylacteryCharge, -_config.PassiveChargeDrainPerSecond * Time.deltaTime);

            UpdateLight();
        }

        private void UpdateLight()
        {
            if (_light == null)
                return;

            _light.intensity = Mathf.Lerp(_config.LightMinIntensity, _config.LightMaxIntensity, NormalizedCharge);
            _light.range = Mathf.Lerp(_config.LightMinRange, _config.LightMaxRange, NormalizedCharge);
        }
    }
}
