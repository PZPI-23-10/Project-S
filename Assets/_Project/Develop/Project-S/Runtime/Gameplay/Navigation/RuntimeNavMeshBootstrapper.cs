using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Diagnostics;
using Project_S.Runtime.Gameplay.Enemies;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Navigation
{
    public static class RuntimeNavMeshBootstrapper
    {
        private const string RunnerName = "[MVP] Runtime NavMesh Bootstrapper";
        private const float DefaultBoundsSize = 600f;
        private const float BoundsPadding = 30f;
        private const float AgentRadius = 0.5f;
        private const float AgentHeight = 2f;
        private const float AgentSlope = 45f;
        private const float AgentClimb = 0.4f;
        private const int DefaultLayer = 0;
        private const int GroundLayer = 8;
        private const int WalkableLayerMask = 1 << GroundLayer;
        private const int ObstacleLayerMask = 1 << DefaultLayer;
        private const int NavigationLayerMask = WalkableLayerMask | ObstacleLayerMask;
        private const int BuildDelayFrames = 3;
        private static readonly int NotWalkableArea = NavMesh.GetAreaFromName("Not Walkable");


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (GameObject.Find(RunnerName) != null)
                return;

            var runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<RuntimeNavMeshRunner>();
        }

        private sealed class RuntimeNavMeshRunner : MonoBehaviour
        {
            private readonly List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
            private readonly List<NavMeshBuildMarkup> _markups = new List<NavMeshBuildMarkup>();
            private NavMeshData _navMeshData;
            private NavMeshDataInstance _navMeshDataInstance;
            private Coroutine _buildRoutine;
            private int _lastSourceSignature;
            private bool _hasBuiltNavMesh;

            private void OnEnable()
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            private void Start()
            {
                StartBuildRoutine();
            }

            private void OnDisable()
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                RemoveNavMeshData();
            }

            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                StartBuildRoutine();
            }

            private void StartBuildRoutine()
            {
                if (_buildRoutine != null)
                    StopCoroutine(_buildRoutine);

                _buildRoutine = StartCoroutine(BuildAfterSceneSettles());
            }

            private IEnumerator BuildAfterSceneSettles()
            {
                for (int frame = 0; frame < BuildDelayFrames; frame++)
                    yield return null;

                if (ShouldUseExistingNavMesh())
                {
                    Debug.Log("[NPC Startup] Runtime NavMesh build skipped; existing NavMesh data is available.");
                    _buildRoutine = null;
                    yield break;
                }

                int sourceSignature = NpcStartupDiagnostics.Time("Runtime NavMesh source signature", CalculateSourceSignature);
                if (_hasBuiltNavMesh && _navMeshDataInstance.valid && sourceSignature == _lastSourceSignature)
                {
                    Debug.Log("[NPC Startup] Runtime NavMesh build skipped; navigation sources are unchanged.");
                    _buildRoutine = null;
                    yield break;
                }

                yield return null;
                NpcStartupDiagnostics.Time("Runtime NavMesh build total", () => BuildNavMesh(sourceSignature));
                _buildRoutine = null;
            }

            private void BuildNavMesh(int sourceSignature)
            {
                RemoveNavMeshData();

                Bounds bounds = CalculateWorldBounds();
                var buildSettings = NavMesh.GetSettingsByID(0);
                buildSettings.agentRadius = AgentRadius;
                buildSettings.agentHeight = AgentHeight;
                buildSettings.agentSlope = AgentSlope;
                buildSettings.agentClimb = AgentClimb;
                buildSettings.ledgeDropHeight = 0f;
                buildSettings.maxJumpAcrossDistance = 0f;

                _sources.Clear();
                _markups.Clear();
                int obstacleCount = MarkStaticObstaclesAsNotWalkable();
                IgnoreDynamicRoot<PlayerFacade>();
                IgnoreDynamicRoot<GroundNavMeshMover>();
                IgnoreDynamicRoot<EnemyHealth>();
                NpcStartupDiagnostics.Time("Runtime NavMesh CollectSources", () =>
                    NavMeshBuilder.CollectSources(
                        bounds,
                        NavigationLayerMask,
                        NavMeshCollectGeometry.PhysicsColliders,
                        0,
                        _markups,
                        _sources));

                Debug.Log($"[NPC Startup] Runtime NavMesh sources collected: {_sources.Count}");
                Debug.Log($"[NPC Startup] Runtime NavMesh static obstacles marked: {obstacleCount}");

                _navMeshData = NpcStartupDiagnostics.Time("Runtime NavMesh BuildNavMeshData", () =>
                    NavMeshBuilder.BuildNavMeshData(buildSettings, _sources, bounds, Vector3.zero, Quaternion.identity));
                if (_navMeshData == null)
                {
                    Debug.LogWarning("[NavMesh] Runtime navmesh build returned no data.");
                    return;
                }

                _navMeshData.name = "Runtime Ground NavMesh";
                _navMeshDataInstance = NavMesh.AddNavMeshData(_navMeshData);
                _lastSourceSignature = sourceSignature;
                _hasBuiltNavMesh = true;
            }

            private void RemoveNavMeshData()
            {
                if (_navMeshDataInstance.valid)
                    _navMeshDataInstance.Remove();

                _navMeshDataInstance = default;

                if (_navMeshData != null)
                {
                    Destroy(_navMeshData);
                    _navMeshData = null;
                }
            }

            private static bool ShouldUseExistingNavMesh()
            {
#if UNITY_EDITOR
                if (HasStaticObstacleColliders())
                    return false;

                var triangulation = NavMesh.CalculateTriangulation();
                return triangulation.vertices != null && triangulation.vertices.Length > 0;
#else
                return false;
#endif
            }

            private static Bounds CalculateWorldBounds()
            {
                bool hasBounds = false;
                Bounds bounds = new Bounds(Vector3.zero, Vector3.one * DefaultBoundsSize);

                foreach (var terrain in Terrain.activeTerrains)
                {
                    if (terrain == null || terrain.terrainData == null)
                        continue;

                    var terrainBounds = new Bounds(
                        terrain.transform.position + terrain.terrainData.size * 0.5f,
                        terrain.terrainData.size);

                    if (!hasBounds)
                    {
                        bounds = terrainBounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(terrainBounds);
                    }
                }

                foreach (var collider in FindObjectsOfType<Collider>())
                {
                    if (!IsNavigationRelevantCollider(collider))
                        continue;

                    if (!hasBounds)
                    {
                        bounds = collider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }

                bounds.Expand(BoundsPadding);

                if (bounds.size.x < DefaultBoundsSize)
                    bounds.Expand(new Vector3(DefaultBoundsSize - bounds.size.x, 0f, 0f));

                if (bounds.size.z < DefaultBoundsSize)
                    bounds.Expand(new Vector3(0f, 0f, DefaultBoundsSize - bounds.size.z));

                return bounds;
            }

            private static int CalculateSourceSignature()
            {
                unchecked
                {
                    int hash = 17;
                    foreach (var terrain in Terrain.activeTerrains)
                    {
                        if (terrain == null || terrain.terrainData == null)
                            continue;

                        hash = hash * 31 + terrain.GetInstanceID();
                        hash = hash * 31 + terrain.terrainData.GetInstanceID();
                        hash = hash * 31 + terrain.gameObject.layer;
                        hash = hash * 31 + Quantize(terrain.transform.position.x);
                        hash = hash * 31 + Quantize(terrain.transform.position.z);
                    }

                    foreach (var collider in FindObjectsOfType<Collider>())
                    {
                        if (!IsNavigationRelevantCollider(collider))
                            continue;

                        Bounds bounds = collider.bounds;
                        hash = hash * 31 + collider.GetInstanceID();
                        hash = hash * 31 + collider.gameObject.layer;
                        hash = hash * 31 + Quantize(bounds.center.x);
                        hash = hash * 31 + Quantize(bounds.center.y);
                        hash = hash * 31 + Quantize(bounds.center.z);
                        hash = hash * 31 + Quantize(bounds.size.x);
                        hash = hash * 31 + Quantize(bounds.size.y);
                        hash = hash * 31 + Quantize(bounds.size.z);
                    }

                    return hash;
                }
            }

            private static bool IsNavigationCollider(Collider collider)
            {
                return IsNavigationRelevantCollider(collider);
            }

            private static bool IsNavigationRelevantCollider(Collider collider)
            {
                if (collider == null || !collider.enabled || collider.isTrigger)
                    return false;

                if (IsDynamicNavigationCollider(collider))
                    return false;

                return (NavigationLayerMask & (1 << collider.gameObject.layer)) != 0;
            }

            private static bool IsStaticObstacleCollider(Collider collider)
            {
                if (!IsNavigationRelevantCollider(collider))
                    return false;

                if ((ObstacleLayerMask & (1 << collider.gameObject.layer)) == 0)
                    return false;

                return !IsDynamicNavigationCollider(collider);
            }

            private static bool HasStaticObstacleColliders()
            {
                foreach (var collider in FindObjectsOfType<Collider>())
                {
                    if (IsStaticObstacleCollider(collider))
                        return true;
                }

                return false;
            }

            private static bool IsDynamicNavigationCollider(Collider collider)
            {
                return collider.GetComponentInParent<GroundNavMeshMover>() != null
                    || collider.GetComponentInParent<EnemyHealth>() != null
                    || collider.GetComponentInParent<PlayerFacade>() != null;
            }

            private int MarkStaticObstaclesAsNotWalkable()
            {
                int count = 0;
                foreach (var collider in FindObjectsOfType<Collider>())
                {
                    if (!IsStaticObstacleCollider(collider))
                        continue;

                    _markups.Add(new NavMeshBuildMarkup
                    {
                        root = collider.transform,
                        overrideArea = true,
                        area = NotWalkableArea
                    });
                    count++;
                }

                return count;
            }

            private static int Quantize(float value)
            {
                return Mathf.RoundToInt(value * 100f);
            }

            private void IgnoreDynamicRoot<T>() where T : Component
            {
                foreach (var component in FindObjectsOfType<T>())
                {
                    if (component == null)
                        continue;

                    _markups.Add(new NavMeshBuildMarkup
                    {
                        root = component.transform,
                        ignoreFromBuild = true
                    });
                }
            }
        }
    }
}
