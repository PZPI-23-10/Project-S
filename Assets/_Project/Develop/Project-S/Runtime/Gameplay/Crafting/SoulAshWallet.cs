using System;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class SoulAshWallet : MonoBehaviour
    {
        [SerializeField] private int _amount;

        public int Amount => _amount;
        public event Action<int> Changed;

        public bool CanSpend(int amount)
        {
            return amount <= 0 || _amount >= amount;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            SetAmount(_amount + amount);
        }

        public void AddReward(int amount, GameObject rewardSource = null)
        {
            if (amount <= 0)
                return;

            var buffs = rewardSource != null ? rewardSource.GetComponentInParent<BuffController>() : null;
            if (buffs == null)
                buffs = GetComponentInParent<BuffController>();

            float multiplier = buffs != null ? buffs.SoulAshRewardMultiplier : 1f;
            float scaledAmount = amount * multiplier;
            int finalAmount = multiplier >= 1f
                ? Mathf.CeilToInt(scaledAmount)
                : Mathf.FloorToInt(scaledAmount);

            finalAmount = Mathf.Max(0, finalAmount);
            Add(finalAmount);
        }

        public bool Spend(int amount)
        {
            if (amount <= 0)
                return true;

            if (!CanSpend(amount))
                return false;

            SetAmount(_amount - amount);
            return true;
        }

        public void SetAmount(int amount)
        {
            int nextAmount = Mathf.Max(0, amount);
            if (_amount == nextAmount)
                return;

            _amount = nextAmount;
            Changed?.Invoke(_amount);
        }
    }
}
