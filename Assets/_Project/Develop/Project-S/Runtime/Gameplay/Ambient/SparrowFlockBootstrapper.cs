using System.Collections;
using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Ambient
{
    public static class SparrowFlockBootstrapper
    {
        private const string RunnerName = "[MVP] Sparrow Flock Bootstrapper";
        private const string AmbientRootName = "[MVP] Ambient";
        private const string FlockRootName = "[MVP] Sparrow Flocks";
        private const string SparrowPrefabPath = "Ambient/Sparrow/Sparrow";

        private const int FlockCount = 2;
        private const int BirdsPerFlock = 5;
        private const float MinSpawnDistance = 18f;
        private const float MaxSpawnDistance = 35f;
        private const float FlockRadius = 6f;
        private const float GroundMoveSpeed = 0.8f;
        private const float FlyMoveSpeed = 5.5f;
        private const float ScareRadius = 5f;
        private const float MinReturnDelay = 6f;
        private const float MaxReturnDelay = 10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (GameObject.Find(RunnerName) != null)
                return;

            var runnerObject = new GameObject(RunnerName);
            Object.DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<SparrowFlockBootstrapRunner>();
        }

        private static bool TrySpawn()
        {
            if (GameObject.Find(FlockRootName) != null)
                return true;

            var player = Object.FindFirstObjectByType<PlayerFacade>();
            if (player == null)
                return false;

            var sparrowPrefab = Resources.Load<GameObject>(SparrowPrefabPath);
            if (sparrowPrefab == null)
            {
                Debug.LogWarning("[SparrowFlock] Sparrow prefab was not found in Resources.");
                return true;
            }

            var ambientRoot = GameObject.Find(AmbientRootName);
            if (ambientRoot == null)
                ambientRoot = new GameObject(AmbientRootName);

            var flockRoot = new GameObject(FlockRootName);
            flockRoot.transform.SetParent(ambientRoot.transform);

            for (int flockIndex = 0; flockIndex < FlockCount; flockIndex++)
            {
                Vector3 flockCenter = FindFlockCenter(player.transform, flockIndex);
                var flock = new GameObject($"Sparrow Flock {flockIndex + 1}");
                flock.transform.SetParent(flockRoot.transform);
                flock.transform.position = flockCenter;

                for (int birdIndex = 0; birdIndex < BirdsPerFlock; birdIndex++)
                    SpawnBird(sparrowPrefab, flock.transform, player.transform, flockCenter, birdIndex);
            }

            return true;
        }

        private static Vector3 FindFlockCenter(Transform player, int flockIndex)
        {
            float angle = (360f / FlockCount * flockIndex) + Random.Range(-35f, 35f);
            float distance = Random.Range(MinSpawnDistance, MaxSpawnDistance);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * player.forward;
            Vector3 position = player.position + direction.normalized * distance;
            return SparrowAmbientController.SampleGround(position);
        }

        private static void SpawnBird(
            GameObject sparrowPrefab,
            Transform parent,
            Transform player,
            Vector3 flockCenter,
            int birdIndex)
        {
            Vector3 spawnPosition = SparrowAmbientController.SampleGround(
                flockCenter + Random.insideUnitSphere.WithY(0f) * FlockRadius);

            var bird = Object.Instantiate(sparrowPrefab, spawnPosition, RandomYaw(), parent);
            bird.name = $"Sparrow {birdIndex + 1}";

            foreach (var collider in bird.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var animator = bird.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            var controller = bird.AddComponent<SparrowAmbientController>();
            controller.Configure(
                player,
                flockCenter,
                FlockRadius,
                GroundMoveSpeed,
                FlyMoveSpeed,
                ScareRadius,
                MinReturnDelay,
                MaxReturnDelay);
        }

        private static Quaternion RandomYaw()
        {
            return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        private sealed class SparrowFlockBootstrapRunner : MonoBehaviour
        {
            private Coroutine _spawnRoutine;

            private void OnEnable()
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            private void Start()
            {
                StartSpawnRoutine();
            }

            private void OnDisable()
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                StartSpawnRoutine();
            }

            private void StartSpawnRoutine()
            {
                if (_spawnRoutine != null)
                    StopCoroutine(_spawnRoutine);

                _spawnRoutine = StartCoroutine(SpawnWhenPlayerIsReady());
            }

            private IEnumerator SpawnWhenPlayerIsReady()
            {
                const int maxAttempts = 120;

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    if (TrySpawn())
                    {
                        _spawnRoutine = null;
                        yield break;
                    }

                    yield return null;
                }

                Debug.LogWarning("[SparrowFlock] Player was not found, sparrow flock spawn skipped.");
                _spawnRoutine = null;
            }
        }
    }
}
