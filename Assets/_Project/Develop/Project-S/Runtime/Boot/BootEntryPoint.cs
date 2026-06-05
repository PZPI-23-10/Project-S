using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Services.SceneManagement;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Boot
{
    public class BootEntryPoint : MonoBehaviour
    {
        [Inject] private SceneLoader _sceneLoader;

        private void Awake()
        {
            _sceneLoader.Load(SceneNames.Menu);
        }
    }
}
