using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Upgrades
{
    public class UpgradeService
    {
        private readonly InventoryController _inventory;
        private readonly SoulAshWallet _wallet;
        private readonly BaseResourceStorage _baseStorage;

        public UpgradeService(InventoryController inventory, SoulAshWallet wallet, BaseResourceStorage baseStorage = null)
        {
            _inventory = inventory;
            _wallet = wallet;
            _baseStorage = baseStorage;
        }

        public UpgradeCheck Check(UpgradeDefinition upgrade, IReadOnlyCollection<string> purchasedIds)
        {
            var check = new UpgradeCheck();

            if (upgrade == null)
            {
                check.AddProblem("Апгрейд не налаштовано.");
                return check;
            }

            if (purchasedIds != null && purchasedIds.Contains(upgrade.Id))
                check.AddProblem("Вже куплено.");

            if (_inventory == null)
                check.AddProblem("Інвентар відсутній.");

            foreach (string prerequisiteId in upgrade.PrerequisiteIds ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(prerequisiteId)
                    && (purchasedIds == null || !purchasedIds.Contains(prerequisiteId)))
                {
                    check.AddProblem($"Потрібен апгрейд {prerequisiteId}.");
                }
            }

            if (upgrade.SoulAshCost > 0 && GetSoulAshCount() < upgrade.SoulAshCost)
                check.AddProblem($"Потрібно ще {upgrade.SoulAshCost - GetSoulAshCount()} попелу душ.");

            foreach (var cost in GetValidItemCosts(upgrade))
            {
                int owned = GetItemCount(cost.Item);
                if (owned < cost.Amount)
                    check.AddProblem($"Не вистачає {cost.Item.ItemName} x{cost.Amount - owned}.");
            }

            foreach (var invalidCost in (upgrade.ItemCosts ?? Enumerable.Empty<UpgradeItemCost>()).Where(x => !IsValidCost(x)))
                check.AddProblem("Ціну предмета не налаштовано.");

            return check;
        }

        public bool TryConsumeCosts(UpgradeDefinition upgrade, IReadOnlyCollection<string> purchasedIds, out UpgradeCheck check)
        {
            check = Check(upgrade, purchasedIds);
            if (!check.CanPurchase)
                return false;

            foreach (var cost in GetValidItemCosts(upgrade))
            {
                if (!TryRemoveItem(cost.Item, cost.Amount))
                {
                    check.AddProblem("Ресурси змінилися до завершення покупки.");
                    return false;
                }
            }

            if (!TrySpendSoulAsh(upgrade.SoulAshCost))
            {
                check.AddProblem("Кількість попелу душ змінилася до завершення покупки.");
                return false;
            }

            return true;
        }

        public int GetItemCount(ItemData item)
        {
            int count = _inventory != null ? _inventory.GetItemCount(item) : 0;
            if (_baseStorage != null)
                count += _baseStorage.GetItemCount(item);

            return count;
        }

        public int GetSoulAshCount()
        {
            return (_wallet != null ? _wallet.Amount : 0)
                + (_baseStorage != null ? _baseStorage.SoulAshAmount : 0);
        }

        private static IEnumerable<UpgradeItemCost> GetValidItemCosts(UpgradeDefinition upgrade)
        {
            return (upgrade.ItemCosts ?? Enumerable.Empty<UpgradeItemCost>()).Where(IsValidCost);
        }

        private static bool IsValidCost(UpgradeItemCost cost)
        {
            return cost != null && cost.Item != null && cost.Amount > 0;
        }

        private bool TryRemoveItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0 || _inventory == null)
                return false;

            int remaining = amount;
            int fromInventory = Mathf.Min(_inventory.GetItemCount(item), remaining);
            if (fromInventory > 0)
            {
                if (!_inventory.TryRemoveItem(item, fromInventory))
                    return false;

                remaining -= fromInventory;
            }

            if (remaining <= 0)
                return true;

            return _baseStorage != null && _baseStorage.TryRemoveItem(item, remaining);
        }

        private bool TrySpendSoulAsh(int amount)
        {
            if (amount <= 0)
                return true;

            int remaining = amount;
            int fromWallet = _wallet != null ? Mathf.Min(_wallet.Amount, remaining) : 0;
            if (fromWallet > 0)
            {
                if (!_wallet.Spend(fromWallet))
                    return false;

                remaining -= fromWallet;
            }

            if (remaining <= 0)
                return true;

            return _baseStorage != null && _baseStorage.SpendSoulAsh(remaining);
        }
    }
}
