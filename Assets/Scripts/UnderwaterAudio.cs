using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UnderwaterAudio : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        // Отримуємо наш Audio Source на старті
        audioSource = GetComponent<AudioSource>();
    }

    // Коли хтось входить у воду (в Box Collider)
    void OnTriggerEnter(Collider other)
    {
        // Перевіряємо, чи це гравець
        if (other.CompareTag("Player"))
        {
            audioSource.Play();
        }
    }

    // Коли хтось виходить з води
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            audioSource.Stop();
        }
    }
}