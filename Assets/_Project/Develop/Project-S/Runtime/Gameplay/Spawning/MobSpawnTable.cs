using System;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Spawning
{
    [CreateAssetMenu(fileName = "New Mob Spawn Table", menuName = "Project-S/Spawning/Mob Spawn Table")]
    public class MobSpawnTable : ScriptableObject
    {
        [SerializeField] private MobSpawnEntry[] _entries = Array.Empty<MobSpawnEntry>();

#if UNITY_EDITOR
        public void ConfigureEntries(params MobSpawnEntry[] entries)
        {
            _entries = entries ?? Array.Empty<MobSpawnEntry>();
        }
#endif

        public bool TrySelect(out MobSpawnDefinition definition)
        {
            definition = null;

            float totalWeight = 0f;
            foreach (var entry in _entries)
            {
                if (entry.Definition != null)
                    totalWeight += Mathf.Max(0f, entry.Weight);
            }

            if (totalWeight <= 0f)
                return false;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            foreach (var entry in _entries)
            {
                if (entry.Definition == null)
                    continue;

                roll -= Mathf.Max(0f, entry.Weight);
                if (roll > 0f)
                    continue;

                definition = entry.Definition;
                return true;
            }

            return false;
        }
    }

    [Serializable]
    public struct MobSpawnEntry
    {
        public MobSpawnDefinition Definition;
        [Min(0f)] public float Weight;
    }
}
