using System;

namespace Project_S.Runtime.Services.SceneManagement
{
    public static class SceneTransitionRequestBus
    {
        public static event Action<string, string> TransitionRequested;
        public static event Action TransitionStarted;
        public static event Action TransitionCompleted;

        public static void RequestTransition(string sceneName, string spawnId)
        {
            TransitionRequested?.Invoke(sceneName, spawnId);
        }

        internal static void NotifyTransitionStarted()
        {
            TransitionStarted?.Invoke();
        }

        internal static void NotifyTransitionCompleted()
        {
            TransitionCompleted?.Invoke();
        }
    }
}
