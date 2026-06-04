using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Spawning
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class BossSpawnTrigger : MonoBehaviour
    {
        [SerializeField] private GameObject _bossPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private bool _useSpawnPointRotation = true;
        [SerializeField] private bool _despawnOnPlayerExit = true;
        [SerializeField] private bool _logDiagnostics;

        private readonly HashSet<Collider> _playerCollidersInside = new HashSet<Collider>();
        private readonly List<Collider> _playerColliderBuffer = new List<Collider>(16);
        private GameObject _currentBoss;
        private Collider _triggerCollider;
        private PlayerFacade _player;
        private bool _playerInsideByPolling;
        private float _nextPresenceCheckTime;

        public GameObject CurrentBoss => _currentBoss;
        public bool HasBossSpawned => _currentBoss != null;

        private void Awake()
        {
            EnsureTriggerCollider();
        }

        private void Update()
        {
            if (Time.time < _nextPresenceCheckTime)
                return;

            _nextPresenceCheckTime = Time.time + 0.1f;
            TickPlayerPresence();
        }

        private void Reset()
        {
            EnsureTriggerCollider();
        }

        private void OnValidate()
        {
            EnsureTriggerCollider();
        }

        private void OnDisable()
        {
            _playerCollidersInside.Clear();
            _playerInsideByPolling = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            _playerCollidersInside.Add(other);
            _playerInsideByPolling = true;
            SpawnBossIfNeeded();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayerCollider(other))
                return;

            _playerCollidersInside.Remove(other);
            if (_despawnOnPlayerExit && !_playerInsideByPolling && _playerCollidersInside.Count == 0)
                DespawnBoss();
        }

        public void SpawnBossIfNeeded()
        {
            if (_currentBoss != null)
                return;

            if (_bossPrefab == null)
            {
                if (_logDiagnostics)
                    Debug.LogWarning($"[BossSpawn] '{name}' cannot spawn because no boss prefab is assigned.", this);

                return;
            }

            Transform point = _spawnPoint != null ? _spawnPoint : transform;
            Quaternion rotation = _useSpawnPointRotation ? point.rotation : Quaternion.identity;
            _currentBoss = Instantiate(_bossPrefab, point.position, rotation, _spawnParent);
            _currentBoss.name = $"{_bossPrefab.name} (Spawned)";

            if (_logDiagnostics)
                Debug.Log($"[BossSpawn] '{name}' spawned '{_currentBoss.name}' at {point.position}.", this);
        }

        public void DespawnBoss()
        {
            if (_currentBoss == null)
                return;

            GameObject boss = _currentBoss;
            _currentBoss = null;

            if (Application.isPlaying)
                Destroy(boss);
            else
                DestroyImmediate(boss);

            if (_logDiagnostics)
                Debug.Log($"[BossSpawn] '{name}' despawned boss.", this);
        }

        private bool IsPlayerCollider(Collider other)
        {
            return other != null && other.GetComponentInParent<PlayerFacade>() != null;
        }

        private void TickPlayerPresence()
        {
            if (_triggerCollider == null)
                EnsureTriggerCollider();

            if (_triggerCollider == null)
                return;

            if (_player == null)
                _player = FindFirstObjectByType<PlayerFacade>();

            bool wasInside = _playerInsideByPolling || _playerCollidersInside.Count > 0;
            bool isInside = _player != null && IsPlayerInside(_player);
            _playerInsideByPolling = isInside;

            if (isInside)
            {
                SpawnBossIfNeeded();
                return;
            }

            _playerCollidersInside.Clear();
            if (_despawnOnPlayerExit && wasInside)
                DespawnBoss();
        }

        private bool IsPlayerInside(PlayerFacade player)
        {
            player.GetComponentsInChildren(false, _playerColliderBuffer);
            for (int index = 0; index < _playerColliderBuffer.Count; index++)
            {
                Collider playerCollider = _playerColliderBuffer[index];
                if (playerCollider == null || !playerCollider.enabled)
                    continue;

                if (CollidersOverlap(_triggerCollider, playerCollider))
                    return true;
            }

            return _triggerCollider.bounds.Contains(player.transform.position);
        }

        private static bool CollidersOverlap(Collider triggerCollider, Collider playerCollider)
        {
            return Physics.ComputePenetration(
                triggerCollider,
                triggerCollider.transform.position,
                triggerCollider.transform.rotation,
                playerCollider,
                playerCollider.transform.position,
                playerCollider.transform.rotation,
                out _,
                out _);
        }

        private void EnsureTriggerCollider()
        {
            _triggerCollider = GetComponent<Collider>();
            if (_triggerCollider != null)
                _triggerCollider.isTrigger = true;
        }
    }
}
