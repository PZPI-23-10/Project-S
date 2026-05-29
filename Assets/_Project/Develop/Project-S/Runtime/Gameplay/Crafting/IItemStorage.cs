using System;
using Project_S.Runtime.Gameplay.Character.Inventory;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public interface IItemStorage
    {
        string InteractionPrompt { get; }
        event Action Changed;

        int GetSize();
        ItemStack GetSlot(int index);
        void SetSlot(int index, ItemStack stack);
        int GetItemCount(ItemData item);
        bool CanStoreItem(ItemData item);
        bool CanAddItem(ItemData item, int amount);
        bool AddItem(ItemData item, int amount);
        bool CanRemoveItem(ItemData item, int amount);
        bool TryRemoveItem(ItemData item, int amount);
        bool TryWithdrawToInventory(int index, int amount, InventoryController inventory);
        bool TryDepositFromInventory(InventoryController inventory, int slotIndex, int amount);
    }
}
