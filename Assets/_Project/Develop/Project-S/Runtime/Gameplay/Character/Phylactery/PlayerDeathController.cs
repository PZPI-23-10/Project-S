using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Phylactery
{
    public class PlayerDeathController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private PhylacteryController _phylactery;
        [SerializeField] private PhylacteryConfig _config;
        [SerializeField] private PlayerActionGate _actionGate;
        [SerializeField] private HomeTeleportController _homeTeleport;
        [SerializeField] private Transform _fallbackRespawnPoint;

        private bool _isDead;
        private bool _handlingHealthChange;

        public bool IsDead => _isDead;

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();
            if (_phylactery == null) _phylactery = GetComponent<PhylacteryController>() ?? GetComponentInParent<PhylacteryController>();
            if (_actionGate == null) _actionGate = GetComponent<PlayerActionGate>() ?? GetComponentInParent<PlayerActionGate>();
            if (_homeTeleport == null) _homeTeleport = GetComponent<HomeTeleportController>() ?? GetComponentInParent<HomeTeleportController>();
            if (_config == null && _phylactery != null) _config = _phylactery.Config;
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (_stats != null)
                _stats.Changed += OnStatChanged;
        }

        private void OnDisable()
        {
            if (_stats != null)
                _stats.Changed -= OnStatChanged;

            if (_actionGate != null)
                _actionGate.SetDeathBlocked(false);
        }

        public bool ForceRespawnAtHome()
        {
            Revive(false);
            return true;
        }

        private void OnStatChanged(StatType type, float value)
        {
            if (_handlingHealthChange || type != StatType.Health || value > 0f)
                return;

            HandleFatalHealth();
        }

        private void HandleFatalHealth()
        {
            if (_isDead)
                return;

            _isDead = true;
            if (_actionGate != null)
                _actionGate.SetDeathBlocked(true);

            if (CanSpendReviveCharge())
                Revive(true);
        }

        private bool CanSpendReviveCharge()
        {
            float cost = GetReviveChargeCost();
            return _phylactery != null && (cost <= 0f || _phylactery.Charge >= cost);
        }

        private void Revive(bool spendCharge)
        {
            _handlingHealthChange = true;

            if (spendCharge && _phylactery != null)
                _phylactery.TrySpend(GetReviveChargeCost());

            RestoreStatFraction(StatType.Health, StatType.MaxHealth, GetReviveHealthFraction());
            RestoreStatFraction(StatType.Stamina, StatType.MaxStamina, GetReviveStaminaFraction());

            if (ShouldRespawnAtHome())
                RespawnAtHome();

            _isDead = false;
            if (_actionGate != null)
                _actionGate.SetDeathBlocked(false);

            _handlingHealthChange = false;
        }

        private void RestoreStatFraction(StatType statType, StatType maxStatType, float fraction)
        {
            if (_stats == null)
                return;

            float max = _stats.Get(maxStatType);
            if (max <= 0f)
                max = _stats.GetMax(statType);

            _stats.Set(statType, Mathf.Max(1f, max * Mathf.Clamp01(fraction)));
        }

        private void RespawnAtHome()
        {
            if (_homeTeleport != null)
            {
                _homeTeleport.StartTeleport(0f);
                return;
            }

            if (_fallbackRespawnPoint != null)
                transform.position = _fallbackRespawnPoint.position;
        }

        private float GetReviveChargeCost()
        {
            return _config != null ? Mathf.Max(0f, _config.ReviveChargeCost) : 25f;
        }

        private float GetReviveHealthFraction()
        {
            return _config != null ? Mathf.Clamp01(_config.ReviveHealthFraction) : 0.5f;
        }

        private float GetReviveStaminaFraction()
        {
            return _config != null ? Mathf.Clamp01(_config.ReviveStaminaFraction) : 0.5f;
        }

        private bool ShouldRespawnAtHome()
        {
            return _config == null || _config.RespawnAtHome;
        }
    }
}
