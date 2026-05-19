using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Input
{
    public class PlayerActionGate : MonoBehaviour
    {
        private int _uiBlockCount;

        public bool IsGameplayBlocked => _uiBlockCount > 0;

        public void SetInventoryOpen(bool open)
        {
            _uiBlockCount = open ? 1 : 0;
        }

        public PlayerInputSnapshot Filter(PlayerInputSnapshot input)
        {
            return IsGameplayBlocked ? PlayerInputSnapshot.Blocked : input;
        }
    }
}
