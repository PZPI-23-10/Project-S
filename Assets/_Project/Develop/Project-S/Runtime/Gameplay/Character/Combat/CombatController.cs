using UnityEngine;
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
        Center,
        Left,
        Right
    }

    public class CombatController : MonoBehaviour
    {
        [Header("Зв'язки")]
        [SerializeField] private StaminaController _stamina;
        [SerializeField] private BlockController _blockController;

        [Header("Візуал")]
        [SerializeField] private Transform _weaponHolder; // Пустишка в руці гравця (куди спавнити)
        private GameObject _currentWeaponModel; // Збережене посилання на 3D-модель (щоб видалити стару)

        private MeleeHitTester _currentHitTester;
        private Animator _weaponAnimator;

        [Header("Екіпірування")]
        [SerializeField] private WeaponItemData _unarmedWeapon; // Твої Кулаки (перетягни файл сюди)
        [SerializeField] private WeaponItemData _currentWeapon; // Зброя з інвентарю

        [Header("Налаштування Комбо")]
        [SerializeField] private float _comboResetTime = 1.5f;

        // ВЛАСТИВІСТЬ: Повертає зброю з рук, або кулаки, якщо рук порожні
        public WeaponItemData ActiveWeapon => _currentWeapon != null ? _currentWeapon : _unarmedWeapon;

        // Для Блок-контролера та інших скриптів
        public WeaponItemData CurrentWeapon => ActiveWeapon;

        public CombatState CurrentState { get; private set; } = CombatState.Idle;

        private int _comboStep = 0;
        private int _currentHeavyCharge = 0;
        private float _lastAttackTime = 0f;
        private float _lastAbilityTime = 0f;

        private void Update()
        {
            if (CurrentState == CombatState.Staggered) return;

            // Якщо немає навіть кулаків у слоті Unarmed - нічого не робимо
            if (ActiveWeapon == null) return;

            HandleCombatInput();
            CheckComboReset();
        }

        private void HandleCombatInput()
        {
            // 1. БЛОК (ПКМ)
            if (UnityEngine.Input.GetMouseButtonDown(1) && CurrentState == CombatState.Idle)
            {
                StartBlocking();
            }
            else if (UnityEngine.Input.GetMouseButtonUp(1) && CurrentState == CombatState.Blocking)
            {
                StopBlocking();
            }

            if (CurrentState != CombatState.Idle) return;

            // 2. ВАЖКИЙ УДАР / УЛЬТА (ЛКМ + ПКМ)
            if (UnityEngine.Input.GetMouseButton(0) && UnityEngine.Input.GetMouseButton(1))
            {
                // Замінили _currentWeapon на ActiveWeapon
                if (_currentHeavyCharge >= ActiveWeapon.HitsToChargeHeavy)
                {
                    PerformHeavySkill();
                }
                return;
            }

            // 3. ЛЕГКИЙ УДАР (ЛКМ)
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                PerformLightAttack();
            }

            // 4. ЗДІБНІСТЬ ДРУГОЇ РУКИ (Кнопка F)
            if (UnityEngine.Input.GetKeyDown(KeyCode.F))
            {
                // Замінили _currentWeapon на ActiveWeapon
                if (Time.time >= _lastAbilityTime + ActiveWeapon.AbilityCooldown)
                {
                    PerformOffhandAbility();
                }
            }
        }

        private void PerformLightAttack()
        {
            CurrentState = CombatState.Attacking;
            _lastAttackTime = Time.time;

            _comboStep++;
            if (_comboStep > ActiveWeapon.MaxComboHits) _comboStep = 1;

            AttackDirection direction = GetAttackDirection();
            // Замінив тут _currentWeapon.name на ActiveWeapon.name, щоб не було помилки, коли зброї немає
            Debug.Log($"<color=cyan>[Боївка]</color> Удар: {ActiveWeapon.name} | Крок: {_comboStep} | Напрямок: Center");

            // 1. ВМИКАЄМО ФІЗИКУ (Тільки один раз!)
            if (_currentHitTester != null)
            {
                _currentHitTester.StartHitDetection();
                Invoke(nameof(EndAttackHitbox), 0.2f);
            }

            // Додаємо заряд ульти
            if (_currentHeavyCharge < ActiveWeapon.HitsToChargeHeavy)
            {
                _currentHeavyCharge++;
            }

            // Швидкість повернення в Idle
            float animDuration = 0.5f / ActiveWeapon.AttackSpeedMultiplier;
            Invoke(nameof(ResetToIdle), animDuration);

            // 2. ВМИКАЄМО АНІМАЦІЮ
            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetInteger("ComboStep", _comboStep);
                _weaponAnimator.SetTrigger("Attack");
            }
        }

        private void EndAttackHitbox()
        {
            if (_currentHitTester != null)
            {
                _currentHitTester.StopHitDetection();
            }
        }

        private void PerformHeavySkill()
        {
            CurrentState = CombatState.HeavySkill;
            _currentHeavyCharge = 0;

            Debug.Log($"<color=red>[Боївка]</color> ВМІННЯ: {ActiveWeapon.HeavyAbility}!");

            Invoke(nameof(ResetToIdle), 1.0f);
        }

        private void PerformOffhandAbility()
        {
            CurrentState = CombatState.Ability;
            _lastAbilityTime = Time.time;

            Debug.Log($"<color=magenta>[Боївка]</color> ДРУГА РУКА: {ActiveWeapon.OffhandAbility}!");

            Invoke(nameof(ResetToIdle), 0.8f);
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

        private AttackDirection GetAttackDirection()
        {
            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(h) > Mathf.Abs(v))
            {
                return h < 0 ? AttackDirection.Left : AttackDirection.Right;
            }
            return AttackDirection.Center;
        }

        private void CheckComboReset()
        {
            if (CurrentState == CombatState.Idle && _comboStep > 0 && Time.time - _lastAttackTime > _comboResetTime)
            {
                _comboStep = 0;
            }
        }

        private void ResetToIdle()
        {
            CurrentState = CombatState.Idle;
        }

        public void EquipWeapon(WeaponItemData newWeapon)
        {
            // 1. Знищуємо стару модельку меча (або старий хітбокс кулаків)
            if (_currentWeaponModel != null)
            {
                Destroy(_currentWeaponModel);
            }

            // 2. Визначаємо, що беремо в руки. Якщо newWeapon порожній - беремо кулаки!
            WeaponItemData weaponToEquip = newWeapon;
            if (weaponToEquip == null)
            {
                weaponToEquip = _unarmedWeapon; // _unarmedWeapon - це те поле, куди ти поклав файл Fists
            }

            // 3. Оновлюємо логічні дані
            _currentWeapon = weaponToEquip;
            _comboStep = 0;

            // 4. Спавнимо нову 3D-модель АБО невидимий хітбокс кулаків
            if (weaponToEquip != null && weaponToEquip.WeaponPrefab != null)
            {
                _currentWeaponModel = Instantiate(weaponToEquip.WeaponPrefab, _weaponHolder);

                if (_currentWeaponModel != null) // перевір, як у тебе називається змінна заспавненого об'єкта
                {
                    _weaponAnimator = _currentWeaponModel.GetComponent<Animator>();
                }

                // Скидаємо координати
                _currentWeaponModel.transform.localPosition = Vector3.zero;
                _currentWeaponModel.transform.localRotation = Quaternion.identity;

                // Знаходимо тестер і передаємо йому дані
                _currentHitTester = _currentWeaponModel.GetComponentInChildren<MeleeHitTester>();
                if (_currentHitTester != null)
                {
                    _currentHitTester.Setup(weaponToEquip, gameObject);
                }
            }

            Debug.Log($"<color=green>[Екіпірування]</color> Взято зброю: {(weaponToEquip != null ? weaponToEquip.name : "Порожньо")}");
        }
    }
}