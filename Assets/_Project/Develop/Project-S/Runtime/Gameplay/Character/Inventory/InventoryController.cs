using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private List<ItemData> _items = new List<ItemData>();

        public System.Action OnInventoryChanged;

        private void Awake()
        {
            if (_stats == null) _stats = GetComponent<CharacterStats>();
        }

        public float GetMaxWeight()
        {
            // Беремо адекватний стат CarryWeight замість маразму з Poise
            return _stats != null ? _stats.Get(StatType.CarryWeight) : 50f; 
        }

        public float GetCurrentWeight()
        {
            float weight = 0;
            foreach (var item in _items) 
            {
                if (item != null) weight += item.Weight;
            }
            return weight;
        }

        public bool AddItem(ItemData item)
        {
            if (GetCurrentWeight() + item.Weight > GetMaxWeight())
            {
                Debug.LogWarning("<color=red>[Інвентар]</color> Заважко! Перевантаження.");
                return false;
            }

            _items.Add(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public List<ItemData> GetAllItems() => _items;
    }
}