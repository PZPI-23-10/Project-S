using UnityEngine;
using KinematicCharacterController;
using Project_S.Runtime.Gameplay.Character.Movement; // Додано для доступу до твого мотора

public class DashAnimationFreezer : StateMachineBehaviour
{
    [Header("Налаштування заморозки")]
    [Tooltip("На якій секунді зупинити анімацію.")]
    public float freezeAtSecond = 0.05f;

    [Tooltip("При якій швидкості розморозити (якщо врізалися в стіну).")]
    public float unfreezeVelocity = 2f;

    private CharacterMotor _customMotor;
    private KinematicCharacterMotor _kccMotor;
    private bool _hasFrozen = false;
    private bool _isDashing = false;

    // Спрацьовує, коли анімація тільки починається
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _customMotor = animator.GetComponentInParent<CharacterMotor>();
        _kccMotor = animator.GetComponentInParent<KinematicCharacterMotor>();
        _hasFrozen = false;
        _isDashing = false;
    }

    // Спрацьовує кожен кадр
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_kccMotor == null) return;

        float currentTimeInSeconds = stateInfo.normalizedTime * stateInfo.length;

        // 1. Ставимо на паузу, коли дійшли до ідеальної пози
        if (currentTimeInSeconds >= freezeAtSecond && !_hasFrozen)
        {
            animator.speed = 0f;
            _hasFrozen = true;
            _isDashing = true;
        }

        // 2. Знімаємо з паузи
        if (_isDashing)
        {
            float currentSpeed = _kccMotor.Velocity.magnitude;

            // Перевіряємо, чи наш мотор вважає, що ривок ОФІЦІЙНО завершено 
            // (час вийшов, або ми врізалися у ворога)
            bool isDashOfficiallyOver = _customMotor != null && !_customMotor.IsAttackDashing;

            // Розморожуємо, якщо швидкість впала (стіна) АБО ривок закінчився
            if (currentSpeed <= unfreezeVelocity || isDashOfficiallyOver)
            {
                animator.speed = 1f; // Розморожуємо!
                _isDashing = false;
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1f;
    }
}