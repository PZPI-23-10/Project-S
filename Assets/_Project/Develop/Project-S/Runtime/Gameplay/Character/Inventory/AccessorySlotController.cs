using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    [Serializable]
    public class AccessorySlot
    {
        public string DisplayName = "Перстень";
        public AccessorySlotType SlotType = AccessorySlotType.Ring;
        public AccessoryItemData Item;
    }

    public class AccessorySlotController : MonoBehaviour
    {
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private AccessorySlot[] _slots =
        {
            new AccessorySlot { DisplayName = "Лівий перстень", SlotType = AccessorySlotType.Ring },
            new AccessorySlot { DisplayName = "Правий перстень", SlotType = AccessorySlotType.Ring }
        };

        public event Action Changed;

        private void Awake()
        {
            if (_inventory == null)
                _inventory = GetComponent<InventoryController>() ?? GetComponentInParent<InventoryController>();

            if (_stats == null)
                _stats = GetComponent<CharacterStats>() ?? GetComponentInParent<CharacterStats>();

            EnsureSlots();
            ApplyModifiers();
        }

        private void OnDisable()
        {
            if (_stats != null)
                _stats.ClearModifiers(this);
        }

        public int GetSize()
        {
            EnsureSlots();
            return _slots.Length;
        }

        public AccessorySlotType GetSlotType(int index)
        {
            EnsureSlots();
            return index >= 0 && index < _slots.Length ? _slots[index].SlotType : AccessorySlotType.Ring;
        }

        public string GetSlotName(int index)
        {
            EnsureSlots();
            return index >= 0 && index < _slots.Length ? _slots[index].DisplayName : string.Empty;
        }

        public AccessoryItemData GetItemInSlot(int index)
        {
            EnsureSlots();
            return index >= 0 && index < _slots.Length ? _slots[index].Item : null;
        }

        public bool TryEquipFromInventory(int inventorySlotIndex)
        {
            if (_inventory == null)
                return false;

            var stack = _inventory.GetSlot(inventorySlotIndex);
            if (stack == null || stack.Item == null || stack.Amount <= 0)
                return false;

            var accessory = stack.Item as AccessoryItemData;
            if (accessory == null)
                return false;

            int targetSlot = FindFirstFreeCompatibleSlot(accessory);
            if (targetSlot < 0)
                return false;

            _slots[targetSlot].Item = accessory;
            stack.Amount--;
            _inventory.SetSlot(inventorySlotIndex, stack.Amount > 0 ? stack : null);
            ApplyModifiers();
            Changed?.Invoke();
            return true;
        }

        public bool TryUnequipToInventory(int slotIndex)
        {
            EnsureSlots();

            if (_inventory == null || slotIndex < 0 || slotIndex >= _slots.Length)
                return false;

            var item = _slots[slotIndex].Item;
            if (item == null || !_inventory.CanAddItem(item, 1))
                return false;

            if (!_inventory.AddItem(item, 1))
                return false;

            _slots[slotIndex].Item = null;
            ApplyModifiers();
            Changed?.Invoke();
            return true;
        }

        public void SetSlot(int slotIndex, AccessoryItemData item)
        {
            EnsureSlots();

            if (slotIndex < 0 || slotIndex >= _slots.Length)
                return;

            if (item != null && item.SlotType != _slots[slotIndex].SlotType)
                return;

            _slots[slotIndex].Item = item;
            ApplyModifiers();
            Changed?.Invoke();
        }

        private int FindFirstFreeCompatibleSlot(AccessoryItemData item)
        {
            EnsureSlots();

            if (item == null)
                return -1;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].Item == null && _slots[i].SlotType == item.SlotType)
                    return i;
            }

            return -1;
        }

        private void ApplyModifiers()
        {
            if (_stats == null)
                return;

            var modifiers = new List<StatModifier>();
            for (int i = 0; i < _slots.Length; i++)
            {
                var item = _slots[i].Item;
                if (item == null || item.StatModifiers == null)
                    continue;

                modifiers.AddRange(item.StatModifiers);
            }

            _stats.SetModifiers(this, modifiers);
        }

        private void EnsureSlots()
        {
            if (_slots != null && _slots.Length > 0)
                return;

            _slots = new[]
            {
                new AccessorySlot { DisplayName = "Лівий перстень", SlotType = AccessorySlotType.Ring },
                new AccessorySlot { DisplayName = "Правий перстень", SlotType = AccessorySlotType.Ring }
            };
        }
    }
}
