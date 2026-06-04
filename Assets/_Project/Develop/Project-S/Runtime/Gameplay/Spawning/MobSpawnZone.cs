using System;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Spawning
{
    public enum MobSpawnZoneShape
    {
        Sphere,
        Box
    }

    public class MobSpawnZone : MonoBehaviour
    {
        [SerializeField] private string _zoneId = "Spawn Zone";
        [SerializeField] private MobSpawnTable _spawnTable;
        [SerializeField] private MobSpawnZoneShape _shape = MobSpawnZoneShape.Sphere;
        [SerializeField] private float _radius = 20f;
        [SerializeField] private Vector3 _boxSize = new Vector3(30f, 8f, 30f);
        [SerializeField] private int _initialCount = 3;
        [SerializeField] private int _maxActive = 6;
        [SerializeField] private float _respawnInterval = 45f;
        [SerializeField] private float _spawnCheckInterval = 3f;
        [SerializeField] private float _minPlayerDistance = 18f;
        [SerializeField] private float _maxPlayerDistance = 90f;
        [SerializeField] private bool _avoidCameraView = true;
        [SerializeField] private float _cameraViewPadding = 0.08f;
        [SerializeField] private Transform _spawnParent;
        [SerializeField] private bool _logSpawnDiagnostics;

        private int _activeCount;
        private float _nextSpawnTime;
        private float _nextCheckTime;

        public string ZoneId => string.IsNullOrWhiteSpace(_zoneId) ? name : _zoneId;
        public MobSpawnTable SpawnTable => _spawnTable;
        public int InitialCount => Mathf.Max(0, _initialCount);
        public int MaxActive => Mathf.Max(0, _maxActive);
        public float RespawnInterval => Mathf.Max(0f, _respawnInterval);
        public float SpawnCheckInterval => Mathf.Max(0.05f, _spawnCheckInterval);
        public float MinPlayerDistance => Mathf.Max(0f, _minPlayerDistance);
        public float MaxPlayerDistance => Mathf.Max(MinPlayerDistance, _maxPlayerDistance);
        public bool AvoidCameraView => _avoidCameraView;
        public float CameraViewPadding => Mathf.Clamp01(_cameraViewPadding);
        public Transform SpawnParent => _spawnParent != null ? _spawnParent : transform;
        public bool LogSpawnDiagnostics => _logSpawnDiagnostics;
        public int ActiveCount => _activeCount;
        public Vector3 HomeCenter => transform.position;
        public float HomeRadius => _shape == MobSpawnZoneShape.Sphere ? Mathf.Max(0.5f, _radius) : Mathf.Max(0.5f, Mathf.Max(_boxSize.x, _boxSize.z) * 0.5f);

        public bool CanSpawnNow(float time)
        {
            if (_spawnTable == null || MaxActive <= 0 || _activeCount >= MaxActive)
                return false;

            if (time < _nextSpawnTime || time < _nextCheckTime)
                return false;

            _nextCheckTime = time + SpawnCheckInterval;
            return true;
        }

        public void MarkInitialSpawnWindow()
        {
            _nextSpawnTime = 0f;
            _nextCheckTime = 0f;
        }

        public void RegisterSpawn()
        {
            _activeCount++;
        }

        public void ReleaseSpawnSlot()
        {
            _activeCount = Mathf.Max(0, _activeCount - 1);
            _nextSpawnTime = Time.time + RespawnInterval;
        }

        public Vector3 RandomPoint()
        {
            if (_shape == MobSpawnZoneShape.Box)
            {
                Vector3 half = _boxSize * 0.5f;
                Vector3 local = new Vector3(
                    UnityEngine.Random.Range(-half.x, half.x),
                    UnityEngine.Random.Range(-half.y, half.y),
                    UnityEngine.Random.Range(-half.z, half.z));
                return transform.TransformPoint(local);
            }

            Vector2 circle = UnityEngine.Random.insideUnitCircle * Mathf.Max(0.5f, _radius);
            return transform.position + new Vector3(circle.x, 0f, circle.y);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.7f, 0.35f);
            if (_shape == MobSpawnZoneShape.Box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, _boxSize);
                Gizmos.DrawWireCube(Vector3.zero, _boxSize);
                Gizmos.matrix = Matrix4x4.identity;
                return;
            }

            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.5f, _radius));
        }
    }
}
