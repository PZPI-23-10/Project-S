using UnityEngine;

namespace Project_S.Runtime.Services.SceneManagement
{
    public class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _id;

        public string Id => _id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                _id = name;
        }
#endif
    }
}
