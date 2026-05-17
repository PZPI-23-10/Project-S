using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    [Serializable]
    public class ResourceDrop
    {
        public ItemData Item;
        [Min(0)] public int MinAmount = 1;
        [Min(0)] public int MaxAmount = 1;
        [Range(0f, 1f)] public float Chance = 1f;

        public int RollAmount()
        {
            int min = Mathf.Max(0, MinAmount);
            int max = Mathf.Max(min, MaxAmount);
            return UnityEngine.Random.Range(min, max + 1);
        }
    }

    [CreateAssetMenu(fileName = "New Resource Node", menuName = "Project-S/Harvesting/Resource Node")]
    public class ResourceNodeData : ScriptableObject
    {
        public string DisplayName;
        [Min(1f)] public float MaxHealth = 25f;
        public HarvestToolType PreferredTool = HarvestToolType.None;
        [Min(0f)] public float MatchingToolDamageMultiplier = 1f;
        [Min(0f)] public float MismatchedToolDamageMultiplier = 0.25f;
        [Min(0)] public int SoulAshReward;
        public List<ResourceDrop> Drops = new List<ResourceDrop>();

        public string NodeName => string.IsNullOrWhiteSpace(DisplayName) ? name : DisplayName;
    }
}
