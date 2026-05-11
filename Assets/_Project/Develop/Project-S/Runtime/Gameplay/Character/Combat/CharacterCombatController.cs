using Project_S.Runtime.Gameplay.Character.Input;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class CharacterCombatController : MonoBehaviour
    {
        [SerializeField] private AttackController _attack;
        [SerializeField] private BlockController _block;

        public void Tick(PlayerInputSnapshot input)
        {
            _block.Tick(input);
            _attack.Tick(input);
        }
    }
}
