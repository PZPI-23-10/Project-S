using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private CombatController _combatController;
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private Transform _hotbarGrid;
        [SerializeField] private int _hotbarSize = 6; // За замовчуванням тепер 6
        [SerializeField] private Vector2 _slotSize = new Vector2(72f, 72f);
        [SerializeField] private float _slotSpacing = 10f;
        [SerializeField] private Vector2 _panelPadding = new Vector2(12f, 10f);

        private readonly List<InventorySlotUI> _hotbarSlots = new List<InventorySlotUI>();
        private int _currentSelectedIndex = 0;

        private void Start()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += RefreshHotbar;
                GenerateHotbar();
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= RefreshHotbar;
        }

        // НОВИЙ БЛОК: Читаємо мишку і клавіатуру прямо тут
        private void Update()
        {
            // МАГІЯ ТУТ: Якщо курсор на екрані (відкритий інвентар/крафт), блокуємо хотбар!
            if (Cursor.visible) return;

            // 1. Коліщатко мишки
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
                SelectPreviousSlot();
            else if (scroll < 0f)
                SelectNextSlot();

            // 2. Кнопки від 1 до 9
            for (int i = 0; i < Mathf.Min(_hotbarSize, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i);
                }
            }
        }

        // НОВА ФУНКЦІЯ: Наступний слот (з перекиданням на початок)
        private void SelectNextSlot()
        {
            int nextIndex = _currentSelectedIndex + 1;
            if (nextIndex >= _hotbarSize) nextIndex = 0;
            SelectSlot(nextIndex);
        }

        // НОВА ФУНКЦІЯ: Попередній слот (з перекиданням у кінець)
        private void SelectPreviousSlot()
        {
            int prevIndex = _currentSelectedIndex - 1;
            if (prevIndex < 0) prevIndex = _hotbarSize - 1;
            SelectSlot(prevIndex);
        }

        public void Tick(PlayerInputSnapshot input)
        {
            // Стара логіка вводу (залишаємо про всяк випадок)
            if (input.HotbarSlotPressed >= 0)
                SelectSlot(input.HotbarSlotPressed);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _hotbarSize)
                return;

            _currentSelectedIndex = index;
            RefreshSelectionVisuals();

            ItemStack stack = _inventory != null ? _inventory.GetSlot(index) : null;

            if (stack != null && stack.Item != null)
            {
                if (stack.Item.IsUsable)
                {
                    if (_inventory.TryUseItemAtSlot(index))
                        Debug.Log($"[Hotbar] Used {stack.Item.ItemName}.");

                    return;
                }

                WeaponItemData weaponData = stack.Item as WeaponItemData;

                if (_combatController != null)
                    _combatController.EquipWeapon(weaponData);
            }
            else if (_combatController != null)
            {
                _combatController.EquipWeapon(null);
            }
        }

        private void GenerateHotbar()
        {
            if (_hotbarGrid == null || _slotPrefab == null || _inventory == null) return;

            ConfigureLayout();

            foreach (Transform child in _hotbarGrid) Destroy(child.gameObject);
            _hotbarSlots.Clear();

            for (int i = 0; i < _hotbarSize; i++)
            {
                InventorySlotUI newSlot = Instantiate(_slotPrefab, _hotbarGrid);
                ConfigureSlotRect(newSlot);
                newSlot.Init(i, null, OnHotbarSlotClicked);
                _hotbarSlots.Add(newSlot);
            }

            RefreshHotbar();
            SelectSlot(0);
        }

        private void OnHotbarSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left)
                SelectSlot(slotIndex);
        }

        public void RefreshHotbar()
        {
            if (_inventory == null) return;

            var allSlots = _inventory.GetAllSlots();
            for (int i = 0; i < _hotbarSlots.Count; i++)
            {
                if (i < allSlots.Length)
                    _hotbarSlots[i].UpdateView(allSlots[i]);
            }
        }

        private void ConfigureLayout()
        {
            var gridRect = _hotbarGrid as RectTransform;
            if (gridRect != null)
            {
                gridRect.anchorMin = new Vector2(0.5f, 0.5f);
                gridRect.anchorMax = new Vector2(0.5f, 0.5f);
                gridRect.pivot = new Vector2(0.5f, 0.5f);
                gridRect.anchoredPosition = Vector2.zero;

                float width = _slotSize.x * _hotbarSize + _slotSpacing * Mathf.Max(0, _hotbarSize - 1);
                gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _slotSize.y);
            }

            if (_hotbarGrid.TryGetComponent(out ContentSizeFitter fitter))
            {
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            var layout = _hotbarGrid.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _hotbarGrid.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = _slotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (transform is RectTransform panelRect)
            {
                float panelWidth = _slotSize.x * _hotbarSize
                    + _slotSpacing * Mathf.Max(0, _hotbarSize - 1)
                    + _panelPadding.x * 2f;
                float panelHeight = _slotSize.y + _panelPadding.y * 2f;
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
            }
        }

        private void ConfigureSlotRect(InventorySlotUI slot)
        {
            if (slot == null)
                return;

            if (slot.transform is RectTransform slotRect)
            {
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.localScale = Vector3.one;
                slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _slotSize.x);
                slotRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _slotSize.y);
            }

            var layoutElement = slot.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = slot.gameObject.AddComponent<LayoutElement>();

            layoutElement.minWidth = _slotSize.x;
            layoutElement.minHeight = _slotSize.y;
            layoutElement.preferredWidth = _slotSize.x;
            layoutElement.preferredHeight = _slotSize.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < _hotbarSlots.Count; i++)
            {
                if (_hotbarSlots[i] != null)
                    _hotbarSlots[i].SetSelected(i == _currentSelectedIndex);
            }
        }
    }
}