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
        [SerializeField] private int _hotbarSize = 6;
        [SerializeField] private Vector2 _slotSize = new Vector2(72f, 72f);
        [SerializeField] private float _slotSpacing = 10f;
        [SerializeField] private Vector2 _panelPadding = new Vector2(12f, 10f);

        [Header("Налаштування анімації")]
        [Tooltip("Затримка між перемиканнями зброї")]
        [SerializeField] private float _switchCooldown = 0.3f;
        private float _lastSwitchTime;

        [Header("Налаштування їжі")]
        [Tooltip("Час у секундах між поїданням/питтям (Кулдаун)")]
        [SerializeField] private float _consumableCooldown = 3f;
        private float _lastConsumeTime = -999f;

        // ЗАПОБІЖНИК ВІД БАГУ ОСТАННЬОГО ПРЕДМЕТА:
        private int _lastConsumeFrame = -1;

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

        private void Update()
        {
            if (Cursor.visible) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");

            // Скрол миші тільки перемикає виділення, не використовує предмети
            if (scroll > 0f) SelectPreviousSlot(false);
            else if (scroll < 0f) SelectNextSlot(false);

            // Обробка клавіатури
            for (int i = 0; i < Mathf.Min(_hotbarSize, 9); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SelectSlot(i, true);
                }
            }
        }

        private void SelectPreviousSlot(bool autoUse)
        {
            int newIndex = _currentSelectedIndex - 1;
            if (newIndex < 0) newIndex = _hotbarSize - 1;
            SelectSlot(newIndex, autoUse);
        }

        private void SelectNextSlot(bool autoUse)
        {
            int newIndex = _currentSelectedIndex + 1;
            if (newIndex >= _hotbarSize) newIndex = 0;
            SelectSlot(newIndex, autoUse);
        }

        public void Tick(PlayerInputSnapshot input)
        {
            if (input.HotbarSlotPressed >= 0)
                SelectSlot(input.HotbarSlotPressed, true);
        }

        public void SelectSlot(int index, bool autoUse = true)
        {
            if (index < 0 || index >= _hotbarSize) return;

            ItemStack targetStack = _inventory != null ? _inventory.GetSlot(index) : null;

            // 1. ЯКЩО В СЛОТІ ЗІЛЛЯ
            if (targetStack != null && targetStack.Item != null && targetStack.Item.IsUsable)
            {
                if (!autoUse)
                {
                    _currentSelectedIndex = index;
                    RefreshSelectionVisuals();
                    _combatController?.EquipWeapon(null);
                    return;
                }

                if (Time.time - _lastConsumeTime < 3f) return;

                if (_inventory.TryUseItemAtSlot(index))
                {
                    _lastConsumeTime = Time.time;
                    _lastConsumeFrame = Time.frameCount; // МАГІЯ: Запам'ятовуємо кадр, коли випили зілля!

                    if (targetStack.Item.ConsumeSound != null && UnityEngine.Camera.main != null)
                    {
                        AudioSource.PlayClipAtPoint(targetStack.Item.ConsumeSound, UnityEngine.Camera.main.transform.position, 1f);
                    }
                }

                RefreshSelectionVisuals();
                return;
            }

            // 2. ЯКЩО СЛОТ ПОРОЖНІЙ АБО ТАМ ЗБРОЯ

            // МАГІЯ: Якщо ми щойно (в цьому ж кадрі) з'їли зілля і слот став пустим - БЛОКУЄМО ПЕРЕМИКАННЯ!
            if (Time.frameCount == _lastConsumeFrame) return;

            if (Time.time - _lastSwitchTime < _switchCooldown) return;
            _lastSwitchTime = Time.time;

            _currentSelectedIndex = index;
            RefreshSelectionVisuals();
            SyncWeaponWithCurrentSlot();
        }

        private void SyncWeaponWithCurrentSlot()
        {
            ItemStack stack = _inventory != null ? _inventory.GetSlot(_currentSelectedIndex) : null;

            if (stack != null && stack.Item != null && stack.Amount > 0)
            {
                if (stack.Item.IsUsable)
                {
                    _combatController?.EquipWeapon(null);
                    return;
                }

                WeaponItemData weaponData = stack.Item as WeaponItemData;
                if (_combatController != null && _combatController.CurrentWeapon != weaponData)
                {
                    _combatController.EquipWeapon(weaponData);
                }
            }
            else
            {
                if (_combatController != null && _combatController.CurrentWeapon != null)
                {
                    if (_combatController.ActiveWeapon != null && _combatController.CurrentWeapon.WeaponPrefab != null)
                    {
                        _combatController.EquipWeapon(null);
                    }
                }
            }
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

            SyncWeaponWithCurrentSlot();
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

            _currentSelectedIndex = 0;
            RefreshSelectionVisuals();
            SyncWeaponWithCurrentSlot();
        }

        private void OnHotbarSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Left)
                SelectSlot(slotIndex, true);
        }

        // --- БЛОК НАЛАШТУВАННЯ UI ---
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
            if (layout == null) layout = _hotbarGrid.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = _slotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (transform is RectTransform panelRect)
            {
                float panelWidth = _slotSize.x * _hotbarSize + _slotSpacing * Mathf.Max(0, _hotbarSize - 1) + _panelPadding.x * 2f;
                float panelHeight = _slotSize.y + _panelPadding.y * 2f;
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, panelWidth);
                panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);
            }
        }

        private void ConfigureSlotRect(InventorySlotUI slot)
        {
            if (slot == null) return;

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
            if (layoutElement == null) layoutElement = slot.gameObject.AddComponent<LayoutElement>();

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