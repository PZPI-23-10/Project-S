using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Services.SceneManagement
{
    public class SceneLoader
    {
        private readonly HashSet<string> _loadingScenes = new HashSet<string>();

        public void Load(string sceneName, Action onLoaded = null) =>
            LoadAsync(sceneName, LoadSceneMode.Single, onLoaded).Forget();

        public async UniTask LoadAsync(
            string sceneName,
            LoadSceneMode loadMode = LoadSceneMode.Single,
            Action onLoaded = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            string loadingKey = $"{loadMode}:{sceneName}";
            if (_loadingScenes.Contains(loadingKey))
            {
                await UniTask.WaitUntil(() => IsLoaded(sceneName) || !_loadingScenes.Contains(loadingKey));
                onLoaded?.Invoke();
                return;
            }

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

            _loadingScenes.Add(loadingKey);
            try
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, loadMode);
                if (operation == null)
                    return;

                await operation.ToUniTask();
            }
            finally
            {
                _loadingScenes.Remove(loadingKey);
            }

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
