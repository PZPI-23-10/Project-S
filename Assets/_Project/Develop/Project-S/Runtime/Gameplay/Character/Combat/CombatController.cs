using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public enum CombatState
    {
        Idle,
        Attacking,
        HeavySkill,
        Ability,
        Blocking,
        Staggered
    }

    public enum AttackDirection
    {
        Center, Left, Right
    }

    public class CombatController : MonoBehaviour
    {
        [Header("Зв'язки")]
        [SerializeField] private StaminaController _stamina;
        [SerializeField] private BlockController _blockController;

        [Header("Візуал (Права рука)")]
        [SerializeField] private Transform _weaponHolder;
        private GameObject _currentWeaponModel;
        private MeleeHitTester _currentHitTester;
        private Animator _weaponAnimator;

        [Header("Візуал (Ліва рука)")]
        [SerializeField] private Transform _offhandHolder;
        [SerializeField] private GameObject _phylacteryPrefab;
        private GameObject _currentOffhandModel;

        private bool _isPhylacteryInHand = false;
        private bool _isCombatOffhandInHand = false;
        private bool _isOffhandActive => _isPhylacteryInHand || _isCombatOffhandInHand;

        [Header("Екіпірування")]
        [SerializeField] private WeaponItemData _unarmedWeapon;
        [SerializeField] private WeaponItemData _currentWeapon;
        [SerializeField] private WeaponItemData _equippedOffhandItem;

        [Header("Прогресія (Скіли)")]
        [SerializeField] private bool _isOffhandSkillUnlocked = true;

        public WeaponItemData ActiveWeapon => _currentWeapon != null ? _currentWeapon : _unarmedWeapon;
        public WeaponItemData CurrentWeapon => ActiveWeapon;
        public CombatState CurrentState { get; private set; } = CombatState.Idle;

        // ==========================================
        // НАША ІДЕАЛЬНА СИСТЕМА КОМБО БЕЗ ТАЙМЕРІВ
        // ==========================================

        private bool _isComboWindowOpen = false;
        private bool _nextAttackBuffered = false;
        private bool _isTransitioningToNextCombo = false;
        private int _comboStep = 0;
        public int ComboStep => _comboStep;

        private int _currentHeavyCharge = 0;
        private float _lastAbilityTime = 0f;

        public void Tick(PlayerInputSnapshot input)
        {
            if (CurrentState == CombatState.Staggered) return;
            if (ActiveWeapon == null) return;

            HandleCombatInput(input);
        }

        private void HandleCombatInput(PlayerInputSnapshot input)
        {
            if (input.ToggleOffhandPressed && CurrentState == CombatState.Idle) ToggleOffhand();

            if (input.HeavyAttackPressed && !_isOffhandActive)
            {
                if (_currentHeavyCharge >= ActiveWeapon.HitsToChargeHeavy) PerformHeavySkill();
                else Debug.Log($"<color=orange>[Боївка]</color> Вміння ще не заряджено! ({_currentHeavyCharge}/{ActiveWeapon.HitsToChargeHeavy})");
                return;
            }

            if (_currentWeaponModel != null)
            {
                HammerWeapon customWeapon = _currentWeaponModel.GetComponent<HammerWeapon>();

                if (customWeapon != null)
                {
                    bool inputHandled = customWeapon.ProcessCustomInput(input, _weaponAnimator, this);

                    if (inputHandled) return;
                }
            }

            if (input.BlockHeld && CurrentState == CombatState.Idle && !_isOffhandActive) StartBlocking();
            else if (!input.BlockHeld && CurrentState == CombatState.Blocking) StopBlocking();

            // Логіка буферизації ударів
            if (input.LightAttackHeld || input.LightAttackPressed)
            {
                if (CurrentState == CombatState.Idle)
                {
                    PerformLightAttack();
                }
                else if (CurrentState == CombatState.Attacking && _isComboWindowOpen)
                {
                    _nextAttackBuffered = true;
                }
            }

            if (CurrentState != CombatState.Idle) return;
            if (input.OffhandAbilityPressed && _isOffhandActive) PerformOffhandAbility();
        }

        public void PerformLightAttack()
        {
            CurrentState = CombatState.Attacking;
            _isComboWindowOpen = false;
            _nextAttackBuffered = false;

            _comboStep++;
            if (_comboStep > ActiveWeapon.MaxComboHits) _comboStep = 1;

            Debug.Log($"<color=cyan>[Боївка]</color> Удар: {ActiveWeapon.name} | Крок: {_comboStep}");

            CancelInvoke(nameof(FailsafeReset));
            Invoke(nameof(FailsafeReset), 4.5f);

            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetInteger("ComboStep", _comboStep);
                _weaponAnimator.SetTrigger("Attack");
            }
        }

        private void ToggleOffhand()
        {
            if (ActiveWeapon.IsTwoHanded) return;
            if (_currentOffhandModel != null) Destroy(_currentOffhandModel);

            if (!_isCombatOffhandInHand && _isOffhandSkillUnlocked && _equippedOffhandItem != null)
            {
                _isCombatOffhandInHand = true; _isPhylacteryInHand = false;
                _currentOffhandModel = Instantiate(_equippedOffhandItem.WeaponPrefab, _offhandHolder);
                if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", true);
            }
            else if (!_isPhylacteryInHand)
            {
                _isPhylacteryInHand = true; _isCombatOffhandInHand = false;
                if (_phylacteryPrefab != null) _currentOffhandModel = Instantiate(_phylacteryPrefab, _offhandHolder);
                if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", true);
            }
            else
            {
                _isPhylacteryInHand = false; _isCombatOffhandInHand = false;
                if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", false);
            }

            if (_currentOffhandModel != null)
            {
                _currentOffhandModel.transform.localPosition = Vector3.zero;
                _currentOffhandModel.transform.localRotation = Quaternion.identity;
            }
        }

        public void EquipOffhand(WeaponItemData newOffhandItem)
        {
            _equippedOffhandItem = newOffhandItem;
            if (_isOffhandActive)
            {
                ToggleOffhand(); if (newOffhandItem != null) ToggleOffhand();
            }
        }

        public void EquipWeapon(WeaponItemData newWeapon)
        {
            if (_currentWeaponModel != null) Destroy(_currentWeaponModel);

            WeaponItemData weaponToEquip = newWeapon != null ? newWeapon : _unarmedWeapon;
            _currentWeapon = newWeapon; _comboStep = 0;

            if (weaponToEquip != null && weaponToEquip.IsTwoHanded && _isOffhandActive) ToggleOffhand();

            if (weaponToEquip != null && weaponToEquip.WeaponPrefab != null)
            {
                _currentWeaponModel = Instantiate(weaponToEquip.WeaponPrefab, _weaponHolder);
                if (_currentWeaponModel != null) _weaponAnimator = _currentWeaponModel.GetComponent<Animator>();

                _currentWeaponModel.transform.localPosition = Vector3.zero;
                _currentWeaponModel.transform.localRotation = Quaternion.identity;

                _currentHitTester = _currentWeaponModel.GetComponentInChildren<MeleeHitTester>();
                if (_currentHitTester != null) _currentHitTester.Setup(weaponToEquip, gameObject);
            }
        }

        // ==========================================
        //  ПОВЕРНУТІ МЕТОДИ (БЛОК, УРОН, МНОЖНИК)
        // ==========================================
        public void AddChargeOnHit()
        {
            if (_currentHeavyCharge < ActiveWeapon.HitsToChargeHeavy)
            {
                _currentHeavyCharge++;
                Debug.Log($"<color=yellow>[Боївка]</color> Влучання! Заряд: {_currentHeavyCharge} / {ActiveWeapon.HitsToChargeHeavy}");
            }
        }

        public float GetAttackSpeedMultiplier()
        {
            if (ActiveWeapon == null) return 1f;
            float multiplier = ActiveWeapon.AttackSpeedMultiplier;
            var buffs = GetComponentInParent<BuffController>();
            if (buffs != null) multiplier *= buffs.AttackSpeedMultiplier;
            return Mathf.Max(0.01f, multiplier);
        }

        private void PerformHeavySkill()
        {
            CurrentState = CombatState.HeavySkill;
            _currentHeavyCharge = 0;

            Debug.Log($"<color=red>[Боївка]</color> ВМІННЯ ПРАВОЇ РУКИ: {ActiveWeapon.HeavyAbility}!");

            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetInteger("HeavyType", (int)ActiveWeapon.HeavyAbility);
                _weaponAnimator.SetTrigger("HeavySkill");
            }
        }

        private void PerformOffhandAbility()
        {
            CurrentState = CombatState.Ability;
            _lastAbilityTime = Time.time;

            if (_equippedOffhandItem != null)
            {
                Debug.Log($"<color=yellow>[Магія]</color> ЗДІБНІСТЬ ЛІВОЇ РУКИ: {_equippedOffhandItem.OffhandAbility}!");

                if (_weaponAnimator != null)
                {
                    _weaponAnimator.SetInteger("OffhandType", (int)_equippedOffhandItem.OffhandAbility);
                    _weaponAnimator.SetTrigger("OffhandSkill");
                }
            }
        }

        private void StartBlocking()
        {
            CurrentState = CombatState.Blocking;
            if (_blockController != null) _blockController.StartBlock();
            if (_weaponAnimator != null) _weaponAnimator.SetBool("IsBlocking", true);
        }

        private void StopBlocking()
        {
            CurrentState = CombatState.Idle;
            if (_blockController != null) _blockController.StopBlock();
            if (_weaponAnimator != null) _weaponAnimator.SetBool("IsBlocking", false);
        }

        private void ResetToIdle()
        {
            CurrentState = CombatState.Idle;
        }

        private void FailsafeReset()
        {
            if (CurrentState == CombatState.Attacking)
            {
                Debug.LogWarning("<color=red>[Запобіжник]</color> Анімація зависла!");
                _isTransitioningToNextCombo = false;

                ForceResetToIdle();
            }
        }

        public bool DrainStamina(float amount)
        {
            if (_stamina == null) return true;
            return _stamina.Spend(amount);
        }

        // ==========================================
        // ІВЕНТИ АНІМАЦІЙ
        // ==========================================
        public void AnimEvent_StartHitbox() { if (_currentHitTester != null) _currentHitTester.StartHitDetection(); }
        public void AnimEvent_StopHitbox() { if (_currentHitTester != null) _currentHitTester.StopHitDetection(); }

        public void AnimEvent_OpenComboWindow()
        {
            _isComboWindowOpen = true;
        }

        public void AnimEvent_TriggerNextCombo()
        {
            if (_nextAttackBuffered)
            {
                _isTransitioningToNextCombo = true;
                PerformLightAttack();
            }
        }

        public void AnimEvent_ExecuteHeavyAbility()
        {
            if (_currentWeaponModel != null)
            {
                IWeaponActiveAbility activeSkill = _currentWeaponModel.GetComponentInChildren<IWeaponActiveAbility>();
                if (activeSkill != null)
                {
                    activeSkill.ExecuteHeavyAbility(this, _weaponAnimator);
                }
            }
        }

        // Цей івент ти будеш ставити в Аніматорі на кадр застосування лівої руки (щита/магії)
        public void AnimEvent_ExecuteOffhandAbility()
        {
            if (_currentOffhandModel != null)
            {
                IOffhandAbility offhandSkill = _currentOffhandModel.GetComponentInChildren<IOffhandAbility>();
                if (offhandSkill != null)
                {
                    offhandSkill.ExecuteOffhandAbility(this, _weaponAnimator);
                }
            }
        }

        public void ForceResetToIdle()
        {
            CancelInvoke(nameof(FailsafeReset));
            _isComboWindowOpen = false;
            _nextAttackBuffered = false;
            _isTransitioningToNextCombo = false;
            _comboStep = 0;
            CurrentState = CombatState.Idle;

            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetInteger("ComboStep", 0);
            }

            Debug.Log("<color=lime>[Аніматор]</color> Стан Idle! Комбо успішно скинуто.");
        }
    }
}