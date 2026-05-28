using System.Collections.Generic;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Project_S.Runtime.Gameplay.Diagnostics
{
    public static class NpcStartupDiagnostics
    {
        private static readonly Dictionary<string, Object> ResourceCache = new Dictionary<string, Object>();
        private static readonly Dictionary<string, Object[]> ResourceArrayCache = new Dictionary<string, Object[]>();

        public static void Time(string label, System.Action action)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            UnityEngine.Debug.Log($"[NPC Startup] {label}: {stopwatch.ElapsedMilliseconds} ms");
#else
            action();
#endif
        }

        public static T Time<T>(string label, System.Func<T> action)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var stopwatch = Stopwatch.StartNew();
            T result = action();
            stopwatch.Stop();
            UnityEngine.Debug.Log($"[NPC Startup] {label}: {stopwatch.ElapsedMilliseconds} ms");
            return result;
#else
            return action();
#endif
        }

        public static T LoadResource<T>(string owner, string path) where T : Object
        {
            string key = $"{typeof(T).FullName}:{path}";
            if (ResourceCache.TryGetValue(key, out Object cached) && cached != null)
                return (T)cached;

            T asset = Time($"{owner} Resources.Load<{typeof(T).Name}>({path})", () => Resources.Load<T>(path));
            if (asset != null)
                ResourceCache[key] = asset;

            return asset;
        }

        public static T[] LoadAllResources<T>(string owner, string path) where T : Object
        {
            string key = $"{typeof(T).FullName}:All:{path}";
            if (ResourceArrayCache.TryGetValue(key, out Object[] cached) && cached != null)
                return CastArray<T>(cached);

            T[] assets = Time($"{owner} Resources.LoadAll<{typeof(T).Name}>({path})", () => Resources.LoadAll<T>(path));
            ResourceArrayCache[key] = assets;
            return assets;
        }

        private static T[] CastArray<T>(Object[] source) where T : Object
        {
            var result = new T[source.Length];
            for (int index = 0; index < source.Length; index++)
                result[index] = (T)source[index];

            return result;
        }
    }
}
