using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Services.SceneManagement
{
    public class SceneLoader
    {
        public void Load(string sceneName, Action onLoaded = null) =>
            LoadAsync(sceneName, LoadSceneMode.Single, onLoaded).Forget();

        public async UniTask LoadAsync(
            string sceneName,
            LoadSceneMode loadMode = LoadSceneMode.Single,
            Action onLoaded = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            if (loadMode == LoadSceneMode.Additive && IsLoaded(sceneName))
            {
                onLoaded?.Invoke();
                return;
            }

            if (loadMode == LoadSceneMode.Single && SceneManager.GetActiveScene().name == sceneName)
            {
                onLoaded?.Invoke();
                return;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadMode);
            if (operation == null)
                return;

            await operation.ToUniTask();

            onLoaded?.Invoke();
        }

        public async UniTask UnloadAsync(string sceneName, Action onUnloaded = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !IsLoaded(sceneName))
                return;

            AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);
            if (operation == null)
                return;

            await operation.ToUniTask();
            onUnloaded?.Invoke();
        }

        public bool IsLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return false;

            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }
    }
}
