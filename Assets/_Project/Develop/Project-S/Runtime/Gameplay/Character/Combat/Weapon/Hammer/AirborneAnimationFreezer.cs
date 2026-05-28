using UnityEngine;
using KinematicCharacterController;

public class AirborneAnimationFreezer : StateMachineBehaviour
{
    [Header("Налаштування заморозки")]
    [Tooltip("На якій секунді заморозити анімацію (наприклад, 0.05).")]
    public float freezeAtSecond = 0.05f; // Тепер вказуємо точний час!

    private KinematicCharacterMotor _motor;
    private bool _hasFrozen = false;

    // Спрацьовує, коли анімація починає грати
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _motor = animator.GetComponentInParent<KinematicCharacterMotor>();
        _hasFrozen = false;
    }

    // Спрацьовує кожен кадр
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (_motor == null) return;

        // Вираховуємо, на якій секунді ми зараз знаходимося
        float currentTimeInSeconds = stateInfo.normalizedTime * stateInfo.length;

        // Якщо анімація дійшла до вказаної секунди
        if (currentTimeInSeconds >= freezeAtSecond && !_hasFrozen)
        {
            // Якщо гравець у повітрі - ставимо на паузу!
            if (!_motor.GroundingStatus.IsStableOnGround)
            {
                animator.speed = 0f;
                _hasFrozen = true;
            }
        }

        // Якщо ми були на паузі, але торкнулися землі
        if (_hasFrozen && _motor.GroundingStatus.IsStableOnGround)
        {
            animator.speed = 1f; // Розморожуємо!
        }
    }

    // Запобіжник: завжди повертаємо нормальну швидкість при виході
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1f;
    }
}