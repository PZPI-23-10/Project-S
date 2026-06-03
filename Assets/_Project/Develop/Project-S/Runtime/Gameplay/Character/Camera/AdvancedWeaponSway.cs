using UnityEngine;

public class AdvancedWeaponSway : MonoBehaviour
{
    [Header("Інерція від миші (Sway)")]
    [SerializeField] private float _swayAmount = 0.02f;
    [SerializeField] private float _maxSway = 0.06f;
    [SerializeField] private float _swaySmoothness = 6f;

    [Header("Хитання при кроках (Bobbing)")]
    [SerializeField] private float _bobSpeed = 14f;
    [SerializeField] private float _bobAmountX = 0.05f; // Рух вліво-вправо
    [SerializeField] private float _bobAmountY = 0.05f; // Рух вгору-вниз

    private Vector3 _defaultPos;
    private float _timer = 0f;

    void Start()
    {
        _defaultPos = transform.localPosition;
    }

    void Update()
    {
        // === 1. SWAY (ІНЕРЦІЯ МИШІ) ===
        // Беремо рух миші і інвертуємо його (щоб зброя відставала)
        float mouseX = -Input.GetAxis("Mouse X") * _swayAmount;
        float mouseY = -Input.GetAxis("Mouse Y") * _swayAmount;

        // Обмежуємо, щоб зброя не вилетіла за екран при різкому ривку
        mouseX = Mathf.Clamp(mouseX, -_maxSway, _maxSway);
        mouseY = Mathf.Clamp(mouseY, -_maxSway, _maxSway);

        Vector3 targetSway = new Vector3(mouseX, mouseY, 0);


        // === 2. BOBBING (ВІСІМКА ПРИ КРОКАХ) ===
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 targetBob = Vector3.zero;

        // Якщо гравець натискає WASD
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            _timer += Time.deltaTime * _bobSpeed;

            // МАГІЯ ВІСІМКИ: X рухається вдвічі повільніше за Y (timer / 2)
            targetBob.x = Mathf.Cos(_timer / 2) * _bobAmountX;
            targetBob.y = Mathf.Sin(_timer) * _bobAmountY;
        }
        else
        {
            // Плавно скидаємо таймер, щоб зупинка виглядала природно
            _timer = 0f;
        }

        // === 3. ФІНАЛЬНИЙ РУХ ===
        // Додаємо базову позицію + інерцію + кроки
        Vector3 finalPosition = _defaultPos + targetSway + targetBob;

        // ПЛАВНІСТЬ: Використовуємо Lerp, щоб рух не був "смиканим"
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition, Time.deltaTime * _swaySmoothness);
    }
}