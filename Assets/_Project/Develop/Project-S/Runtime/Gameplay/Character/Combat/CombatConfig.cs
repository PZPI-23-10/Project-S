using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [CreateAssetMenu(menuName = "Project-S/Gameplay/Combat Config")]
    public class CombatConfig : ScriptableObject
    {
        [field: SerializeField] public float LightAttackStaminaCost { get; private set; } = 12f;
        [field: SerializeField] public float HeavyAttackStaminaCost { get; private set; } = 28f;
        [field: SerializeField] public float LightAttackCooldown { get; private set; } = 0.35f;
        [field: SerializeField] public float HeavyAttackCooldown { get; private set; } = 0.75f;
        [field: SerializeField] public float BlockDamageMultiplier { get; private set; } = 0.35f;
        [field: SerializeField] public float ParryWindow { get; private set; } = 0.2f;
        [field: SerializeField] public float PoiseRecoveryPerSecond { get; private set; } = 8f;
    }
}
