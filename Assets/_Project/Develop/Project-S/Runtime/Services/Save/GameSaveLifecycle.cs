using UnityEngine;

namespace Project_S.Runtime.Services.Save
{
    internal class GameSaveLifecycle : MonoBehaviour
    {
        private GameSaveService _service;

        public void Initialize(GameSaveService service)
        {
            _service = service;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                _service?.SaveNow("ApplicationPause");
        }

        private void OnApplicationQuit()
        {
            _service?.SaveNow("ApplicationQuit");
        }
    }
}
