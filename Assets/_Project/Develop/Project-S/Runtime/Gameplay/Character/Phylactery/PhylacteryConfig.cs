using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Phylactery
{
    [CreateAssetMenu(menuName = "Project-S/Gameplay/Phylactery Config")]
    public class PhylacteryConfig : ScriptableObject
    {
        [field: SerializeField] public float PassiveChargeDrainPerSecond { get; private set; }
        [field: SerializeField] public float LightMinIntensity { get; private set; } = 0.25f;
        [field: SerializeField] public float LightMaxIntensity { get; private set; } = 2.2f;
        [field: SerializeField] public float LightMinRange { get; private set; } = 2f;
        [field: SerializeField] public float LightMaxRange { get; private set; } = 8f;
    }
}
