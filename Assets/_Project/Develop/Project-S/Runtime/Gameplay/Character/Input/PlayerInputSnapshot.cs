using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Input
{
    public readonly struct PlayerInputSnapshot
    {
        // 18 параметрів: все на false або zero
        public static PlayerInputSnapshot Blocked => new PlayerInputSnapshot(
            Vector2.zero, Vector2.zero, false, false, false, false, false, false, false, false, false, -1, false, false, false, false, false, false);

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
                -1,
                false,
                false, 
                false, 
                false,
                false, 
                false) // 18 параметрів
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
                toggleOffhandPressed,
                offhandAbilityPressed,
                hotbarSlotPressed,
                false, 
                false, 
                false,
                false, 
                false, 
                false) // 18 параметрів
        {
        }

        // ГОЛОВНИЙ КОНСТРУКТОР (18 параметрів)
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
            int hotbarSlotPressed,
            bool qteForwardPressed,
            bool qteBackPressed,
            bool qteLeftPressed,
            bool qteRightPressed,
            bool lightAttackHeld = false, // Наш інпут для Молота
            bool crouchHeld = false)      // Інпут твого друга для присідання
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
            QteForwardPressed = qteForwardPressed;
            QteBackPressed = qteBackPressed;
            QteLeftPressed = qteLeftPressed;
            QteRightPressed = qteRightPressed;
            LightAttackHeld = lightAttackHeld;
            CrouchHeld = crouchHeld;
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
        public bool QteForwardPressed { get; }
        public bool QteBackPressed { get; }
        public bool QteLeftPressed { get; }
        public bool QteRightPressed { get; }
        public bool LightAttackHeld { get; }
        public bool CrouchHeld { get; }
    }
}