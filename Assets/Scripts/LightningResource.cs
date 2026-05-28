using UnityEngine;
using System.Collections;

public class LightningResource : MonoBehaviour
{
    [Header("Lifetime Settings")]
    [Tooltip("Скільки секунд об'єкт існує до зникнення")]
    public float lifetimeSeconds = 30f;
    [Tooltip("Як довго триває анімація зникнення (зменшення розміру)")]
    public float fadeOutDuration = 2f;

    [Header("Animation Settings")]
    [Tooltip("Швидкість обертання навколо своєї осі")]
    public float spinSpeed = 90f;
    [Tooltip("Амплітуда (висота) покачування вверх-вниз")]
    public float hoverAmplitude = 0.25f;
    [Tooltip("Швидкість покачування")]
    public float hoverSpeed = 2.5f;

    private Vector3 initialPosition;
    private bool isFading = false;

    private void Start()
    {
        // Запам'ятовуємо стартову позицію, щоб куб левітував відносно землі, а не відлітав у космос
        initialPosition = transform.position;
        StartCoroutine(ShrinkAndDestroyRoutine());
    }

    private void Update()
    {
        // 1. Обертання навколо осі Y
        transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);

        // 2. Левітація (покачування), поки об'єкт не почав зникати
        if (!isFading)
        {
            // Використовуємо синусоїду для плавного руху вверх і вниз
            float newY = initialPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    private IEnumerator ShrinkAndDestroyRoutine()
    {
        // Чекаємо заданий час
        yield return new WaitForSeconds(lifetimeSeconds);

        isFading = true; // Вимикаємо левітацію, щоб не було ривків під час зникнення

        Vector3 initialScale = transform.localScale;
        Vector3 fadeStartPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;

            // Плавно зменшуємо розмір до нуля
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, elapsedTime / fadeOutDuration);

            // Трохи опускаємо його назад до землі під час зникнення
            transform.position = Vector3.Lerp(fadeStartPos, initialPosition, elapsedTime / fadeOutDuration);

            yield return null;
        }

        // Остаточно знищуємо об'єкт
        Destroy(gameObject);
    }
}