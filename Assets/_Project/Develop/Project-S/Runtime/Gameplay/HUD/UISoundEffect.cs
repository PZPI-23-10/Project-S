using UnityEngine;

public class UISoundEffect : MonoBehaviour
{
    [SerializeField] private AudioClip _openSound;

    // Цей метод автоматично спрацьовує щоразу, коли ти відкриваєш вікно UI!
    private void OnEnable()
    {
        if (_openSound != null && Camera.main != null)
        {
            // Граємо звук прямо в камері, щоб його було чути без 3D-затухання
            AudioSource.PlayClipAtPoint(_openSound, Camera.main.transform.position, 0.8f);
        }
    }
}