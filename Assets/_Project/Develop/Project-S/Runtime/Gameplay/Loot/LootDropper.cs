using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Loot
{
    public class LootDropper : MonoBehaviour
    {
        [SerializeField] private LootTableData _lootTable;

        public LootTableData LootTable => _lootTable;

        public void Configure(LootTableData lootTable)
        {
            _lootTable = lootTable;
        }

        public bool DropFor(GameObject source, int fallbackSoulAshReward = 0)
        {
            var inventory = source != null ? source.GetComponentInParent<InventoryController>() : null;
            var wallet = ResolveWallet(source, inventory);
            if (inventory == null && wallet == null)
                return false;

            int soulAshReward = _lootTable != null ? _lootTable.SoulAshReward : fallbackSoulAshReward;
            if (wallet != null && soulAshReward > 0)
            {
                wallet.AddReward(soulAshReward, source);
                Debug.Log($"[Loot] Added {soulAshReward} Soul Ash reward.");
            }

            if (_lootTable == null)
                return true;

            foreach (var roll in _lootTable.Roll())
                WorldItemDropUtility.GrantOrDrop(roll.Item, roll.Amount, inventory, transform.position, "[Loot]");

            return true;
        }

        private static SoulAshWallet ResolveWallet(GameObject source, InventoryController inventory)
        {
            if (source != null)
            {
                var wallet = source.GetComponentInParent<SoulAshWallet>();
                if (wallet != null)
                    return wallet;
            }

            return inventory != null ? inventory.GetComponent<SoulAshWallet>() : null;
        }
    }
}
