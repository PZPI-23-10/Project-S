using UnityEngine;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _pauseMenuUI; // Сюди закинеш свій Canv_Main (Pause Menu)
    [SerializeField] private GameObject _hudCanvas;   // Сюди закинеш свій HUD (Canvas)

    private bool _isPaused = false;
    private GameObject _optionsMenuUI;

    private void Start()
    {
        // Шукаємо меню налаштувань, якщо воно є поруч із Canv_Main
        if (_pauseMenuUI != null && _pauseMenuUI.transform.parent != null)
        {
            Transform options = _pauseMenuUI.transform.parent.Find("Canv_Options");
            if (options != null)
                _optionsMenuUI = options.gameObject;
        }

        // Виправляємо баг зі Scale: Animator на дочірньому об'єкті MAIN постійно перезаписує RectTransform.localScale.
        // Ми його вимикаємо/видаляємо на старті, щоб масштаб був стабільним.
        if (_pauseMenuUI != null)
        {
            Transform mainTransform = _pauseMenuUI.transform.Find("MAIN");
            if (mainTransform != null)
            {
                Animator animator = mainTransform.GetComponent<Animator>();
                if (animator != null)
                {
                    Destroy(animator); // Видаляємо Animator, який конфліктує зі Scale
                    mainTransform.localScale = Vector3.one; // Скидаємо масштаб на стандартний
                }
            }
            
            // На старті гри меню має бути вимкнене
            _pauseMenuUI.SetActive(false);
        }

        if (_hudCanvas != null)
        {
            // Сам Canvas ми вмикаємо, бо там можуть бути інші важливі речі
            _hudCanvas.SetActive(true);
            // А от специфічні елементи HUD — за налаштуваннями
            bool showHud = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
            ToggleHudElements(showHud);
        }
    }

    private void Update()
    {
        // Ловимо натискання Escape (була помилка: стояла клавіша 'O')
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Якщо ми зараз у меню налаштувань — закриваємо його і повертаємось у головне меню паузи
            if (_optionsMenuUI != null && _optionsMenuUI.activeSelf)
            {
                _optionsMenuUI.SetActive(false);
                if (_pauseMenuUI != null)
                    _pauseMenuUI.SetActive(true);
            }
            // Інакше — стандартна логіка паузи
            else if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // Зробили public, щоб можна було повісити на кнопку "Продовжити"
    public void ResumeGame()
    {
        if (_pauseMenuUI != null)
            _pauseMenuUI.SetActive(false);
            
        if (_hudCanvas != null)
        {
            // Завжди повертаємо сам Canvas
            _hudCanvas.SetActive(true);
            // Але вмикаємо лише потрібні елементи HUD за налаштуваннями
            bool showHud = PlayerPrefs.GetInt("ShowHUD", 1) == 1;
            ToggleHudElements(showHud);
        }

        Time.timeScale = 1f; // Відновлюємо час
        _isPaused = false;

        // Ховаємо курсор і блокуємо його по центру (для 3D гри)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void PauseGame()
    {
        if (_pauseMenuUI != null)
            _pauseMenuUI.SetActive(true);
            
        if (_hudCanvas != null)
            _hudCanvas.SetActive(false); // Ховаємо весь HUD під час паузи, щоб не заважав

        Time.timeScale = 0f; // Зупиняємо час
        _isPaused = true;

        // Показуємо курсор, щоб можна було клікати по кнопках
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ToggleHudElements(bool isVisible)
    {
        if (_hudCanvas == null) return;

        // Шукаємо ці об'єкти незалежно від того, як ти їх назвав (великими чи маленькими літерами)
        string[] targetNames = new string[] 
        { 
            "survivalstats", 
            "hotbarpanel", 
            "staminabarbg", 
            "overloadhud", 
            "crosshairdot" 
        };

        Transform[] allTransforms = _hudCanvas.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            foreach (string target in targetNames)
            {
                if (t.name.ToLower() == target)
                {
                    t.gameObject.SetActive(isVisible);
                    break;
                }
            }
        }
    }
}