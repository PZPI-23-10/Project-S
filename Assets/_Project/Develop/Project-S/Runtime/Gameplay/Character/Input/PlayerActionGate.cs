using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Input
{
    public class PlayerActionGate : MonoBehaviour
    {
        private bool _inventoryOpen;
        private bool _deathBlocked;

        public bool IsGameplayBlocked => _inventoryOpen || _deathBlocked;

        public void SetInventoryOpen(bool open)
        {
            _inventoryOpen = open;
        }

        public void SetDeathBlocked(bool blocked)
        {
            _deathBlocked = blocked;
        }

        public PlayerInputSnapshot Filter(PlayerInputSnapshot input)
        {
            return IsGameplayBlocked ? PlayerInputSnapshot.Blocked : input;
        }
    }
}
