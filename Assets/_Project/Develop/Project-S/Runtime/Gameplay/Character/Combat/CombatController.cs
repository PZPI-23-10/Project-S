using System.Collections; 
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Character.Camera;
using Project_S.Runtime.Gameplay.Respawn;

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

    public class CombatController : MonoBehaviour, IPlayerRespawnResettable
    {
        [Header("Зв'язки")]
        [SerializeField] private StaminaController _stamina;
        [SerializeField] private BlockController _blockController;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _defaultEquipSound;

        private PoiseController _poiseController;

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
        [SerializeField] private bool _isOffhandSkillUnlocked;

        public WeaponItemData ActiveWeapon => _currentWeapon != null ? _currentWeapon : _unarmedWeapon;
        public WeaponItemData CurrentWeapon => ActiveWeapon;
        public WeaponItemData SavedCurrentWeapon => _currentWeapon;
        public WeaponItemData EquippedOffhandItem => _equippedOffhandItem;
        public bool IsOffhandSkillUnlocked => _isOffhandSkillUnlocked;
        public CombatState CurrentState { get; private set; } = CombatState.Idle;
        public event System.Action Changed;

        private bool _isComboWindowOpen = false;
        private bool _nextAttackBuffered = false;
        private bool _isTransitioningToNextCombo = false;
        private int _comboStep = 0;
        public int ComboStep => _comboStep;

        private int _currentHeavyCharge = 0;
        private float _lastAbilityTime = 0f;

        private GameObject _activeWeaponVFX;
        public AudioClip ActiveCoatingSwingSound { get; private set; }
        public AudioClip ActiveCoatingHitSound { get; private set; }

        private Coroutine _drawWeaponCoroutine;
        private Coroutine _hitStopCoroutine;

        private void Start()
        {
            _poiseController = GetComponent<PoiseController>();
        }

        public void ResetForRespawn()
        {
            if (_drawWeaponCoroutine != null)
            {
                StopCoroutine(_drawWeaponCoroutine);
                _drawWeaponCoroutine = null;
            }

            if (_hitStopCoroutine != null)
            {
                StopCoroutine(_hitStopCoroutine);
                _hitStopCoroutine = null;
            }

            CancelInvoke(nameof(FailsafeReset));
            CancelInvoke(nameof(ForceResetToIdle));
            CancelInvoke(nameof(RemoveWeaponCoating));

            if (_blockController != null)
                _blockController.StopBlock();

            if (_currentHitTester != null)
                _currentHitTester.StopHitDetection();

            RemoveWeaponCoating();

            _isComboWindowOpen = false;
            _nextAttackBuffered = false;
            _isTransitioningToNextCombo = false;
            _comboStep = 0;
            CurrentState = CombatState.Idle;

            if (_currentWeaponModel != null)
            {
                _currentWeaponModel.transform.localPosition = Vector3.zero;
                _currentWeaponModel.transform.localRotation = Quaternion.identity;
            }

            if (_currentOffhandModel != null)
            {
                _currentOffhandModel.transform.localPosition = Vector3.zero;
                _currentOffhandModel.transform.localRotation = Quaternion.identity;
            }

            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetInteger("ComboStep", 0);
                _weaponAnimator.SetBool("IsBlocking", false);
                _weaponAnimator.SetBool("PhylacteryActive", _isOffhandActive);
                _weaponAnimator.Update(0f);
            }

            Time.timeScale = 1f;
        }

        // ==========================================
        // ФІКС: Запобіжник, щоб час ніколи не залишався зупиненим назавжди!
        // ==========================================
        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        public void Tick(PlayerInputSnapshot input)
        {
            if (_poiseController != null && _poiseController.IsBroken)
            {
                if (CurrentState == CombatState.Blocking)
                {
                    StopBlocking();
                }
                return;
            }

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
            if (input.OffhandAbilityPressed && _isOffhandActive)
            {
                if (Time.time >= _lastAbilityTime + _equippedOffhandItem.AbilityCooldown)
                {
                    PerformOffhandAbility();
                }
                else
                {
                    float timeLeft = (_lastAbilityTime + _equippedOffhandItem.AbilityCooldown) - Time.time;
                    Debug.Log($"<color=grey>[Кулдаун]</color> Здібність ще заряджається! Залишилось: {timeLeft:F1} сек.");
                }
            }
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
            if (_currentOffhandModel != null) DestroyObjectSafe(_currentOffhandModel);

            if (!_isCombatOffhandInHand && _isOffhandSkillUnlocked && _equippedOffhandItem != null && _equippedOffhandItem != _unarmedWeapon)
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

        public void SetOffhandSkillUnlocked(bool unlocked)
        {
            if (_isOffhandSkillUnlocked == unlocked)
                return;

            _isOffhandSkillUnlocked = unlocked;

            if (!_isOffhandSkillUnlocked && _isCombatOffhandInHand)
                ToggleOffhand();
        }

        public void TryShowCombatOffhand()
        {
            if (!_isOffhandSkillUnlocked || _equippedOffhandItem == null || _equippedOffhandItem == _unarmedWeapon) return;
            if (_isCombatOffhandInHand || _equippedOffhandItem.WeaponPrefab == null || ActiveWeapon == null || ActiveWeapon.IsTwoHanded)
                return;

            if (_currentOffhandModel != null)
                DestroyObjectSafe(_currentOffhandModel);

            _isCombatOffhandInHand = true;
            _isPhylacteryInHand = false;
            _currentOffhandModel = Instantiate(_equippedOffhandItem.WeaponPrefab, _offhandHolder);
            if (_weaponAnimator != null) _weaponAnimator.SetBool("PhylacteryActive", true);

            if (_currentOffhandModel != null)
            {
                _currentOffhandModel.transform.localPosition = Vector3.zero;
                _currentOffhandModel.transform.localRotation = Quaternion.identity;
            }
        }

        public void EquipOffhand(WeaponItemData newOffhandItem)
        {
            if (newOffhandItem == _unarmedWeapon) newOffhandItem = null;

            _equippedOffhandItem = newOffhandItem;
            if (_isOffhandActive)
            {
                ToggleOffhand(); if (newOffhandItem != null) ToggleOffhand();
            }
        }

        public void EquipWeapon(WeaponItemData newWeapon)
        {
            RemoveWeaponCoating();

            var buffController = GetComponent<BuffController>();
            if (buffController != null)
            {
                buffController.ClearWeaponBuffs();
            }

            if (_currentWeapon == newWeapon && _currentWeaponModel != null)
            {
                return;
            }

            if (_drawWeaponCoroutine != null)
            {
                StopCoroutine(_drawWeaponCoroutine);
                _drawWeaponCoroutine = null;
            }

            if (_currentWeaponModel != null) DestroyObjectSafe(_currentWeaponModel);

            WeaponItemData weaponToEquip = newWeapon != null ? newWeapon : _unarmedWeapon;
            _currentWeapon = newWeapon; _comboStep = 0;

            if (weaponToEquip != null && weaponToEquip.IsTwoHanded && _isOffhandActive) ToggleOffhand();

            if (weaponToEquip != null && weaponToEquip.WeaponPrefab != null)
            {
                _currentWeaponModel = Instantiate(weaponToEquip.WeaponPrefab, _weaponHolder);
                if (_currentWeaponModel != null) _weaponAnimator = _currentWeaponModel.GetComponent<Animator>();

                _currentHitTester = _currentWeaponModel.GetComponentInChildren<MeleeHitTester>();
                if (_currentHitTester != null) _currentHitTester.Setup(weaponToEquip, gameObject);

                _drawWeaponCoroutine = StartCoroutine(DrawWeaponRoutine(_currentWeaponModel.transform));
            }

            if (_defaultEquipSound != null && _audioSource != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.15f);
                _audioSource.PlayOneShot(_defaultEquipSound);
            }
        }

        // ==========================================
        // ФІКС: БЕЗПЕЧНА ЗУПИНКА ЧАСУ ТУТ (Щоб гра більше не лагала)
        // ==========================================
        public void TriggerHitImpact()
        {
            if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
            _hitStopCoroutine = StartCoroutine(HitImpactRoutine());
        }

        private IEnumerator HitImpactRoutine()
        {
            Time.timeScale = 0.05f; // Уповільнюємо час

            if (UnityEngine.Camera.main != null)
            {
                CameraJuice camJuice = UnityEngine.Camera.main.GetComponent<CameraJuice>();
                if (camJuice != null) camJuice.PlayImpactShake(0.1f, 0.02f);
            }

            // Чекаємо в РЕАЛЬНОМУ часі, щоб не застрягти назавжди
            yield return new WaitForSecondsRealtime(0.04f);
            Time.timeScale = 1f; // Повертаємо час у норму
        }
        // ==========================================

        private IEnumerator DrawWeaponRoutine(Transform weaponTransform)
        {
            float duration = 0.25f; // Швидкість діставання зброї (0.25 сек)
            float elapsed = 0f;

            // Зброя з'являється знизу екрана і трохи нахилена вперед
            Vector3 startPos = new Vector3(0f, -0.6f, 0.2f);
            Vector3 endPos = Vector3.zero;

            // Нахил зброї: від 45 градусів (лежить) до 0 (рівно в руці)
            Quaternion startRot = Quaternion.Euler(45f, 0f, 0f);
            Quaternion endRot = Quaternion.identity;

            weaponTransform.localPosition = startPos;
            weaponTransform.localRotation = startRot;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Математична формула для плавності (Ease Out - різко починається, плавно закінчується)
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);

                if (weaponTransform != null)
                {
                    weaponTransform.localPosition = Vector3.Lerp(startPos, endPos, smoothT);
                    weaponTransform.localRotation = Quaternion.Lerp(startRot, endRot, smoothT);
                }

                yield return null;
            }

            // На всяк випадок жорстко ставимо нулі в кінці
            if (weaponTransform != null)
            {
                weaponTransform.localPosition = endPos;
                weaponTransform.localRotation = endRot;
            }
        }

        public void AddChargeOnHit()
        {
            if (CurrentState == CombatState.HeavySkill)
            {
                Debug.Log("<color=grey>[Боївка]</color> Важкий удар не заряджає зброю.");
                return;
            }

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

        public void ApplyWeaponCoating(GameObject vfxPrefab, float duration, AudioClip swingSound, AudioClip hitSound)
        {
            if (_currentWeaponModel == null || vfxPrefab == null) return;

            if (_activeWeaponVFX != null) DestroyObjectSafe(_activeWeaponVFX);

            Transform targetAnchor = _currentWeaponModel.transform; 
            if (_currentHitTester != null)
            {
                Collider hitbox = _currentHitTester.GetComponent<Collider>();
                if (hitbox == null) hitbox = _currentHitTester.GetComponentInChildren<Collider>();

                if (hitbox != null) targetAnchor = hitbox.transform;
            }

            _activeWeaponVFX = Instantiate(vfxPrefab, targetAnchor);

            _activeWeaponVFX.transform.localPosition = Vector3.zero;
            _activeWeaponVFX.transform.localRotation = Quaternion.identity;

            ActiveCoatingSwingSound = swingSound;
            ActiveCoatingHitSound = hitSound;

            CancelInvoke(nameof(RemoveWeaponCoating));
            Invoke(nameof(RemoveWeaponCoating), duration);
        }

        private void RemoveWeaponCoating()
        {
            if (_activeWeaponVFX != null)
            {
                DestroyObjectSafe(_activeWeaponVFX);
                Debug.Log("<color=cyan>[Combat]</color> Дія змазки закінчилася.");
            }
            ActiveCoatingSwingSound = null;
            ActiveCoatingHitSound = null;
        }

        private void PerformHeavySkill()
        {
            CurrentState = CombatState.HeavySkill;
            if (ActiveWeapon.HeavyAbilityData != null && ActiveWeapon.HeavyAbilityData.ResetChargeOnUse)
            {
                _currentHeavyCharge = 0;
            }

            if (ActiveWeapon.HeavyAbilityData is EarthquakeAbility earthquake)
            {
                earthquake.StartJump(this);
            }

            if (_weaponAnimator != null)
            {
                _weaponAnimator.SetTrigger("HeavySkill");
            }
        }

        private void PerformOffhandAbility()
        {
            CurrentState = CombatState.Ability;
            _lastAbilityTime = Time.time;

            if (_equippedOffhandItem != null && _currentOffhandModel != null)
            {
                Debug.Log($"<color=yellow>[Ліва рука]</color> Застосовуємо здібність: {_equippedOffhandItem.name}!");

                IOffhandAbility offhandSkill = _currentOffhandModel.GetComponentInChildren<IOffhandAbility>();

                if (offhandSkill != null)
                {
                    offhandSkill.ExecuteOffhandAbility(this, _weaponAnimator);
                }
                else
                {
                    Debug.LogWarning("На префабі лівої руки немає скрипта, який реалізує IOffhandAbility!");
                }
            }

            Invoke(nameof(ForceResetToIdle), 0.5f);
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

        public void AnimEvent_PlaySwingSound()
        {
            if (_audioSource != null)
            {
                float basePitch = Random.Range(0.9f, 1.1f);
                float finalPitch = basePitch * GetAttackSpeedMultiplier();

                if (ActiveWeapon != null && ActiveWeapon.SwingSound != null)
                {
                    _audioSource.pitch = finalPitch;
                    _audioSource.PlayOneShot(ActiveWeapon.SwingSound);
                }

                if (ActiveCoatingSwingSound != null)
                {
                    _audioSource.pitch = finalPitch;
                    _audioSource.PlayOneShot(ActiveCoatingSwingSound);
                }
            }
        }

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
            if (ActiveWeapon != null && ActiveWeapon.HeavyAbilityData != null)
            {
                ActiveWeapon.HeavyAbilityData.ExecuteHeavyAbility(this, _weaponAnimator, _currentWeaponModel);
            }
        }

        public void PlayHitSound(AudioClip hitSound)
        {
            if (_audioSource != null && hitSound != null)
            {
                _audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
                _audioSource.PlayOneShot(hitSound);
            }
        }

        private static void DestroyObjectSafe(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
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
