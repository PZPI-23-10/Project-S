using UnityEngine;

public class LocalWaterFX : MonoBehaviour
{
    [Header("Ефекти:")]
    public ParticleSystem bubbles;
    private AudioSource audioSource;

    private Collider waterCollider;
    private bool isSubmerged = false;

    void Start()
    {
        waterCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // 1. Перевіряємо, чи зайшла камера у зелений куб озера (Box Collider)
        if (waterCollider.bounds.Contains(Camera.main.transform.position))
        {
            // 2. Перевіряємо, чи опустилася камера під воду (Y координата води - 20 см)
            if (Camera.main.transform.position.y < (transform.position.y - 0.2f))
            {
                if (!isSubmerged) TurnOnEffects();
            }
            else
            {
                if (isSubmerged) TurnOffEffects();
            }
        }
        else
        {
            // Якщо камера взагалі вийшла з озера
            if (isSubmerged) TurnOffEffects();
        }
    }

    private void TurnOnEffects()
    {
        isSubmerged = true;
        if (bubbles != null) bubbles.Play();
        if (audioSource != null) audioSource.Play();
    }

    private void TurnOffEffects()
    {
        isSubmerged = false;
        if (bubbles != null)
        {
            bubbles.Stop();
            bubbles.Clear(); // Миттєво вбиває всі бульбашки, щоб ти не бачив їх з берега
        }
        if (audioSource != null) audioSource.Stop();
    }
}