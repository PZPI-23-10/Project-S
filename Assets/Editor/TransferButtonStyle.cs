using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[InitializeOnLoad]
public class TransferButtonStyle
{
    static TransferButtonStyle()
    {
        EditorApplication.delayCall += RunTransfer;
    }

    static void RunTransfer()
    {
        if (EditorPrefs.GetBool("StyleCopierRun3", false)) return;
        EditorPrefs.SetBool("StyleCopierRun3", true);

        GameObject deathScreen = FindInactiveObjectByName("DeathScreen");
        GameObject pauseMenu = FindInactiveObjectByName("Canv_Main");

        if (deathScreen == null)
        {
            Debug.LogError("DeathScreen not found!");
            return;
        }
        if (pauseMenu == null)
        {
            Debug.LogError("Pause Menu (Canv_Main) not found!");
            return;
        }

        Button sourceButton = pauseMenu.GetComponentInChildren<Button>(true);
        if (sourceButton == null)
        {
            Debug.LogError("No button found in Pause Menu!");
            return;
        }

        // Find buttons inside DeathScreen
        Button[] deathButtons = deathScreen.GetComponentsInChildren<Button>(true);
        foreach (Button btn in deathButtons)
        {
            string oldName = btn.gameObject.name;
            string newText = "BUTTON";
            if (oldName.ToLower().Contains("respawn") || oldName.ToLower().Contains("відрод")) newText = "ВІДРОДИТИСЯ";
            if (oldName.ToLower().Contains("menu") || oldName.ToLower().Contains("меню")) newText = "ГОЛОВНЕ МЕНЮ";

            ReplaceButton(btn.gameObject, sourceButton.gameObject, newText);
        }

        Debug.Log("Button styles transferred successfully!");
    }

    static GameObject FindInactiveObjectByName(string name)
    {
        Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in objs)
        {
            if (t.hideFlags == HideFlags.None && t.gameObject.name == name)
                return t.gameObject;
        }
        return null;
    }

    static void ReplaceButton(GameObject oldBtnGo, GameObject sourceBtnGo, string newTextStr)
    {
        Button oldBtn = oldBtnGo.GetComponent<Button>();
        RectTransform oldRect = oldBtnGo.GetComponent<RectTransform>();

        // Create duplicate of source button
        GameObject newBtnGo = Object.Instantiate(sourceBtnGo);
        newBtnGo.name = oldBtnGo.name;
        
        // Setup transform
        newBtnGo.transform.SetParent(oldBtnGo.transform.parent, false);
        RectTransform newRect = newBtnGo.GetComponent<RectTransform>();
        newRect.anchorMin = oldRect.anchorMin;
        newRect.anchorMax = oldRect.anchorMax;
        newRect.anchoredPosition = oldRect.anchoredPosition;
        newRect.sizeDelta = oldRect.sizeDelta;
        newRect.pivot = oldRect.pivot;

        // Set text
        UnityEngine.UI.Text newText = newBtnGo.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (newText != null) newText.text = newTextStr;

        // Copy onClick event
        Button newBtn = newBtnGo.GetComponent<Button>();
        if (oldBtn != null && newBtn != null)
        {
            newBtn.onClick = oldBtn.onClick;
        }
        
        Object.DestroyImmediate(oldBtnGo);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
