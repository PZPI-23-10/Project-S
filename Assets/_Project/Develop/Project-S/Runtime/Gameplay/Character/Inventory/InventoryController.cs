using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    [System.Serializable]
    public class ItemStack
    {
        public ItemData Item;
        public int Amount;

        public ItemStack(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private int _inventorySize = 16;

        // Масив фіксованого розміру
        private ItemStack[] _slots;

        public System.Action OnInventoryChanged;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
            _slots = new ItemStack[_inventorySize];
        }

        public float GetMaxWeight() => _stats != null ? _stats.Get(StatType.CarryWeight) : 50f;

        public float GetCurrentWeight()
        {
            float total = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].Item != null)
                    total += _slots[i].Item.Weight * _slots[i].Amount;
            }
            return total;
        }

        public float GetWeightPenaltyMultiplier()
        {
            float current = GetCurrentWeight();
            float max = GetMaxWeight(); // Беремо саме макс. вагу гравця

            if (max <= 0) return 1f;

            float ratio = current / max;

            if (ratio >= 1.5f) return 0f;    // >= 150%: Зменшення на 100% (швидкість 0, реген 0)
            if (ratio >= 1.2f) return 0.4f;  // >= 120%: Зменшення на 60% (залишається 40%)
            if (ratio >= 1.0f) return 0.95f; // >= 100%: Зменшення на 5% (залишається 95%)

            return 1f;
        }

        public bool AddItem(ItemData item, int amountToAdd = 1)
        {
            if (item.IsStackable)
            {
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i] != null && _slots[i].Item == item && _slots[i].Amount < item.MaxStack)
                    {
                        int space = item.MaxStack - _slots[i].Amount;
                        if (amountToAdd <= space)
                        {
                            _slots[i].Amount += amountToAdd;
                            OnInventoryChanged?.Invoke();
                            return true;
                        }
                        else
                        {
                            _slots[i].Amount = item.MaxStack;
                            amountToAdd -= space;
                        }
                    }
                }
            }

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null || _slots[i].Item == null)
                {
                    _slots[i] = new ItemStack(item, amountToAdd);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            Debug.LogWarning("<color=orange>[Сумка]</color> Немає вільних слотів!");
            return false; // Повертаємо false тільки якщо фізично немає вільних клітинок
        }

        // Прямий доступ до конкретного слота
        public ItemStack GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length) return null;
            return _slots[index];
        }

        // Записати стак у конкретний слот
        public void SetSlot(int index, ItemStack stack)
        {
            if (index < 0 || index >= _slots.Length) return;
            _slots[index] = stack;
            OnInventoryChanged?.Invoke();
        }

        public ItemStack[] GetAllSlots() => _slots;
        public int GetSize() => _inventorySize;
    }
}