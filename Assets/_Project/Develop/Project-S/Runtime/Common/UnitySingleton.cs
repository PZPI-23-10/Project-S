using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Common
{
    public abstract class UnitySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static void DisableSingleton()
        {
            _instance = null;
        }

        private void Awake()
        {
            if (_instance != null)
            {
                if (_instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                _instance = (T)((System.Object)this);
            }

            if (OrderDontDestroyOnLoad)
            {
                if (gameObject.transform.parent != null)
                {
                    gameObject.transform.SetParent(null);
                }

                DontDestroyOnLoad(gameObject);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            OnAwake();
        }

        protected virtual void OnAwake() { }

        protected virtual bool OrderDontDestroyOnLoad => false;

        protected virtual void OnSceneLoaded(Scene arg0, LoadSceneMode arg1) { }

        protected static T _instance;

        public static bool HasInstance => _instance;

        public static T GetInstance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (_instance == null)
                    {
                        Debug.LogError("Not found instance of SINGLETON object!!!");
                    }
                }

                return _instance;
            }
        }

        public static T GetOrCreateInstance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject();
                    _instance = go.AddComponent<T>();
                    _instance.name = "(singleton) " + typeof(T).ToString();
                }

                return _instance;
            }
        }
    }
}