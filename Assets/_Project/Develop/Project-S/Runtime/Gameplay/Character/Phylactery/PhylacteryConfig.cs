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
        [field: Header("Death / Revive")]
        [field: SerializeField] public float ReviveChargeCost { get; private set; } = 25f;
        [field: Range(0f, 1f)]
        [field: SerializeField] public float ReviveHealthFraction { get; private set; } = 0.5f;
        [field: Range(0f, 1f)]
        [field: SerializeField] public float ReviveStaminaFraction { get; private set; } = 0.5f;
        [field: SerializeField] public bool RespawnAtHome { get; private set; } = true;
    }
}
