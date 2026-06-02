using System;
using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Stats;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public enum ItemKind
    {
        Resource,
        Weapon,
        Consumable,
        Tool,
        Accessory,
        Material
    }

    public enum ConsumableEffectType
    {
        None,
        RestoreHealth,
        RestoreHunger
    }

    public enum ConsumableSpecialEffectType
    {
        None,
        HomeTeleport
    }

    [Serializable]
    public class ConsumableStatEffect
    {
        public StatType StatType = StatType.Health;
        public float Amount;
    }

    [Serializable]
    public class ConsumableTimedBuffEffect
    {
        public TimedBuffType Type = TimedBuffType.None;
        public TimedBuffCategory Category = TimedBuffCategory.None;
        public float Multiplier = 1f;
        [Min(0f)] public float DurationSeconds;
    }

    [Serializable]
    public class ConsumableSpecialEffect
    {
        public ConsumableSpecialEffectType Type = ConsumableSpecialEffectType.None;
        [Min(0f)] public float DelaySeconds;
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Project-S/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Base")]
        public string ItemName = "Предмет";
        public ItemKind Kind = ItemKind.Resource;
        public float Weight = 1.0f;

        [Header("Stacks")]
        public bool IsStackable = false;
        public int MaxStack = 1;
        [Tooltip("0 means unlimited inventory stacks.")]
        public int MaxInventoryStacks;

        [Header("UI")]
        public Sprite Icon;
        [TextArea(3, 5)] public string Description = "Опис предмета.";

        [Header("World")]
        public GameObject WeaponPrefab;
        public GameObject WorldPickupPrefab;

        [Header("Consumable")]
        public ConsumableEffectType ConsumableEffect;
        public float HealthRestoreAmount;
        public float HungerRestoreAmount;
        public float StaminaRestoreAmount;
        public ConsumableSpecialEffectType SpecialEffect;
        public float SpecialEffectDelaySeconds;

        [Header("Sound consumable")]
        public AudioClip ConsumeSound;

        [Header("Visual Grease(-)")]
        public GameObject WeaponCoatingVFX;
        public AudioClip CoatingSwingSound;
        public AudioClip CoatingHitSound;

        [Header("Consumable Effects")]
        public List<ConsumableStatEffect> StatEffects = new List<ConsumableStatEffect>();
        public List<ConsumableTimedBuffEffect> TimedBuffs = new List<ConsumableTimedBuffEffect>();
        public List<ConsumableSpecialEffect> SpecialEffects = new List<ConsumableSpecialEffect>();
        public List<DamageConversionEffect> DamageConversions = new List<DamageConversionEffect>();

        [Header("Timed Buff")]
        public TimedBuffType TimedBuffType = TimedBuffType.None;
        public TimedBuffCategory TimedBuffCategory = TimedBuffCategory.None;
        public float TimedBuffMultiplier = 1f;
        public float TimedBuffDurationSeconds;
        public TimedBuffType SecondaryTimedBuffType = TimedBuffType.None;
        public TimedBuffCategory SecondaryTimedBuffCategory = TimedBuffCategory.None;
        public float SecondaryTimedBuffMultiplier = 1f;
        public float SecondaryTimedBuffDurationSeconds;

        public bool IsUsable =>
            ConsumableEffect != ConsumableEffectType.None
            || !Mathf.Approximately(HealthRestoreAmount, 0f)
            || !Mathf.Approximately(HungerRestoreAmount, 0f)
            || !Mathf.Approximately(StaminaRestoreAmount, 0f)
            || TimedBuffType != TimedBuffType.None
            || SecondaryTimedBuffType != TimedBuffType.None
            || SpecialEffect != ConsumableSpecialEffectType.None
            || (StatEffects != null && StatEffects.Count > 0)
            || (TimedBuffs != null && TimedBuffs.Count > 0)
            || (SpecialEffects != null && SpecialEffects.Count > 0)
            || (DamageConversions != null && DamageConversions.Count > 0);
    }
}
