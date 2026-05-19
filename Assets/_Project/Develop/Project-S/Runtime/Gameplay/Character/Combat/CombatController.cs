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
        Center,
        Left,
        Right
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
        [SerializeField] private WeaponItemData _equippedOffhandItem; // ЛІВА РУКА (те, що лежить у слоті інвентарю)

        [Header("Прогресія (Скіли)")]
        [SerializeField] private bool _isOffhandSkillUnlocked = true; // ГАЛОЧКА ДЛЯ ТЕСТУ ПРОКАЧКИ


        [Header("Налаштування Комбо")]
        [SerializeField] private float _comboResetTime = 1.5f;

        public WeaponItemData ActiveWeapon => _currentWeapon != null ? _currentWeapon : _unarmedWeapon;
        public WeaponItemData CurrentWeapon => ActiveWeapon;
        public CombatState CurrentState { get; private set; } = CombatState.Idle;

        private int _comboStep = 0;
        private int _currentHeavyCharge = 0;
        private float _lastAttackTime = 0f;
        private float _lastAbilityTime = 0f;

        public void Tick(PlayerInputSnapshot input)
        {
            if (CurrentState == CombatState.Staggered) return;
            if (ActiveWeapon == null) return;

            HandleCombatInput(input);
            CheckComboReset();
        }

        private void HandleCombatInput(PlayerInputSnapshot input)
        {
            // 1. ДІСТАТИ / СХОВАТИ ПРЕДМЕТ У ЛІВІЙ РУЦІ
            if (input.ToggleOffhandPressed && CurrentState == CombatState.Idle)
            {
                ToggleOffhand();
            }

            // 2. ВАЖКИЙ УДАР (ЛКМ ЗАЖАТО + ПКМ НАТИСНУТО)
            // Ми перевіряємо це до блоку, щоб гра не плутала ПКМ для блоку з ПКМ для здібності
            if (input.HeavyAttackPressed && !_isOffhandActive)
            {
                if (_currentHeavyCharge >= ActiveWeapon.HitsToChargeHeavy)
                {
                    PerformHeavySkill();
                }
                else
                {
                    Debug.Log($"<color=orange>[Боївка]</color> Вміння ще не заряджено! ({_currentHeavyCharge}/{ActiveWeapon.HitsToChargeHeavy})");
                }
                return; // Зупиняємо перевірку, щоб не спрацював звичайний блок
            }

            // 3. БЛОК (ПКМ)
            if (input.BlockHeld && CurrentState == CombatState.Idle && !_isOffhandActive)
            {
                StartBlocking();
            }
            else if (!input.BlockHeld && CurrentState == CombatState.Blocking)
            {
                StopBlocking();
            }

            if (CurrentState != CombatState.Idle) return;

            // 4. ЛЕГКИЙ УДАР (ЛКМ)
            if (input.LightAttackPressed)
            {
                PerformLightAttack();
            }

            // 5. ЗДІБНІСТЬ ДРУГОЇ РУКИ (Кнопка F)
            if (input.OffhandAbilityPressed)
            {
                if (_isOffhandActive)
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

            // Якщо в руках КУЛАКИ і активна ліва рука — комбо ріжеться до 1 удару
            if (_currentWeapon == null && _isOffhandActive)
            {
                _comboStep = 1;
            }
            else if (_comboStep > ActiveWeapon.MaxComboHits)
            {
                _comboStep = 1;
            }

            Debug.Log($"<color=cyan>[Боївка]</color> Удар: {ActiveWeapon.name} | Крок: {_comboStep}");

            float animDuration = 0.5f / GetAttackSpeedMultiplier();
            Invoke(nameof(ResetToIdle), animDuration);

            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetInteger("ComboStep", _comboStep);
                _weaponAnimator.SetTrigger("Attack");
            }
        }

        private void ToggleOffhand()
        {
            // Якщо зброя дворучна — ліва рука взагалі заблокована
            if (ActiveWeapon.IsTwoHanded)
            {
                Debug.Log("<color=orange>[Боївка]</color> Не можна використати ліву руку! Зброя дворучна.");
                return;
            }

            // Очищаємо ліву руку перед тим, як щось туди дати
            if (_currentOffhandModel != null)
            {
                Destroy(_currentOffhandModel);
            }

            // ЛОГІКА ПЕРЕМИКАННЯ
            // Якщо зараз в руці нічого немає (або був ліхтар), і в нас Є ПРОКАЧКА та ЕКІПІРОВАНИЙ ПРЕДМЕТ
            if (!_isCombatOffhandInHand && _isOffhandSkillUnlocked && _equippedOffhandItem != null)
            {
                // Дістаємо бойовий предмет (Щит/Кинджал)
                _isCombatOffhandInHand = true;
                _isPhylacteryInHand = false;

                _currentOffhandModel = Instantiate(_equippedOffhandItem.WeaponPrefab, _offhandHolder);
                if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", true);

                Debug.Log($"<color=magenta>[Ліва рука]</color> Дістали бойовий предмет: {_equippedOffhandItem.name}");
            }
            // Якщо бойового предмета немає, скіл не вкачаний, АБО ми хочемо змінити Щит на Ліхтар
            else if (!_isPhylacteryInHand)
            {
                // Дістаємо Філактерій (ЙОМУ НЕ ПОТРІБНА ПРОКАЧКА!)
                _isPhylacteryInHand = true;
                _isCombatOffhandInHand = false;

                if (_phylacteryPrefab != null)
                {
                    _currentOffhandModel = Instantiate(_phylacteryPrefab, _offhandHolder);
                }
                if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", true);

                Debug.Log("<color=yellow>[Ліва рука]</color> Дістали Філактерій!");
            }
            // Якщо в руці вже був Філактерій — просто ховаємо все
            else
            {
                _isPhylacteryInHand = false;
                _isCombatOffhandInHand = false;
                if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", false);

                Debug.Log("<color=grey>[Ліва рука]</color> Сховали все в ножни.");
            }

            // Скидаємо координати спавну, щоб предмет рівно сидів у руці
            if (_currentOffhandModel != null)
            {
                _currentOffhandModel.transform.localPosition = Vector3.zero;
                _currentOffhandModel.transform.localRotation = Quaternion.identity;
            }
        }

        // Цей метод викликає інвентар, коли гравець перетягує предмет у слот лівої руки
        public void EquipOffhand(WeaponItemData newOffhandItem)
        {
            _equippedOffhandItem = newOffhandItem;

            if (_isOffhandActive)
            {
                ToggleOffhand(); // Сховати старий
                if (newOffhandItem != null)
                {
                    ToggleOffhand(); // Дістати новий
                }
            }

            Debug.Log($"<color=green>[Екіпірування]</color> У ліву руку покладено: {(newOffhandItem != null ? newOffhandItem.name : "Порожньо")}");
        }

        public void AddChargeOnHit()
        {
            if (_currentHeavyCharge < ActiveWeapon.HitsToChargeHeavy)
            {
                _currentHeavyCharge++;
                Debug.Log($"<color=yellow>[Боївка]</color> Влучання! Заряд: {_currentHeavyCharge} / {ActiveWeapon.HitsToChargeHeavy}");
            }
        }

        public void AnimEvent_StartHitbox()
        {
            if (_currentHitTester != null) _currentHitTester.StartHitDetection();
        }

        public void AnimEvent_StopHitbox()
        {
            if (_currentHitTester != null) _currentHitTester.StopHitDetection();
        }

        public void EquipWeapon(WeaponItemData newWeapon)
        {
            if (_currentWeaponModel != null) Destroy(_currentWeaponModel);

            WeaponItemData weaponToEquip = newWeapon != null ? newWeapon : _unarmedWeapon;
            _currentWeapon = newWeapon;
            _comboStep = 0;

            if (weaponToEquip != null && weaponToEquip.IsTwoHanded && _isOffhandActive)
            {
                ToggleOffhand();
            }

            if (weaponToEquip != null && weaponToEquip.WeaponPrefab != null)
            {
                _currentWeaponModel = Instantiate(weaponToEquip.WeaponPrefab, _weaponHolder);
                if (_currentWeaponModel != null)
                {
                    _weaponAnimator = _currentWeaponModel.GetComponent<Animator>();
                }

                _currentWeaponModel.transform.localPosition = Vector3.zero;
                _currentWeaponModel.transform.localRotation = Quaternion.identity;

                _currentHitTester = _currentWeaponModel.GetComponentInChildren<MeleeHitTester>();
                if (_currentHitTester != null)
                {
                    _currentHitTester.Setup(weaponToEquip, gameObject);
                }
            }

            Debug.Log($"<color=green>[Екіпірування]</color> Взято зброю: {weaponToEquip.name}");
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
            Invoke(nameof(ResetToIdle), 1.0f);
        }

        private void PerformOffhandAbility()
        {
            CurrentState = CombatState.Ability;
            _lastAbilityTime = Time.time;

            // Якщо екіпіровано предмет, викликаємо його здібність (наприклад, Філактерій)
            if (_equippedOffhandItem != null)
            {
                Debug.Log($"<color=yellow>[Магія]</color> ЗДІБНІСТЬ ЛІВОЇ РУКИ: {_equippedOffhandItem.OffhandAbility}!");
            }

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
    }
}