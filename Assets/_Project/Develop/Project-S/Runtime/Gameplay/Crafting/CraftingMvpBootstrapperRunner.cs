using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CraftingMvpBootstrapperRunner : MonoBehaviour
    {
        private bool _bootstrapped;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            TryBootstrap();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrap();
        }

        private void TryBootstrap()
        {
            if (_bootstrapped)
                return;

            _bootstrapped = CraftingMvpBootstrapper.TryBootstrap();
        }
    }
}
