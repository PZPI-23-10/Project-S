using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Input
{
    public readonly struct PlayerInputSnapshot
    {
        public PlayerInputSnapshot(
            Vector2 move,
            Vector2 look,
            bool sprintHeld,
            bool jumpPressed,
            bool dodgePressed,
            bool blockHeld,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool interactPressed)
        {
            Move = move;
            Look = look;
            SprintHeld = sprintHeld;
            JumpPressed = jumpPressed;
            DodgePressed = dodgePressed;
            BlockHeld = blockHeld;
            LightAttackPressed = lightAttackPressed;
            HeavyAttackPressed = heavyAttackPressed;
            InteractPressed = interactPressed;
        }

        public Vector2 Move { get; }
        public Vector2 Look { get; }
        public bool SprintHeld { get; }
        public bool JumpPressed { get; }
        public bool DodgePressed { get; }
        public bool BlockHeld { get; }
        public bool LightAttackPressed { get; }
        public bool HeavyAttackPressed { get; }
        public bool InteractPressed { get; }
    }
}
