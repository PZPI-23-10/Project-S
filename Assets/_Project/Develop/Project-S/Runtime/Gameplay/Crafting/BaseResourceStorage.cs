using System;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class BaseResourceStorage : MonoBehaviour, IInteractable, IItemStorage
    {
        private const int DefaultStorageSize = 24;

        [SerializeField] private string _interactionPrompt = "Base Storage";
        [SerializeField] private int _storageSize = DefaultStorageSize;
        [SerializeField] private ItemStack[] _slots;
        [SerializeField] private int _soulAshAmount;

        public static BaseResourceStorage Active { get; private set; }

        public string InteractionPrompt => _interactionPrompt;
        public int SoulAshAmount => _soulAshAmount;
        public event Action Changed;

        private void OnEnable()
        {
            EnsureSlots();

            if (Active == null)
                Active = this;
        }

        private void OnValidate()
        {
            if (_storageSize <= 0)
                _storageSize = DefaultStorageSize;
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor == null)
                return;

            var inventoryUI = FindFirstObjectByType<InventoryUI>();
            if (inventoryUI != null)
                inventoryUI.OpenWithStorage(this, transform, interactor.transform, interactor.MenuCloseDistance);
        }

        public void DepositFrom(InventoryController inventory, SoulAshWallet wallet)
        {
            DepositAllResourcesFrom(inventory);
            DepositSoulAshFrom(wallet);
        }

        public int GetSize()
        {
            EnsureSlots();
            return _slots.Length;
        }

        public ItemStack GetSlot(int index)
        {
            EnsureSlots();

            if (index < 0 || index >= _slots.Length)
                return null;

            return _slots[index];
        }

        public void SetSlot(int index, ItemStack stack)
        {
            EnsureSlots();

            if (index < 0 || index >= _slots.Length)
                return;

            if (stack != null && stack.Item != null && !CanStore(stack.Item))
                return;

            _slots[index] = stack;
            NormalizeSlot(index);
            NotifyChanged();
        }

        public int GetItemCount(ItemData item)
        {
            EnsureSlots();

            if (item == null)
                return 0;

            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                var stack = _slots[i];
                if (stack != null && stack.Item == item && stack.Amount > 0)
                    count += stack.Amount;
            }

            return count;
        }

        public bool CanAddItem(ItemData item, int amount)
        {
            EnsureSlots();

            if (item == null || amount <= 0 || !CanStore(item))
                return false;

            int remaining = amount;
            int maxStack = GetMaxStack(item);

            if (item.IsStackable)
            {
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    var stack = _slots[i];
                    if (stack == null || stack.Item != item || stack.Amount >= maxStack)
                        continue;

                    remaining -= Mathf.Min(maxStack - stack.Amount, remaining);
                }
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                var stack = _slots[i];
                if (stack != null && stack.Item != null)
                    continue;

                int add = item.IsStackable ? Mathf.Min(maxStack, remaining) : 1;
                remaining -= add;
            }

            return remaining <= 0;
        }

        public bool AddItem(ItemData item, int amount)
        {
            EnsureSlots();

            if (!CanAddItem(item, amount))
                return false;

            int remaining = amount;
            int maxStack = GetMaxStack(item);

            if (item.IsStackable)
            {
                for (int i = 0; i < _slots.Length && remaining > 0; i++)
                {
                    var stack = _slots[i];
                    if (stack == null || stack.Item != item || stack.Amount >= maxStack)
                        continue;

                    int add = Mathf.Min(maxStack - stack.Amount, remaining);
                    stack.Amount += add;
                    remaining -= add;
                }
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                if (_slots[i] != null && _slots[i].Item != null)
                    continue;

                int add = item.IsStackable ? Mathf.Min(maxStack, remaining) : 1;
                _slots[i] = new ItemStack(item, add);
                remaining -= add;
            }

            NotifyChanged();
            return true;
        }

        public bool CanRemoveItem(ItemData item, int amount)
        {
            return amount <= 0 || GetItemCount(item) >= amount;
        }

        public bool TryRemoveItem(ItemData item, int amount)
        {
            EnsureSlots();

            if (item == null || amount <= 0)
                return false;

            if (!CanRemoveItem(item, amount))
                return false;

            int remaining = amount;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = _slots[i];
                if (stack == null || stack.Item != item)
                    continue;

                int take = Mathf.Min(stack.Amount, remaining);
                stack.Amount -= take;
                remaining -= take;

                if (stack.Amount <= 0)
                    _slots[i] = null;
            }

            NotifyChanged();
            return true;
        }

        public bool TryWithdrawToInventory(int index, int amount, InventoryController inventory)
        {
            EnsureSlots();

            if (inventory == null || amount <= 0 || index < 0 || index >= _slots.Length)
                return false;

            var slot = _slots[index];
            if (slot == null || slot.Item == null || slot.Amount <= 0)
                return false;

            int take = Mathf.Min(amount, slot.Amount);
            if (!inventory.CanAddItem(slot.Item, take))
                return false;

            ItemData item = slot.Item;
            if (!inventory.AddItem(item, take))
                return false;

            slot.Amount -= take;
            if (slot.Amount <= 0)
                _slots[index] = null;

            NotifyChanged();
            return true;
        }

        public bool TryDepositFromInventory(InventoryController inventory, int slotIndex, int amount)
        {
            EnsureSlots();

            if (inventory == null || amount <= 0)
                return false;

            var source = inventory.GetSlot(slotIndex);
            if (source == null || source.Item == null || source.Amount <= 0 || !CanStore(source.Item))
                return false;

            int depositAmount = Mathf.Min(amount, source.Amount);
            ItemData item = source.Item;

            if (!CanAddItem(item, depositAmount))
                return false;

            if (!AddItem(item, depositAmount))
                return false;

            source.Amount -= depositAmount;
            inventory.SetSlot(slotIndex, source.Amount > 0 ? source : null);
            return true;
        }

        public bool DepositAllResourcesFrom(InventoryController inventory)
        {
            if (inventory == null)
                return false;

            bool changed = false;
            var slots = inventory.GetAllSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Item == null || slot.Amount <= 0 || !CanStore(slot.Item))
                    continue;

                if (TryDepositFromInventory(inventory, i, slot.Amount))
                    changed = true;
            }

            return changed;
        }

        public bool CanSpendSoulAsh(int amount)
        {
            return amount <= 0 || _soulAshAmount >= amount;
        }

        public bool SpendSoulAsh(int amount)
        {
            if (amount <= 0)
                return true;

            if (!CanSpendSoulAsh(amount))
                return false;

            _soulAshAmount -= amount;
            NotifyChanged();
            return true;
        }

        public void AddSoulAsh(int amount)
        {
            if (amount <= 0)
                return;

            _soulAshAmount += amount;
            NotifyChanged();
        }

        public bool DepositSoulAshFrom(SoulAshWallet wallet, int amount = int.MaxValue)
        {
            if (wallet == null || wallet.Amount <= 0 || amount <= 0)
                return false;

            int depositAmount = Mathf.Min(wallet.Amount, amount);
            if (!wallet.Spend(depositAmount))
                return false;

            _soulAshAmount += depositAmount;
            NotifyChanged();
            return true;
        }

        public bool WithdrawSoulAshTo(SoulAshWallet wallet, int amount = int.MaxValue)
        {
            if (wallet == null || _soulAshAmount <= 0 || amount <= 0)
                return false;

            int withdrawAmount = Mathf.Min(_soulAshAmount, amount);
            _soulAshAmount -= withdrawAmount;
            wallet.Add(withdrawAmount);
            NotifyChanged();
            return true;
        }

        private static bool CanStore(ItemData item)
        {
            return item != null && (item.Kind == ItemKind.Resource || item.Kind == ItemKind.Material);
        }

        private static int GetMaxStack(ItemData item)
        {
            if (item == null)
                return 1;

            return item.IsStackable ? Mathf.Max(1, item.MaxStack) : 1;
        }

        private void EnsureSlots()
        {
            if (_storageSize <= 0)
                _storageSize = DefaultStorageSize;

            if (_slots != null && _slots.Length == _storageSize)
                return;

            var oldSlots = _slots;
            _slots = new ItemStack[_storageSize];

            if (oldSlots == null)
                return;

            int count = Mathf.Min(oldSlots.Length, _slots.Length);
            for (int i = 0; i < count; i++)
            {
                _slots[i] = oldSlots[i];
                NormalizeSlot(i);
            }
        }

        private void NormalizeSlot(int index)
        {
            var slot = _slots[index];
            if (slot == null || slot.Item == null || slot.Amount <= 0 || !CanStore(slot.Item))
            {
                _slots[index] = null;
                return;
            }

            slot.Amount = Mathf.Min(slot.Amount, GetMaxStack(slot.Item));
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
