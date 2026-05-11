using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Movement
{
    [CreateAssetMenu(menuName = "Project-S/Gameplay/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [field: SerializeField] public float Acceleration { get; private set; } = 18f;
        [field: SerializeField] public float Gravity { get; private set; } = -24f;
        [field: SerializeField] public float JumpHeight { get; private set; } = 1.1f;
        [field: SerializeField] public float MouseSensitivity { get; private set; } = 2f;
        [field: SerializeField] public float MinPitch { get; private set; } = -80f;
        [field: SerializeField] public float MaxPitch { get; private set; } = 80f;
        [field: SerializeField] public float DodgeSpeed { get; private set; } = 11f;
        [field: SerializeField] public float DodgeDuration { get; private set; } = 0.22f;
        [field: SerializeField] public float DodgeCooldown { get; private set; } = 0.55f;
        [field: SerializeField] public float DodgeStaminaCost { get; private set; } = 25f;
        [field: SerializeField] public float SprintStaminaCostPerSecond { get; private set; } = 8f;
        [field: SerializeField] public float AirAcceleration { get; private set; } = 8f;
        [field: SerializeField] public float AirDrag { get; private set; } = 0.1f;
    }
}
