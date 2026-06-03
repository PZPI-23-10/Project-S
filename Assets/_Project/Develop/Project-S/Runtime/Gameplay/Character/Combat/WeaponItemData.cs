using System;
using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Harvesting;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [Serializable]
    public struct DamageInstance
    {
        public DamageType Type;
        public float Amount;
    }

    [CreateAssetMenu(fileName = "New Weapon", menuName = "Project-S/Items/Weapon")]
    public class WeaponItemData : ItemData
    {
        [Header("Equip Settings")]
        public bool IsTwoHanded = false;

        [Header("Passive Abilities")]
        public List<WeaponPassiveData> Passives = new List<WeaponPassiveData>();

        [Header("Damage")]
        public List<DamageInstance> DamageProfile = new List<DamageInstance>();

        public float PoiseDamage = 20f;
        public float AttackSpeedMultiplier = 1f;

        [Header("Combo")]
        public int MaxComboHits = 3;

        [Header("Heavy ability")]
        public int HitsToChargeHeavy = 3;
        public WeaponActiveData HeavyAbilityData;
        public float HeavyAbilityDuration = 0f;

        [Header("Offhand ability")]
        public float AbilityCooldown = 8f;

        [Header("Defense")]
        [Range(0f, 1f)]
        public float BlockMitigation = 0.5f;
        public float ParryWindow = 0.25f;
        public float ParryStaminaReward = 20f;
        public float ParryPoiseDamage = 40f;

        [Header("Sounds")]
        public AudioClip SwingSound;
        public AudioClip HitSound;

        [Header("Harvesting")]
        public HarvestToolType HarvestTool = HarvestToolType.None;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Kind = ItemKind.Weapon;
            IsStackable = false;
            MaxStack = 1;
        }
#endif
    }
}