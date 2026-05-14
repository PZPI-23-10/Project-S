using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Project_S.Runtime.Gameplay.Character.Inventory;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Зв'язки")]
        [SerializeField] private GameObject _inventoryPanel; // Головна панель (рюкзак)
        [SerializeField] private Transform _slotsGrid;       // Об'єкт з Grid Layout Group
        [SerializeField] private InventorySlotUI _slotPrefab;// Префаб кнопки-слота
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private TMP_Text _weightText;

        [Header("Налаштування")]
        [SerializeField] private int _totalSlots = 16; // Скільки слотів малювати в сітці

        private List<InventorySlotUI> _createdSlots = new List<InventorySlotUI>();

        private void Awake()
        {
            GenerateSlots();
            if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
        }

        private void GenerateSlots()
        {
            foreach (Transform child in _slotsGrid) Destroy(child.gameObject);

            for (int i = 0; i < _totalSlots; i++)
            {
                InventorySlotUI newSlot = Instantiate(_slotPrefab, _slotsGrid);
                newSlot.Setup(null);
                _createdSlots.Add(newSlot);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
            {
                bool nextState = !_inventoryPanel.activeSelf;
                _inventoryPanel.SetActive(nextState);

                if (nextState)
                {
                    Refresh();
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        public void Refresh()
        {
            if (_inventory == null) return;

            var items = _inventory.GetAllItems();

            for (int i = 0; i < _createdSlots.Count; i++)
            {
                if (i < items.Count) _createdSlots[i].Setup(items[i]);
                else _createdSlots[i].Setup(null);
            }

            if (_weightText != null)
            {
                // Виправлено: тепер викликаємо метод GetMaxWeight()
                _weightText.text = $"Вага: {_inventory.GetCurrentWeight()} / {_inventory.GetMaxWeight()} кг";
            }
        }
    }
}