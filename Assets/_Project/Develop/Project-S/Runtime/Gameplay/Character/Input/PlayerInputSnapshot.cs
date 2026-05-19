using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Input
{
    public readonly struct PlayerInputSnapshot
    {
        public static PlayerInputSnapshot Blocked => new PlayerInputSnapshot(
            Vector2.zero,
            Vector2.zero,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            false,
            -1);

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
            : this(
                move,
                look,
                sprintHeld,
                jumpPressed,
                dodgePressed,
                blockHeld,
                lightAttackPressed,
                heavyAttackPressed,
                interactPressed,
                false,
                false,
                -1)
        {
        }

        public PlayerInputSnapshot(
            Vector2 move,
            Vector2 look,
            bool sprintHeld,
            bool jumpPressed,
            bool dodgePressed,
            bool blockHeld,
            bool lightAttackPressed,
            bool heavyAttackPressed,
            bool interactPressed,
            bool toggleOffhandPressed,
            bool offhandAbilityPressed,
            int hotbarSlotPressed)
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
            ToggleOffhandPressed = toggleOffhandPressed;
            OffhandAbilityPressed = offhandAbilityPressed;
            HotbarSlotPressed = hotbarSlotPressed;
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
        public bool ToggleOffhandPressed { get; }
        public bool OffhandAbilityPressed { get; }
        public int HotbarSlotPressed { get; }
    }
}
