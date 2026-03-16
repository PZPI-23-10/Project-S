using Project_S.Runtime.Common;
using UnityEngine;

namespace Project_S.Runtime.Services.Storage
{
    public abstract class BasePlayerStorage<T> : UnitySingleton<T> where T : MonoBehaviour
    {
        public DataStorage DataStorage { get; } = new DataStorage();

        protected override void OnAwake()
        {
            base.OnAwake();

            Init();
        }

        protected abstract void Init();

        public abstract void Reset();

        protected override void OnSceneLoaded(UnityEngine.SceneManagement.Scene arg0,
            UnityEngine.SceneManagement.LoadSceneMode arg1)
        {
            base.OnSceneLoaded(arg0, arg1);

            DataStorage.SaveData();
        }

        protected override bool OrderDontDestroyOnLoad => true;
    }
}