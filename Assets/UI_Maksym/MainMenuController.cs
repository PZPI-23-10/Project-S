using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    private void Start()
    {
        // Якщо ми вийшли з меню паузи, час міг залишитися зупиненим (Time.timeScale = 0). 
        // Тому в головному меню обов'язково відновлюємо його.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

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
        // Завантажуємо Core, бо саме в Core знаходиться гравець, камера і логіка гри,
        // яка вже сама завантажить світ (YavWorld).
        SceneManager.LoadScene("Core");
    }
}