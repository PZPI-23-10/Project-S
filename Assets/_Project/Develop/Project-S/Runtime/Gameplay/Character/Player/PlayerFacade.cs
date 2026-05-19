using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Movement;
using Project_S.Runtime.Gameplay.Character.Phylactery;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Player
{
    public class PlayerFacade : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour _inputSource;
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private CombatController _combat;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private HotbarUI _hotbar;
        [SerializeField] private PlayerActionGate _actionGate;
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

            if (_actionGate == null) _actionGate = GetComponent<PlayerActionGate>() ?? gameObject.AddComponent<PlayerActionGate>();
            if (_motor == null) _motor = GetComponent<CharacterMotor>();
            if (_combat == null) _combat = GetComponent<CombatController>();
            if (_interactor == null) _interactor = GetComponentInChildren<PlayerInteractor>();
            if (_interactor == null) _interactor = FindFirstObjectByType<PlayerInteractor>();
            if (_hotbar == null) _hotbar = FindFirstObjectByType<HotbarUI>();
        }

        private void Update()
        {
            if (_input == null)
                return;

            var snapshot = _actionGate != null ? _actionGate.Filter(_input.Snapshot) : _input.Snapshot;
            _motor?.Tick(snapshot);
            _combat?.Tick(snapshot);
            _interactor?.Tick(snapshot);
            _hotbar?.Tick(snapshot);
        }
    }
}
