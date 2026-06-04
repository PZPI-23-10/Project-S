using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Upgrades
{
    public enum UpgradeEffectType
    {
        StatAdd,
        UnlockOffhand,
        DodgeInvulnerability
    }

    [Serializable]
    public class UpgradeItemCost
    {
        public ItemData Item;
        [Min(1)] public int Amount = 1;
    }

    [Serializable]
    public class UpgradeEffect
    {
        public UpgradeEffectType Type;
        public StatType StatType;
        public float Amount;
        public bool ExpandStatLimit;
    }

    [CreateAssetMenu(fileName = "New Upgrade", menuName = "Project-S/Upgrades/Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;
        public Vector2 TreePosition;
        public List<string> PrerequisiteIds = new List<string>();
        public int SoulAshCost;
        public List<UpgradeItemCost> ItemCosts = new List<UpgradeItemCost>();
        public List<UpgradeEffect> Effects = new List<UpgradeEffect>();
        [TextArea(3, 7)] public string Description;

        public string Title => string.IsNullOrWhiteSpace(DisplayName) ? name : DisplayName;
    }
}
