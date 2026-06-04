using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Spawning
{
    public class MobSpawnManager : MonoBehaviour
    {
        private const string RunnerName = "[Project-S] Mob Spawn Manager";
        private const int MaxSpawnPointAttempts = 24;
        private static readonly Collider[] OverlapBuffer = new Collider[16];

        private readonly List<MobSpawnZone> _zones = new List<MobSpawnZone>();
        private Transform _player;
        private bool _loggedMissingPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<MobSpawnManager>() != null)
                return;

            var runner = new GameObject(RunnerName);
            DontDestroyOnLoad(runner);
            runner.AddComponent<MobSpawnManager>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            RefreshSceneState();
            StartCoroutine(SpawnLoop());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshSceneState();
        }

        private IEnumerator SpawnLoop()
        {
            var wait = new WaitForSeconds(0.25f);
            while (enabled)
            {
                if (_player == null)
                    ResolvePlayer();

                if (_player != null)
                    TickZones();

                yield return wait;
            }
        }

        private void RefreshSceneState()
        {
            _zones.Clear();
            _zones.AddRange(FindObjectsByType<MobSpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));

            foreach (var zone in _zones)
                zone.MarkInitialSpawnWindow();

            ResolvePlayer();
            Debug.Log($"[MobSpawn] Found {_zones.Count} active spawn zone(s). Player found: {_player != null}.");
        }

        private void ResolvePlayer()
        {
            var playerFacade = FindFirstObjectByType<PlayerFacade>();
            _player = playerFacade != null ? playerFacade.transform : null;
            if (_player != null)
            {
                _loggedMissingPlayer = false;
                return;
            }

            if (!_loggedMissingPlayer)
            {
                Debug.LogWarning("[MobSpawn] PlayerFacade was not found. Spawn zones will wait until the player exists.");
                _loggedMissingPlayer = true;
            }
        }

        private void TickZones()
        {
            float time = Time.time;
            foreach (var zone in _zones)
            {
                if (zone == null || !zone.CanSpawnNow(time))
                    continue;

                int targetCount = zone.ActiveCount == 0 ? Mathf.Min(zone.InitialCount, zone.MaxActive) : 1;
                for (int index = 0; index < targetCount && zone.ActiveCount < zone.MaxActive; index++)
                    TrySpawnInZone(zone);
            }
        }

        private bool TrySpawnInZone(MobSpawnZone zone)
        {
            if (zone.SpawnTable == null || !zone.SpawnTable.TrySelect(out MobSpawnDefinition definition) || definition == null)
            {
                if (zone.LogSpawnDiagnostics)
                    Debug.LogWarning($"[MobSpawn] Zone '{zone.ZoneId}' has no spawn table entries.");

                return false;
            }

            if (!TryFindSpawnPoint(zone, definition, out Vector3 spawnPoint))
            {
                if (zone.LogSpawnDiagnostics)
                    Debug.LogWarning($"[MobSpawn] Zone '{zone.ZoneId}' could not find a valid spawn point for '{definition.DisplayName}'. Check NavMesh, player distance, camera visibility, and blockers.");

                return false;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject mob = definition.Spawn(spawnPoint, rotation, zone.SpawnParent, _player, zone.HomeCenter, zone.HomeRadius);
            if (mob == null)
            {
                if (zone.LogSpawnDiagnostics)
                    Debug.LogWarning($"[MobSpawn] Zone '{zone.ZoneId}' selected '{definition.DisplayName}', but its prefab could not be spawned.");

                return false;
            }

            var handle = mob.GetComponent<SpawnedMobHandle>();
            if (handle == null)
                handle = mob.AddComponent<SpawnedMobHandle>();

            zone.RegisterSpawn();
            handle.Configure(zone, definition);
            handle.SlotReleased += OnMobSlotReleased;
            if (zone.LogSpawnDiagnostics)
                Debug.Log($"[MobSpawn] Zone '{zone.ZoneId}' spawned '{definition.DisplayName}' at {spawnPoint}.");

            return true;
        }

        private void OnMobSlotReleased(SpawnedMobHandle handle)
        {
            if (handle == null)
                return;

            handle.SlotReleased -= OnMobSlotReleased;
            if (handle.Zone != null)
                handle.Zone.ReleaseSpawnSlot();
        }

        private bool TryFindSpawnPoint(MobSpawnZone zone, MobSpawnDefinition definition, out Vector3 spawnPoint)
        {
            spawnPoint = default;
            for (int attempt = 0; attempt < MaxSpawnPointAttempts; attempt++)
            {
                Vector3 candidate = GroundPositionSampler.SampleNavMeshNearGround(zone.RandomPoint(), zone.HomeRadius);
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(0.5f, zone.HomeRadius), NavMesh.AllAreas))
                    continue;

                candidate = hit.position;
                if (!PassesPlayerDistance(zone, candidate))
                    continue;

                if (zone.AvoidCameraView && IsVisibleToMainCamera(zone, candidate))
                    continue;

                if (IsBlocked(candidate, definition.SpawnClearanceRadius))
                    continue;

                spawnPoint = candidate;
                return true;
            }

            return false;
        }

        private bool PassesPlayerDistance(MobSpawnZone zone, Vector3 candidate)
        {
            if (_player == null)
                return true;

            Vector3 toPlayer = _player.position - candidate;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            return distance >= zone.MinPlayerDistance && distance <= zone.MaxPlayerDistance;
        }

        private static bool IsVisibleToMainCamera(MobSpawnZone zone, Vector3 candidate)
        {
            var camera = Camera.main;
            if (camera == null)
                return false;

            Vector3 viewportPoint = camera.WorldToViewportPoint(candidate + Vector3.up);
            float padding = zone.CameraViewPadding;
            return viewportPoint.z > 0f
                && viewportPoint.x >= -padding
                && viewportPoint.x <= 1f + padding
                && viewportPoint.y >= -padding
                && viewportPoint.y <= 1f + padding;
        }

        private static bool IsBlocked(Vector3 candidate, float radius)
        {
            float clearanceRadius = Mathf.Max(0.05f, radius);
            Vector3 center = candidate + Vector3.up * (clearanceRadius + 0.15f);
            int hitCount = Physics.OverlapSphereNonAlloc(center, clearanceRadius, OverlapBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < hitCount; index++)
            {
                var hit = OverlapBuffer[index];
                if (hit != null && !hit.isTrigger && !(hit is TerrainCollider))
                    return true;
            }

            return false;
        }
    }
}
