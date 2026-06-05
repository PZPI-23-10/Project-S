using System.Collections;
using Project_S.Runtime.Gameplay.Respawn;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Camera
{
    public class CameraJuice : MonoBehaviour, IPlayerRespawnResettable
    {
        [Header("Зв'язки")]
        [SerializeField] private Transform _weaponRoot;

        [Header("Налаштування тряски (Імпакт)")]
        [SerializeField] private float _defaultDuration = 0.3f;
        [SerializeField] private float _defaultMagnitude = 0.1f;

        [Header("Налаштування завалу (Stagger)")]
        [SerializeField] private float _rollAngle = 20f;
        [SerializeField] private float _tiltSpeed = 10f;
        [SerializeField] private Vector3 _weaponStaggerOffset = new Vector3(0.2f, -0.3f, 0f);

        private Vector3 _originalCamPos;
        private Quaternion _originalCamRot;
        private Vector3 _originalWeaponPos;

        private float _targetRoll;
        private float _currentRoll;
        private float _targetHeightOffset;
        private float _currentHeightOffset;
        private Vector3 _targetWeaponPos;

        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            _originalCamPos = transform.localPosition;
            _originalCamRot = transform.localRotation;

            if (_weaponRoot != null)
                _originalWeaponPos = _weaponRoot.localPosition;
        }

        private void Update()
        {
            _currentRoll = Mathf.Lerp(_currentRoll, _targetRoll, Time.deltaTime * _tiltSpeed);
            _currentHeightOffset = Mathf.Lerp(_currentHeightOffset, _targetHeightOffset, Time.deltaTime * _tiltSpeed);

            transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, _currentRoll);
            transform.localPosition = new Vector3(_originalCamPos.x, _originalCamPos.y + _currentHeightOffset, _originalCamPos.z);

            if (_weaponRoot != null)
            {
                _weaponRoot.localPosition = Vector3.Lerp(_weaponRoot.localPosition, _targetWeaponPos, Time.deltaTime * (_tiltSpeed * 0.8f));
            }
        }

        public void PlayImpactShake(float duration = -1, float magnitude = -1)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ImpactShakeRoutine(
                duration > 0 ? duration : _defaultDuration,
                magnitude > 0 ? magnitude : _defaultMagnitude
            ));
        }

        public void TriggerStaggerTilt(Vector3 hitDirectionRelative)
        {
            if (hitDirectionRelative.x > 0)
            {
                _targetRoll = _rollAngle;
                _targetWeaponPos = _originalWeaponPos + new Vector3(-_weaponStaggerOffset.x, _weaponStaggerOffset.y, _weaponStaggerOffset.z);
            }
            else
            {
                _targetRoll = -_rollAngle;
                _targetWeaponPos = _originalWeaponPos + new Vector3(_weaponStaggerOffset.x, _weaponStaggerOffset.y, _weaponStaggerOffset.z);
            }

            _targetHeightOffset = -0.15f;
        }

        public void TriggerRecoverySuccess()
        {
            _currentRoll = -(_targetRoll * 0.3f);
            _targetRoll = 0f;
            _targetHeightOffset = 0f;
            _targetWeaponPos = _originalWeaponPos;
        }

        public void TriggerRecoveryFail()
        {
            _targetRoll = _targetRoll > 0 ? 45f : -45f;
            _targetHeightOffset = -0.6f;
            _targetWeaponPos = _originalWeaponPos + new Vector3(0f, -0.5f, 0f);
        }

        public void ResetToNormal()
        {
            _targetRoll = 0f;
            _targetHeightOffset = 0f;
            _targetWeaponPos = _originalWeaponPos;
        }

        public void ResetForRespawn()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            _targetRoll = 0f;
            _currentRoll = 0f;
            _targetHeightOffset = 0f;
            _currentHeightOffset = 0f;
            _targetWeaponPos = _originalWeaponPos;

            transform.localPosition = _originalCamPos;
            transform.localRotation = _originalCamRot;

            if (_weaponRoot != null)
                _weaponRoot.localPosition = _originalWeaponPos;
        }

        private IEnumerator ImpactShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_targetHeightOffset > -0.5f)
                {
                    float x = Random.Range(-1f, 1f) * magnitude;
                    float y = Random.Range(-1f, 1f) * magnitude;
                    transform.localPosition = new Vector3(_originalCamPos.x + x, _originalCamPos.y + _currentHeightOffset + y, _originalCamPos.z);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = new Vector3(_originalCamPos.x, _originalCamPos.y + _currentHeightOffset, _originalCamPos.z);
        }
    }
}