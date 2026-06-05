using UnityEngine;

namespace Project_S.Runtime.Gameplay.Respawn
{
    public class RespawnPoint : MonoBehaviour
    {
        [SerializeField] private string _id;
        [Tooltip("Exact transform where the player appears. If empty, this object's transform is used.")]
        [SerializeField] private Transform _spawnTransform;
        [SerializeField] private bool _isAvailable = true;
        [SerializeField] private bool _useAsNewGameSpawn;

        public string Id => _id;
        public Transform SpawnTransform => _spawnTransform != null ? _spawnTransform : transform;
        public Vector3 Position => SpawnTransform.position;
        public Quaternion Rotation => SpawnTransform.rotation;
        public bool IsAvailable => _isAvailable && isActiveAndEnabled && gameObject.activeInHierarchy;
        public bool UseAsNewGameSpawn => _useAsNewGameSpawn;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                _id = name;
        }

        private void OnDrawGizmosSelected()
        {
            Transform spawn = SpawnTransform;
            Gizmos.color = _useAsNewGameSpawn ? Color.cyan : Color.green;
            Gizmos.DrawWireSphere(spawn.position, 0.35f);
            Gizmos.DrawLine(spawn.position, spawn.position + spawn.forward);
        }
#endif
    }
}
