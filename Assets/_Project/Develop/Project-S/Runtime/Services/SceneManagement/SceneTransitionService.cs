using System;
using Cysharp.Threading.Tasks;
using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Respawn;
using Project_S.Runtime.Services.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Services.SceneManagement
{
    public class SceneTransitionState
    {
        public bool IsTransitioning { get; private set; }

        public bool TryBegin()
        {
            if (IsTransitioning)
                return false;

            IsTransitioning = true;
            return true;
        }

        public void End()
        {
            IsTransitioning = false;
        }
    }

    public class SceneTransitionService : IDisposable
    {
        private readonly SceneLoader _sceneLoader;
        private readonly PlayerProvider _playerProvider;
        private readonly GameSaveService _saveService;
        private readonly SceneTransitionState _transitionState;
        private bool _isTransitioning;
        private string _currentLevelSceneName;

        public SceneTransitionService(
            SceneLoader sceneLoader,
            PlayerProvider playerProvider,
            GameSaveService saveService,
            SceneTransitionState transitionState)
        {
            _sceneLoader = sceneLoader;
            _playerProvider = playerProvider;
            _saveService = saveService;
            _transitionState = transitionState;
            SceneTransitionRequestBus.TransitionRequested += TransitionTo;
        }

        public void LoadInitialLevel(string levelSceneName, string spawnId = null, bool useNewGameSpawn = false) =>
            TransitionToAsync(levelSceneName, spawnId, true, useNewGameSpawn).Forget();

        public void TransitionTo(string levelSceneName, string spawnId = null) =>
            TransitionToAsync(levelSceneName, spawnId, false).Forget();

        public void Dispose()
        {
            SceneTransitionRequestBus.TransitionRequested -= TransitionTo;
        }

        public async UniTask TransitionToAsync(
            string levelSceneName,
            string spawnId = null,
            bool unloadBootScene = false,
            bool useNewGameSpawn = false)
        {
            if (_isTransitioning || string.IsNullOrWhiteSpace(levelSceneName) || !_transitionState.TryBegin())
                return;

            PlayerFacade player = _playerProvider != null && _playerProvider.Player != null
                ? _playerProvider.Player
                : UnityEngine.Object.FindFirstObjectByType<PlayerFacade>(FindObjectsInactive.Include);

            Vector3 startPos = Vector3.zero;
            Quaternion startRot = Quaternion.identity;

            if (player != null)
            {
                startPos = player.transform.position;
                startRot = player.transform.rotation;
            }

            _isTransitioning = true;
            SceneTransitionRequestBus.NotifyTransitionStarted();

            try
            {
                await EnsureCoreLoaded();

                string previousLevelSceneName = ResolvePreviousLevelSceneName(levelSceneName);
                if (!string.IsNullOrWhiteSpace(previousLevelSceneName))
                    _saveService?.SaveNow("SceneTransitionBeforeUnload");

                if (!_sceneLoader.IsLoaded(levelSceneName))
                    await _sceneLoader.LoadAsync(levelSceneName, LoadSceneMode.Additive);

                Scene targetScene = SceneManager.GetSceneByName(levelSceneName);
                if (targetScene.IsValid() && targetScene.isLoaded)
                    SceneManager.SetActiveScene(targetScene);

                if (_saveService == null || !_saveService.ShouldRestorePlayerFromSave(levelSceneName))
                {
                    if (string.IsNullOrWhiteSpace(spawnId))
                    {
                        if (useNewGameSpawn)
                            MovePlayerToNewGameSpawn(targetScene);
                        else if (player != null)
                            PlayerRespawnUtility.MovePlayer(player, startPos, startRot);
                    }
                    else
                    {
                        MovePlayerToSpawn(targetScene, spawnId);
                    }
                }

                if (!string.IsNullOrWhiteSpace(previousLevelSceneName)
                    && previousLevelSceneName != levelSceneName)
                {
                    await _sceneLoader.UnloadAsync(previousLevelSceneName);
                }

                if (unloadBootScene && _sceneLoader.IsLoaded(SceneNames.Boot))
                    await _sceneLoader.UnloadAsync(SceneNames.Boot);

                _currentLevelSceneName = levelSceneName;
                _saveService?.ApplyAfterSceneLoaded(targetScene);
                _saveService?.RequestAutosave("SceneTransitionCompleted");
            }
            finally
            {
                SceneTransitionRequestBus.NotifyTransitionCompleted();
                _isTransitioning = false;
                _transitionState.End();
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
                && sceneName != SceneNames.Menu
                && sceneName != SceneNames.Credits;
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

            PlayerRespawnUtility.MovePlayer(player, spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        private void MovePlayerToNewGameSpawn(Scene targetScene)
        {
            if (!RespawnPointResolver.TryFindNewGameSpawn(targetScene, out RespawnPoint respawnPoint))
            {
                Debug.LogWarning($"[SceneTransition] New game spawn point was not found in scene '{targetScene.name}'.");
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

            PlayerRespawnUtility.MovePlayer(player, respawnPoint.Position, respawnPoint.Rotation);
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
