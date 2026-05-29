using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
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

    [Serializable]
    public class ResourceDamageMultiplier
    {
        public DamageType Type;
        [Min(0f)] public float Multiplier = 1f;
    }

    [CreateAssetMenu(fileName = "New Resource Node", menuName = "Project-S/Harvesting/Resource Node")]
    public class ResourceNodeData : ScriptableObject
    {
        public string DisplayName;
        [Min(1f)] public float MaxHealth = 25f;

        [Header("Base Yield")]
        public ItemData BaseYieldItem;
        [Min(0f)] public float HealthPerBaseYield;
        [Min(1)] public int BaseYieldAmount = 1;

        [Header("Harvesting")]
        public HarvestToolType PreferredTool = HarvestToolType.None;
        [Min(0f)] public float MatchingToolDamageMultiplier = 1f;
        [Min(0f)] public float MismatchedToolDamageMultiplier = 1f;
        [Range(0f, 1f)] public float MismatchedYieldMultiplier = 0.9f;

        [Header("Audio & VFX")]
        public AudioClip[] CorrectToolSounds; 
        public AudioClip[] WrongToolSounds;  
        public GameObject HitVFXPrefab;      
        public AudioClip DestructionSound;    

        [Header("Resistances")]
        public List<ResourceDamageMultiplier> DamageMultipliers = new List<ResourceDamageMultiplier>();

        [Header("Completion Rewards")]
        [Min(0)] public int SoulAshReward;
        public List<ResourceDrop> Drops = new List<ResourceDrop>();

        public string NodeName => string.IsNullOrWhiteSpace(DisplayName) ? name : DisplayName;

        public float GetDamageMultiplier(DamageType damageType)
        {
            if (DamageMultipliers == null)
                return 1f;

            for (int i = 0; i < DamageMultipliers.Count; i++)
            {
                var entry = DamageMultipliers[i];
                if (entry != null && entry.Type == damageType)
                    return Mathf.Max(0f, entry.Multiplier);
            }

            return 1f;
        }
    }
}