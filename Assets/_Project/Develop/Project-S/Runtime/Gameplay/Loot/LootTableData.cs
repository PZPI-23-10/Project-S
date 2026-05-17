using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Loot
{
    public interface ILootRandom
    {
        float Value();
        int RangeInclusive(int minInclusive, int maxInclusive);
    }

    public sealed class UnityLootRandom : ILootRandom
    {
        public static readonly UnityLootRandom Instance = new UnityLootRandom();

        private UnityLootRandom() { }

        public float Value() => UnityEngine.Random.value;

        public int RangeInclusive(int minInclusive, int maxInclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxInclusive + 1);
        }
    }

    public sealed class SeededLootRandom : ILootRandom
    {
        private readonly System.Random _random;

        public SeededLootRandom(int seed)
        {
            _random = new System.Random(seed);
        }

        public float Value() => (float)_random.NextDouble();

        public int RangeInclusive(int minInclusive, int maxInclusive)
        {
            return _random.Next(minInclusive, maxInclusive + 1);
        }
    }

    [Serializable]
    public class LootItemDrop
    {
        public ItemData Item;
        [Min(0)] public int MinAmount = 1;
        [Min(0)] public int MaxAmount = 1;
        [Range(0f, 1f)] public float Chance = 1f;

        public int RollAmount(ILootRandom random)
        {
            int min = Mathf.Max(0, MinAmount);
            int max = Mathf.Max(min, MaxAmount);
            return random.RangeInclusive(min, max);
        }
    }

    public struct LootRoll
    {
        public LootRoll(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public ItemData Item { get; }
        public int Amount { get; }
    }

    [CreateAssetMenu(fileName = "New Loot Table", menuName = "Project-S/Loot/Loot Table")]
    public class LootTableData : ScriptableObject
    {
        [Min(0)] public int SoulAshReward;
        public List<LootItemDrop> GuaranteedDrops = new List<LootItemDrop>();
        public List<LootItemDrop> ChanceDrops = new List<LootItemDrop>();

        public List<LootRoll> Roll(ILootRandom random = null)
        {
            if (random == null)
                random = UnityLootRandom.Instance;

            var rolls = new List<LootRoll>();
            RollGuaranteedDrops(GuaranteedDrops, random, rolls);
            RollChanceDrops(ChanceDrops, random, rolls);
            return rolls;
        }

        private static void RollGuaranteedDrops(
            IEnumerable<LootItemDrop> drops,
            ILootRandom random,
            ICollection<LootRoll> rolls)
        {
            if (drops == null)
                return;

            foreach (var drop in drops)
                AddDropRoll(drop, random, rolls);
        }

        private static void RollChanceDrops(
            IEnumerable<LootItemDrop> drops,
            ILootRandom random,
            ICollection<LootRoll> rolls)
        {
            if (drops == null)
                return;

            foreach (var drop in drops)
            {
                if (drop == null || drop.Chance <= 0f)
                    continue;

                if (drop.Chance < 1f && random.Value() > drop.Chance)
                    continue;

                AddDropRoll(drop, random, rolls);
            }
        }

        private static void AddDropRoll(LootItemDrop drop, ILootRandom random, ICollection<LootRoll> rolls)
        {
            if (drop == null || drop.Item == null)
                return;

            int amount = drop.RollAmount(random);
            if (amount > 0)
                rolls.Add(new LootRoll(drop.Item, amount));
        }
    }
}
