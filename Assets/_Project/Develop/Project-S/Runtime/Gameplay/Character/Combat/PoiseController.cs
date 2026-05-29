using Project_S.Runtime.Gameplay.Character.Camera;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class PoiseController : MonoBehaviour
    {
        [Header("Зв'язки")]
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private CameraJuice _cameraJuice;
        [SerializeField] private QteUI _qteUI;

        [Header("Налаштування")]
        [SerializeField] private float _recoveryDelay = 2.0f;
        [SerializeField] private float _targetRecoveryPoise = 50f;
        [SerializeField] private float _poiseGainPerTap = 15f;
        [SerializeField] private float _knockbackForce = 5f;


        [Header("Аудіо")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _poiseBreakSound; 

        private KeyCode _currentQteButton;
        private float _recoveryBlockedUntil;
        private bool _isQTEActive;
        private bool _usesExternalInput;
        private Vector3 _knockbackVector;

        public bool IsBroken => _isQTEActive;
        public Vector3 PendingKnockback => _knockbackVector;

        public Vector3 ConsumeKnockback()
        {
            Vector3 knockback = _knockbackVector;
            _knockbackVector = Vector3.zero;
            return knockback;
        }

        private void Start()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            if (_cameraJuice == null && UnityEngine.Camera.main != null)
                _cameraJuice = UnityEngine.Camera.main.GetComponent<CameraJuice>();
        }

        private void Update()
        {
            if (_usesExternalInput)
                return;

            Tick(PlayerInputSnapshot.Blocked);
        }

        public void Tick(PlayerInputSnapshot input)
        {
            _usesExternalInput = true;

            if (_stats == null) return;

            float current = _stats.Get(StatType.Poise);
            float max = _stats.GetMax(StatType.Poise) > 0 ? _stats.GetMax(StatType.Poise) : 50f;

            if (_isQTEActive)
            {
                if (Time.time >= _recoveryBlockedUntil) { FailQTE(); return; }

                if (WasQtePressed(_currentQteButton, input))
                {
                    _stats.Add(StatType.Poise, _poiseGainPerTap);
                    if (_cameraJuice != null) _cameraJuice.PlayImpactShake(0.05f, 0.02f);

                    if (_stats.Get(StatType.Poise) >= _targetRecoveryPoise)
                    {
                        SuccessQTE();
                    }
                }
            }
            else if (current < max && Time.time > _recoveryBlockedUntil)
            {
                float nextPoise = current + 40f * Time.deltaTime;
                if (nextPoise >= max) nextPoise = max;

                _stats.Set(StatType.Poise, nextPoise);
                if (_cameraJuice != null) _cameraJuice.ResetToNormal();
            }

            _knockbackVector = Vector3.Lerp(_knockbackVector, Vector3.zero, Time.deltaTime * 10f);

            if (_qteUI != null)
            {
                _qteUI.UpdateUI(_stats.Get(StatType.Poise), max, _isQTEActive, _currentQteButton);
            }
        }

        private static bool WasQtePressed(KeyCode key, PlayerInputSnapshot input)
        {
            return key switch
            {
                KeyCode.W => input.QteForwardPressed,
                KeyCode.S => input.QteBackPressed,
                KeyCode.A => input.QteLeftPressed,
                KeyCode.D => input.QteRightPressed,
                _ => false
            };
        }

        public void ApplyPoiseDamage(float amount, Vector3 attackerPosition)
        {
            if (_stats == null) return;

            _stats.Add(StatType.Poise, -amount);
            _recoveryBlockedUntil = Time.time + _recoveryDelay;

            if (_stats.Get(StatType.Poise) <= 0 && !_isQTEActive)
            {
                Vector3 dir = (transform.position - attackerPosition).normalized;
                dir.y = 0;
                _knockbackVector = dir * _knockbackForce;

                if (_audioSource != null && _poiseBreakSound != null)
                {
                    _audioSource.pitch = Random.Range(0.9f, 1.1f);
                    _audioSource.PlayOneShot(_poiseBreakSound);
                }

                StartDirectionalQTE(attackerPosition);
            }
            else if (!_isQTEActive && _cameraJuice != null)
            {
                _cameraJuice.PlayImpactShake(0.15f, 0.05f);
            }
        }

        private void StartDirectionalQTE(Vector3 attackerPos)
        {
            _isQTEActive = true;
            _stats.Set(StatType.Poise, 0f);
            Vector3 relativePos = transform.InverseTransformPoint(attackerPos);

            if (Mathf.Abs(relativePos.z) > Mathf.Abs(relativePos.x))
                _currentQteButton = relativePos.z > 0 ? KeyCode.W : KeyCode.S;
            else
                _currentQteButton = relativePos.x > 0 ? KeyCode.D : KeyCode.A;

            if (_cameraJuice != null) _cameraJuice.TriggerStaggerTilt(relativePos);
        }

        private void SuccessQTE()
        {
            _isQTEActive = false;
            float max = _stats.GetMax(StatType.Poise) > 0 ? _stats.GetMax(StatType.Poise) : 50f;
            _stats.Set(StatType.Poise, max);
            if (_cameraJuice != null) _cameraJuice.TriggerRecoverySuccess();
        }

        private void FailQTE()
        {
            _isQTEActive = false;
            if (_cameraJuice != null) _cameraJuice.TriggerRecoveryFail();
        }
    }
}