using Project_S.Runtime.Gameplay.Upgrades;
using Project_S.Runtime.Services.Save;
using UnityEditor;
using UnityEngine;

namespace Project_S.Editor
{
    public static class GameSaveEditorTools
    {
        [MenuItem("Project-S/Saves/Clear Game Save")]
        public static void ClearGameSave()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear Game Save",
                $"Delete saved game data?\n\nKeys:\n- {GameSaveService.MainSaveKey}\n- {UpgradeProgressStore.DefaultKey}",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            PlayerPrefs.DeleteKey(GameSaveService.MainSaveKey);
            PlayerPrefs.DeleteKey(UpgradeProgressStore.DefaultKey);
            PlayerPrefs.Save();

            Debug.Log($"[Save] Cleared '{GameSaveService.MainSaveKey}' and '{UpgradeProgressStore.DefaultKey}'.");
        }

        [MenuItem("Project-S/Saves/Clear Game Save", true)]
        private static bool ValidateClearGameSave()
        {
            return PlayerPrefs.HasKey(GameSaveService.MainSaveKey)
                || PlayerPrefs.HasKey(UpgradeProgressStore.DefaultKey);
        }
    }
}
