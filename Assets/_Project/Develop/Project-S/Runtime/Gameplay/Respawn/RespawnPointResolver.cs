using Project_S.Runtime.Common.Constants;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Respawn
{
    public static class RespawnPointResolver
    {
        public static bool TryFindNearest(Vector3 origin, out RespawnPoint respawnPoint)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (IsLevelScene(activeScene) && TryFindNearest(activeScene, origin, out respawnPoint, out _))
                return true;

            respawnPoint = null;
            float bestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!IsLevelScene(scene))
                    continue;

                if (TryFindNearest(scene, origin, out RespawnPoint candidate, out float sqrDistance)
                    && sqrDistance < bestSqrDistance)
                {
                    respawnPoint = candidate;
                    bestSqrDistance = sqrDistance;
                }
            }

            return respawnPoint != null;
        }

        public static bool TryFindNearest(Scene scene, Vector3 origin, out RespawnPoint respawnPoint)
        {
            return TryFindNearest(scene, origin, out respawnPoint, out _);
        }

        public static bool TryFindNewGameSpawn(Scene scene, out RespawnPoint respawnPoint)
        {
            respawnPoint = null;

            if (!IsLevelScene(scene))
                return false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RespawnPoint[] points = root.GetComponentsInChildren<RespawnPoint>(true);
                foreach (RespawnPoint point in points)
                {
                    if (point != null && point.IsAvailable && point.UseAsNewGameSpawn)
                    {
                        respawnPoint = point;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryFindNearest(Scene scene, Vector3 origin, out RespawnPoint respawnPoint, out float sqrDistance)
        {
            respawnPoint = null;
            sqrDistance = float.PositiveInfinity;

            if (!IsLevelScene(scene))
                return false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                RespawnPoint[] points = root.GetComponentsInChildren<RespawnPoint>(true);
                foreach (RespawnPoint point in points)
                {
                    if (point == null || !point.IsAvailable)
                        continue;

                    float candidateSqrDistance = (point.Position - origin).sqrMagnitude;
                    if (candidateSqrDistance >= sqrDistance)
                        continue;

                    respawnPoint = point;
                    sqrDistance = candidateSqrDistance;
                }
            }

            return respawnPoint != null;
        }

        private static bool IsLevelScene(Scene scene)
        {
            return scene.IsValid()
                && scene.isLoaded
                && !string.IsNullOrWhiteSpace(scene.name)
                && scene.name != SceneNames.Boot
                && scene.name != SceneNames.Core
                && scene.name != SceneNames.Menu;
        }
    }
}
