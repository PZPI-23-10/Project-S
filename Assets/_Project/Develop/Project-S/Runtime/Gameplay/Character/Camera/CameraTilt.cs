using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Respawn;

public class CameraTilt : MonoBehaviour, IPlayerRespawnResettable
{
    [Header("Налаштування")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _tiltAmount = 2.0f;
    [SerializeField] private float _tiltSpeed = 5.0f;

    private float _currentTilt = 0f;
    private bool _isEnabled = true; 

    public void SetEnabled(bool enabled) => _isEnabled = enabled;

    public void ResetForRespawn()
    {
        _currentTilt = 0f;
        _isEnabled = true;

        if (_cameraTransform != null)
            _cameraTransform.localRotation = Quaternion.identity;
    }

    public void Tick(PlayerInputSnapshot input)
    {
        if (!_isEnabled)
        {
            _currentTilt = Mathf.Lerp(_currentTilt, 0, Time.deltaTime * _tiltSpeed);
        }
        else
        {
            float targetTilt = -input.Move.x * _tiltAmount;
            _currentTilt = Mathf.Lerp(_currentTilt, targetTilt, Time.deltaTime * _tiltSpeed);
        }

        if (_cameraTransform != null)
        {
            _cameraTransform.localRotation = Quaternion.Euler(0, 0, _currentTilt);
        }
    }
}