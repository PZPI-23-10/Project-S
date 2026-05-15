using System;
using System.Collections.Generic;
using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Inventory;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    [Serializable]
    public struct DamageInstance
    {
        public DamageType Type;
        public float Amount;
    }

    public enum HeavyAbilityType { None, PrecisionAim, Fury, DirectionalSlice, AoEKnockback, SingleStrong, ChainForm }
    public enum OffhandAbilityType { None, DamageParry, StunStrike, DoubleSlice, ShieldBash }

    [CreateAssetMenu(fileName = "New Weapon", menuName = "Project-S/Items/Weapon")]
    public class WeaponItemData : ItemData // Твій базовий клас предмета
    {
        [Header("Урон зброї (Можна комбінувати!)")]
        public List<DamageInstance> DamageProfile = new List<DamageInstance>();

        public float PoiseDamage = 20f;
        public float AttackSpeedMultiplier = 1f;

        [Header("Легкі удари (Комбо ЛКМ)")]
        public int MaxComboHits = 3;

        [Header("Важкий удар (Здібність ЛКМ+ПКМ)")]
        public int HitsToChargeHeavy = 3;
        public HeavyAbilityType HeavyAbility;
        public float HeavyAbilityDuration = 0f;

        [Header("Здібність другої руки (Кнопка F)")]
        public OffhandAbilityType OffhandAbility;
        public float AbilityCooldown = 8f;

        [Header("Блок та Парирування")]
        [Range(0f, 1f)]
        public float BlockMitigation = 0.5f;
        public float ParryWindow = 0.25f;
        public float ParryStaminaReward = 20f;
        public float ParryPoiseDamage = 40f;
    }
}