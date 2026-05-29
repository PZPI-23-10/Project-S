using System;
using Cysharp.Threading.Tasks;
using KinematicCharacterController;
using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Services.SceneManagement
{

                if (!string.IsNullOrWhiteSpace(previousLevelSceneName)
                    && previousLevelSceneName != levelSceneName)
                {
                    await _sceneLoader.UnloadAsync(previousLevelSceneName);
                }

                if (unloadBootScene && _sceneLoader.IsLoaded(SceneNames.Boot))
                    await _sceneLoader.UnloadAsync(SceneNames.Boot);

                _currentLevelSceneName = levelSceneName;
            }
            finally
            {
                SceneTransitionRequestBus.NotifyTransitionCompleted();
                _isTransitioning = false;
            }
        }

        private async UniTask EnsureCoreLoaded()
        {
            if (_sceneLoader.IsLoaded(SceneNames.Core))
                return;

            await _sceneLoader.LoadAsync(SceneNames.Core, LoadSceneMode.Additive);
        }

        private string ResolvePreviousLevelSceneName(string targetLevelSceneName)
        {
            if (IsLevelScene(_currentLevelSceneName))
                return _currentLevelSceneName;

            Scene activeScene = SceneManager.GetActiveScene();
            if (IsLevelScene(activeScene.name) && activeScene.name != targetLevelSceneName)
                return activeScene.name;

            return null;
        }

        private static bool IsLevelScene(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && sceneName != SceneNames.Boot
                && sceneName != SceneNames.Core
                && sceneName != SceneNames.Menu;
        }

        private void MovePlayerToSpawn(Scene targetScene, string spawnId)
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                return;

            SceneSpawnPoint spawnPoint = FindSpawnPoint(targetScene, spawnId);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"[SceneTransition] Spawn point '{spawnId}' was not found in scene '{targetScene.name}'.");
                return;
            }

            PlayerFacade player = _playerProvider != null && _playerProvider.Player != null
                ? _playerProvider.Player
                : UnityEngine.Object.FindFirstObjectByType<PlayerFacade>(FindObjectsInactive.Include);

            if (player == null)
            {
                Debug.LogWarning("[SceneTransition] Player was not found.");
                return;
            }

            KinematicCharacterMotor motor = player.GetComponent<KinematicCharacterMotor>();
            if (motor != null)
            {
                motor.BaseVelocity = Vector3.zero;
                motor.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
                return;
            }

            player.transform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        private static SceneSpawnPoint FindSpawnPoint(Scene scene, string spawnId)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SceneSpawnPoint[] spawnPoints = root.GetComponentsInChildren<SceneSpawnPoint>(true);
                foreach (SceneSpawnPoint spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null && spawnPoint.Id == spawnId)
                        return spawnPoint;
                }
            }

            return null;
        }
    }
}
