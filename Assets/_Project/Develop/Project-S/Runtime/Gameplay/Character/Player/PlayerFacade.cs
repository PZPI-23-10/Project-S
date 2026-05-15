using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Movement;
using Project_S.Runtime.Gameplay.Character.Phylactery;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Player
{
    public class PlayerFacade : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSource;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CombatController _combat;
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private PhylacteryController _phylactery;

        private IPlayerInput _input;

        public CharacterStats Stats => _stats;
        public PhylacteryController Phylactery => _phylactery;

        private void Awake()
        {
            _input = _inputSource as IPlayerInput;

            if (_input == null)
                Debug.LogError($"{nameof(PlayerFacade)} requires an input source implementing {nameof(IPlayerInput)}.", this);
        }

        private void Update()
        {
            if (_input == null)
                return;

            var snapshot = _input.Snapshot;
            _motor.Tick(snapshot);
        }
    }
}
