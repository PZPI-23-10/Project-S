using System.Collections;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Player;
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
                yield return null;
                BuildNavMesh();
                _buildRoutine = null;
            }

            private void BuildNavMesh()
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
                IgnoreDynamicRoot<PlayerFacade>();
                NavMeshBuilder.CollectSources(
                    bounds,
                    ~0,
                    NavMeshCollectGeometry.PhysicsColliders,
                    0,
                    _markups,
                    _sources);

                _navMeshData = NavMeshBuilder.BuildNavMeshData(buildSettings, _sources, bounds, Vector3.zero, Quaternion.identity);
                if (_navMeshData == null)
                {
                    Debug.LogWarning("[NavMesh] Runtime navmesh build returned no data.");
                    return;
                }

                _navMeshData.name = "Runtime Ground NavMesh";
                _navMeshDataInstance = NavMesh.AddNavMeshData(_navMeshData);
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
                    if (collider == null || !collider.enabled || collider.isTrigger)
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
