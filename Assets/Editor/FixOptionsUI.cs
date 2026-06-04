using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class FixOptionsUI : EditorWindow
{
    [MenuItem("Tools/Fix Options UI")]
    public static void FixUI()
    {
        bool changed = false;

        // Шукаємо Canv_Options
        GameObject optionsCanvas = null;
        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name == "Canv_Options" && !EditorUtility.IsPersistent(t))
            {
                optionsCanvas = t.gameObject;
                break;
            }
        }

        if (optionsCanvas != null)
        {
            // 1. Жорстко фіксуємо DimBackground
            Transform dimBg = optionsCanvas.transform.Find("DimBackground");
            if (dimBg != null)
            {
                RectTransform rt = dimBg.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = Vector3.one;
                    
                    // Відключаємо будь-які Layout компоненти, якщо вони є
                    var layout = dimBg.GetComponent<LayoutElement>();
                    if (layout != null) DestroyImmediate(layout);

                    Debug.Log("DimBackground у Canv_Options розтягнуто на весь екран.");
                    changed = true;
                }
            }

            // Також перевіряємо панель гри (GAME), чи немає там зайвого фону, який виглядає як чорний квадрат
            Transform gamePanel = optionsCanvas.transform.Find("SETTINGS/DETAILS/PANELS/GAME");
            if (gamePanel != null)
            {
                Image bgImage = gamePanel.GetComponent<Image>();
                if (bgImage != null)
                {
                    // Робимо фон панелі повністю прозорим, щоб він не заважав
                    Color c = bgImage.color;
                    c.a = 0;
                    bgImage.color = c;
                    Debug.Log("Фон панелі GAME зроблено прозорим.");
                    changed = true;
                }
            }
            
            // Якщо є ще якісь DimBackground у сцені (наприклад, у Canv_Main)
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "DimBackground" && !EditorUtility.IsPersistent(t))
                {
                    RectTransform rt = t.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                        rt.sizeDelta = Vector2.zero;
                        rt.anchoredPosition = Vector2.zero;
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(optionsCanvas);
                EditorSceneManager.MarkSceneDirty(optionsCanvas.scene);
                Debug.Log("Зміни застосовано! Не забудь зберегти сцену (Ctrl+S).");
            }
        }
        else
        {
            Debug.LogWarning("Canv_Options не знайдено у сцені!");
        }
    }
}
