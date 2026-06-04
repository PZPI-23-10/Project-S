using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CreditsSceneGenerator
{
    [MenuItem("Tools/Створити Сцену Титрів")]
    static void GenerateCreditsScene()
    {
        // Попереджаємо користувача зберегти сцену
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string scenePath = "Assets/UI_Maksym/Credits.unity";

        // Create new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Camera
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.tag = "MainCamera";

        // 2. Canvas
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // 3. EventSystem
        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 4. Background Image
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = Color.black;
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 5. Title Text
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "PROJECT\nSURVIVAL";
        titleText.fontSize = 120;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1, 1, 1, 0); // Start transparent
        titleText.fontStyle = FontStyles.Bold;
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(1200, 400);

        // 6. Credits Container
        GameObject containerGo = new GameObject("CreditsContainer", typeof(RectTransform));
        containerGo.transform.SetParent(canvasGo.transform, false);
        RectTransform containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0); // Full width
        containerRect.anchorMax = new Vector2(1f, 0); // Full width
        containerRect.pivot = new Vector2(0.5f, 1);
        containerRect.anchoredPosition = new Vector2(0, 0);
        containerRect.sizeDelta = new Vector2(0, 4000);

        // 7. Roles Text (Ліва колонка)
        GameObject rolesTextGo = new GameObject("RolesText");
        rolesTextGo.transform.SetParent(containerGo.transform, false);
        TextMeshProUGUI rolesText = rolesTextGo.AddComponent<TextMeshProUGUI>();
        rolesText.text = @"<color=#a0a0a0>UI/UX Designer & Programmer</color>

<color=#a0a0a0>Level Designer</color>

<color=#a0a0a0>Gameplay Programmer</color>

<color=#a0a0a0>Game Designer, Sound Designer & 3D Artist</color>

<color=#a0a0a0>AI & Enemy Mechanics Programmer</color>";
        rolesText.fontSize = 40;
        rolesText.alignment = TextAlignmentOptions.TopRight;
        RectTransform rolesRect = rolesTextGo.GetComponent<RectTransform>();
        rolesRect.anchorMin = new Vector2(0, 0);
        rolesRect.anchorMax = new Vector2(0.5f, 1);
        rolesRect.offsetMin = new Vector2(0, 0);
        rolesRect.offsetMax = new Vector2(-40, 0); // padding від центру

        // 8. Names Text (Права колонка)
        GameObject namesTextGo = new GameObject("NamesText");
        namesTextGo.transform.SetParent(containerGo.transform, false);
        TextMeshProUGUI namesText = namesTextGo.AddComponent<TextMeshProUGUI>();
        namesText.text = @"Maksym Kovalenko

Andriy Nikulin

Hlib Zioma

Oleh Vdovenko

Oleksandr Sviderskyi";
        namesText.fontSize = 40;
        namesText.alignment = TextAlignmentOptions.TopLeft;
        RectTransform namesRect = namesTextGo.GetComponent<RectTransform>();
        namesRect.anchorMin = new Vector2(0.5f, 0);
        namesRect.anchorMax = new Vector2(1, 1);
        namesRect.offsetMin = new Vector2(40, 0); // padding від центру
        namesRect.offsetMax = new Vector2(0, 0);

        // 9. Credits Manager
        GameObject managerGo = new GameObject("CreditsManager");
        CreditsController controller = managerGo.AddComponent<CreditsController>();
        
        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("_titleText").objectReferenceValue = titleText;
        so.FindProperty("_creditsContainer").objectReferenceValue = containerRect;
        so.ApplyModifiedProperties();

        // Save Scene
        if (!System.IO.Directory.Exists("Assets/UI_Maksym"))
        {
            System.IO.Directory.CreateDirectory("Assets/UI_Maksym");
        }
        EditorSceneManager.SaveScene(newScene, scenePath);

        // Add to Build Settings
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        bool exists = false;
        foreach (var s in original)
        {
            if (s.path == scenePath)
            {
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            EditorBuildSettingsScene[] newSettings = new EditorBuildSettingsScene[original.Length + 1];
            System.Array.Copy(original, newSettings, original.Length);
            newSettings[newSettings.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newSettings;
        }

        Debug.Log("Credits Scene Generated successfully!");
    }
}
