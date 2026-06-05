using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Portals
{
    public class PortalCompletionManager : MonoBehaviour
    {
        private const string RunnerName = "[Project-S] Portal Completion Manager";

        private readonly List<BossPortal> _portals = new List<BossPortal>();
        private bool _allClosedReported;

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
            _allClosedReported = false;

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
            if (_allClosedReported || _portals.Count == 0)
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
            Debug.Log("[Portals] All portals are closed. Game ending hook is ready.");
            AllPortalsClosed?.Invoke();
        }
    }
}
