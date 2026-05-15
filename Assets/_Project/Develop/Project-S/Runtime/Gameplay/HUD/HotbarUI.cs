using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Combat; // ДОДАЛИ ЦЕ: щоб бачити боївку
using System.Collections.Generic;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private CombatController _combatController; // ДОДАЛИ ЦЕ: посилання на боївку
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private Transform _hotbarGrid;
        [SerializeField] private int _hotbarSize = 5;

        [Header("Налаштування виділення")]
        [SerializeField] private RectTransform _selectionHighlight; // Та сама рамка
        private int _currentSelectedIndex = 0;

        private List<InventorySlotUI> _hotbarSlots = new List<InventorySlotUI>();

        private void Start()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += RefreshHotbar;
                GenerateHotbar();
            }
        }

        private void Update()
        {
            // Перевірка клавіш 1-5
            for (int i = 0; i < _hotbarSize; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                }
            }
        }

        private void SelectSlot(int index)
        {
            _currentSelectedIndex = index;

            // Переміщуємо рамку до обраного слота
            if (_selectionHighlight != null && _hotbarSlots.Count > index)
            {
                _selectionHighlight.SetParent(_hotbarSlots[index].transform);
                _selectionHighlight.localPosition = Vector3.zero; // Центруємо
            }

            // ПЕРЕВІРКА: Що у нас у цьому слоті?
            ItemStack stack = _inventory.GetSlot(index);

            if (stack != null && stack.Item != null)
            {
                Debug.Log($"<color=yellow>[Hotbar]</color> Обрано: <b>{stack.Item.ItemName}</b> (x{stack.Amount})");

                // ПРОБУЄМО ЕКІПІРУВАТИ: 
                // "as WeaponItemData" спробує перетворити предмет на зброю. Якщо це яблуко - буде null.
                WeaponItemData weaponData = stack.Item as WeaponItemData;

                if (_combatController != null)
                {
                    _combatController.EquipWeapon(weaponData); // Якщо weaponData == null, дістануться кулаки
                }
            }
            else
            {
                Debug.Log("<color=grey>[Hotbar]</color> Слот порожній, дістаємо кулаки");
                if (_combatController != null)
                {
                    _combatController.EquipWeapon(null); // Слот порожній -> беремо кулаки
                }
            }
        }

        private void GenerateHotbar()
        {
            foreach (Transform child in _hotbarGrid) Destroy(child.gameObject);
            _hotbarSlots.Clear();

            InventoryUI mainUI = FindFirstObjectByType<InventoryUI>();

            for (int i = 0; i < _hotbarSize; i++)
            {
                InventorySlotUI newSlot = Instantiate(_slotPrefab, _hotbarGrid);
                newSlot.Init(i, mainUI);
                _hotbarSlots.Add(newSlot);
            }

            RefreshHotbar();
            SelectSlot(0); // Виділяємо перший слот при старті
        }

        public void RefreshHotbar()
        {
            var allSlots = _inventory.GetAllSlots();
            for (int i = 0; i < _hotbarSlots.Count; i++)
            {
                if (i < allSlots.Length)
                {
                    _hotbarSlots[i].UpdateView(allSlots[i]);
                }
            }
        }
    }
}