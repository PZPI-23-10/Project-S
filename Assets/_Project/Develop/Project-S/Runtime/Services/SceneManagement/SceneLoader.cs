using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Services.SceneManagement
{
    public class SceneLoader
    {
        public void Load(string sceneName, Action onLoaded = null) =>
            LoadScene(sceneName, onLoaded).Forget();

        private async UniTask LoadScene(string sceneName, Action onLoaded = null)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                onLoaded?.Invoke();
                return;
            }

            await SceneManager.LoadSceneAsync(sceneName).ToUniTask();

            onLoaded?.Invoke();
        }
    }
}