using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Цей метод буде викликатися при натисканні на кнопку "Вихід"
    public void QuitGame()
    {
        // Виводить повідомлення в консоль (щоб ми бачили, що кнопка працює в редакторі)
        Debug.Log("Виходимо з гри!");

        // Закриває саму гру (працює тільки у скомпільованій версії, не в редакторі)
        Application.Quit();
    }
    public void PlayGame()
    {
        // В дужках має бути ТОЧНА назва твоєї ігрової сцени
        SceneManager.LoadScene("YavWorld");
    }
}