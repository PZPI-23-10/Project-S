using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
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
        [SerializeField] private PoiseController _poise;
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private EquipmentSlots _equipmentSlots;
        [SerializeField] private HotbarUI _hotbar;
        [SerializeField] private PlayerActionGate _actionGate;
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private PhylacteryController _phylactery;
        [SerializeField] private PlayerDeathController _deathController;
        [SerializeField] private CameraTilt _cameraTilt;

        private IPlayerInput _input;

        public CharacterStats Stats => _stats;
        public PhylacteryController Phylactery => _phylactery;

        private void Awake()
        {
            _input = _inputSource as IPlayerInput;

            if (_input == null)
                Debug.LogError($"{nameof(PlayerFacade)} requires an input source implementing {nameof(IPlayerInput)}.", this);

            if (_actionGate == null) _actionGate = GetComponent<PlayerActionGate>() ?? gameObject.AddComponent<PlayerActionGate>();
            if (_deathController == null) _deathController = GetComponent<PlayerDeathController>() ?? gameObject.AddComponent<PlayerDeathController>();
            if (GetComponent<PlayerNavMeshObstacle>() == null) gameObject.AddComponent<PlayerNavMeshObstacle>();
            if (_motor == null) _motor = GetComponent<CharacterMotor>();
            if (_combat == null) _combat = GetComponent<CombatController>();
            if (_poise == null) _poise = GetComponent<PoiseController>();
            if (_interactor == null) _interactor = GetComponentInChildren<PlayerInteractor>();
            if (_interactor == null) _interactor = FindFirstObjectByType<PlayerInteractor>();
            if (_equipmentSlots == null) _equipmentSlots = GetComponentInChildren<EquipmentSlots>() ?? GetComponent<EquipmentSlots>();
            if (_hotbar == null) _hotbar = FindFirstObjectByType<HotbarUI>();
        }

        private void Update()
        {
            if (_input == null)
                return;

            var snapshot = _actionGate != null ? _actionGate.Filter(_input.Snapshot) : _input.Snapshot;
            _poise?.Tick(snapshot);
            _motor?.Tick(snapshot);
            _combat?.Tick(snapshot);
            _interactor?.Tick(snapshot);
            _equipmentSlots?.Tick(snapshot);
            _hotbar?.Tick(snapshot);

            bool isStaggered = _combat != null && _combat.CurrentState == CombatState.Staggered;
            if (_cameraTilt != null)
            {
                _cameraTilt.SetEnabled(!isStaggered);
                _cameraTilt.Tick(snapshot);
            }
        }
    }
}
