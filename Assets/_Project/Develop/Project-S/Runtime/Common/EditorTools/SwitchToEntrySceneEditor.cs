using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Common.EditorTools
{
    [DefaultExecutionOrder(-32000)]
    public class SwitchToEntrySceneEditor : MonoBehaviour
    {
#if UNITY_EDITOR
        private const int EntrySceneIndex = 0;
        private static bool _bootLoaded;

        public static void Init() =>
            SceneManager.sceneLoaded += OnSceneLoaded;

        private static void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (_bootLoaded)
                return;

            _bootLoaded = scene.buildIndex == EntrySceneIndex;
        }

        private void Awake()
        {
            if (_bootLoaded)
                return;

            foreach (var root in gameObject.scene.GetRootGameObjects())
                root.SetActive(false);

            SceneManager.LoadScene(EntrySceneIndex);
        }
#endif
    }
}