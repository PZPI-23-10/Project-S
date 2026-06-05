using System;
using System.Collections.Generic;
using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Services.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Project_S.Runtime.Gameplay.Portals
{
    public class PortalCompletionManager : MonoBehaviour
    {
        private const string RunnerName = "[Project-S] Portal Completion Manager";

        private readonly List<BossPortal> _portals = new List<BossPortal>();
        [SerializeField] private bool _loadCreditsOnCompletion = true;
        private bool _allClosedReported;
        private bool _endingStarted;

        public static event Action AllPortalsClosed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PortalCompletionManager>() != null)
                return;

            var runner = new GameObject(RunnerName);
            DontDestroyOnLoad(runner);
            runner.AddComponent<PortalCompletionManager>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void Start()
        {
            RefreshSceneState();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            UnsubscribePortals();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshSceneState();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            RefreshSceneState();
        }

        private void RefreshSceneState()
        {
            UnsubscribePortals();
            _portals.Clear();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                    _portals.AddRange(root.GetComponentsInChildren<BossPortal>(true));
            }

            for (int i = 0; i < _portals.Count; i++)
            {
                BossPortal portal = _portals[i];
                if (portal != null)
                    portal.Changed += OnPortalChanged;
            }

            if (HasOpenPortal())
            {
                _allClosedReported = false;
                _endingStarted = false;
            }
            else if (!_endingStarted)
            {
                _allClosedReported = false;
            }

            CheckCompletion();
        }

        private void UnsubscribePortals()
        {
            for (int i = 0; i < _portals.Count; i++)
            {
                BossPortal portal = _portals[i];
                if (portal != null)
                    portal.Changed -= OnPortalChanged;
            }
        }

        private void OnPortalChanged(BossPortal portal)
        {
            CheckCompletion();
        }

        private void CheckCompletion()
        {
            if (_endingStarted || _allClosedReported || _portals.Count == 0)
                return;

            int activePortalCount = 0;
            for (int i = 0; i < _portals.Count; i++)
            {
                BossPortal portal = _portals[i];
                if (portal == null)
                    continue;

                activePortalCount++;
                if (!portal.IsClosed)
                    return;
            }

            if (activePortalCount == 0)
                return;

            _allClosedReported = true;
            StartEnding();
        }

        private bool HasOpenPortal()
        {
            for (int i = 0; i < _portals.Count; i++)
            {
                BossPortal portal = _portals[i];
                if (portal != null && !portal.IsClosed)
                    return true;
            }

            return false;
        }

        private void StartEnding()
        {
            if (_endingStarted)
                return;

            _endingStarted = true;
            SaveFinalState();
            Debug.Log("[Portals] All portals are closed. Loading credits.");
            AllPortalsClosed?.Invoke();

            if (_loadCreditsOnCompletion)
                SceneManager.LoadScene(SceneNames.Credits);
        }

        private static void SaveFinalState()
        {
            if (!ProjectContext.HasInstance)
                return;

            GameSaveService saveService = ProjectContext.Instance.Container.TryResolve<GameSaveService>();
            saveService?.SaveNow("AllPortalsClosed");
        }
    }
}
