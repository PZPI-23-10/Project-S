using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Основні панелі")]
        [SerializeField] private GameObject _inventoryPanel;
        [SerializeField] private GameObject _contextPanel;

        [Header("Внутрішні зв'язки")]
        [SerializeField] private Transform _slotsGrid;
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private TMP_Text _weightText;

        [Header("Іконка на курсорі (Drag & Drop)")]
        [SerializeField] private Image _draggedItemIcon;
        [SerializeField] private TMP_Text _draggedItemAmount;

        private InventorySlotUI[] _createdSlots;
        private ItemStack _draggedStack;

        private void Awake()
        {
            // В Awake лише ховаємо панельки зі старту
            if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
            if (_contextPanel != null) _contextPanel.SetActive(false);
            SetDraggedIconActive(false);
        }

        private void Start()
        {
            // У Start вже безпечно звертатися до інших скриптів
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += Refresh;
                GenerateSlots();
            }
        }

        private void GenerateSlots()
        {
            if (_slotsGrid == null || _slotPrefab == null) return;

            foreach (Transform child in _slotsGrid) Destroy(child.gameObject);

            int size = _inventory.GetSize();
            _createdSlots = new InventorySlotUI[size];

            for (int i = 0; i < size; i++)
            {
                InventorySlotUI newSlot = Instantiate(_slotPrefab, _slotsGrid);
                newSlot.Init(i, this);
                _createdSlots[i] = newSlot;
            }
            Refresh();
        }

        private void Update()
        {
            if (_draggedStack != null && _draggedItemIcon != null && _draggedItemIcon.gameObject.activeSelf)
            {
                _draggedItemIcon.transform.position = UnityEngine.Input.mousePosition;

                if (UnityEngine.Input.GetKeyDown(KeyCode.Mouse0))
                {
                    if (!EventSystem.current.IsPointerOverGameObject())
                    {
                        DropDraggedStackToWorld();
                    }
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) || UnityEngine.Input.GetKeyDown(KeyCode.I))
            {
                bool nextState = !_inventoryPanel.activeSelf;
                _inventoryPanel.SetActive(nextState);
                if (_contextPanel != null) _contextPanel.SetActive(nextState);

                if (nextState)
                {
                    Refresh();
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    if (_draggedStack != null)
                    {
                        _inventory.AddItem(_draggedStack.Item, _draggedStack.Amount);
                        ClearDraggedItem();
                    }
                    TooltipUI.Instance?.Hide();
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1)) UseHotbarSlot(0);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha2)) UseHotbarSlot(1);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha3)) UseHotbarSlot(2);
        }

        private void UseHotbarSlot(int index)
        {
            ItemStack stack = _inventory.GetSlot(index);
            if (stack != null && stack.Item != null)
            {
                Debug.Log($"Використовуємо: {stack.Item.ItemName}");
            }
        }

        public void OnSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            ItemStack targetSlotStack = _inventory.GetSlot(slotIndex);

            // ==========================================
            // ЛІВА КНОПКА МИШІ (Взяття, Свап, Половина)
            // ==========================================
            if (button == PointerEventData.InputButton.Left)
            {
                // 1. Курсор порожній, беремо зі слота
                if (_draggedStack == null && targetSlotStack != null)
                {
                    // ШИФТ + КЛІК: Беремо рівно половину стака
                    if (Input.GetKey(KeyCode.LeftShift) && targetSlotStack.Item.IsStackable && targetSlotStack.Amount > 1)
                    {
                        int takeAmount = targetSlotStack.Amount / 2;
                        int leaveAmount = targetSlotStack.Amount - takeAmount;

                        // Створюємо новий стак для мишки з половиною предметів
                        _draggedStack = new ItemStack(targetSlotStack.Item, takeAmount);

                        // Залишаємо іншу половину в слоті
                        targetSlotStack.Amount = leaveAmount;
                        _inventory.SetSlot(slotIndex, targetSlotStack);

                        UpdateDraggedIcon();
                    }
                    // ЗВИЧАЙНИЙ КЛІК: Беремо весь стак
                    else
                    {
                        _draggedStack = targetSlotStack;
                        _inventory.SetSlot(slotIndex, null);
                        UpdateDraggedIcon();
                    }
                }
                // 2. На курсорі вже є предмет
                else if (_draggedStack != null)
                {
                    // Слот порожній - кладемо все, що в руці
                    if (targetSlotStack == null)
                    {
                        _inventory.SetSlot(slotIndex, _draggedStack);
                        ClearDraggedItem();
                    }
                    else // У слоті щось є
                    {
                        // Це однакові предмети? Тоді зсипаємо їх до купи
                        if (targetSlotStack.Item == _draggedStack.Item && targetSlotStack.Item.IsStackable)
                        {
                            int space = targetSlotStack.Item.MaxStack - targetSlotStack.Amount;
                            if (space > 0)
                            {
                                int toAdd = Mathf.Min(space, _draggedStack.Amount);
                                targetSlotStack.Amount += toAdd;
                                _draggedStack.Amount -= toAdd;

                                if (_draggedStack.Amount <= 0) ClearDraggedItem();
                                else UpdateDraggedIcon();

                                _inventory.SetSlot(slotIndex, targetSlotStack);
                                return;
                            }
                        }

                        // Якщо предмети різні (або стак повний) - просто міняємо їх місцями
                        ItemStack temp = targetSlotStack;
                        _inventory.SetSlot(slotIndex, _draggedStack);
                        _draggedStack = temp;
                        UpdateDraggedIcon();
                    }
                }
            }
            // ==========================================
            // ПРАВА КНОПКА МИШІ (Екіпірування, По одному)
            // ==========================================
            else if (button == PointerEventData.InputButton.Right)
            {
                // 1. Курсор порожній 
                if (_draggedStack == null && targetSlotStack != null)
                {
                    // НОВА ЛОГІКА: Якщо предмет стакається (ресурси) - беремо 1 шт.
                    if (targetSlotStack.Item.IsStackable)
                    {
                        _draggedStack = new ItemStack(targetSlotStack.Item, 1);

                        targetSlotStack.Amount--;
                        if (targetSlotStack.Amount <= 0)
                            _inventory.SetSlot(slotIndex, null);
                        else
                            _inventory.SetSlot(slotIndex, targetSlotStack);

                        UpdateDraggedIcon();
                    }
                    // Якщо предмет НЕ стакається (зброя, броня) - ЕКІПІРУЄМО
                    else
                    {
                        EquipmentSlots eq = FindFirstObjectByType<EquipmentSlots>();
                        if (eq != null)
                        {
                            eq.EquipItem(targetSlotStack.Item);
                        }
                    }
                }
                // 2. На курсорі є предмет - КЛАДЕМО ПО 1 ШТУЦІ у слот
                else if (_draggedStack != null)
                {
                    // Якщо клікаємо по порожньому слоту - відокремлюємо туди 1 штуку
                    if (targetSlotStack == null)
                    {
                        _inventory.SetSlot(slotIndex, new ItemStack(_draggedStack.Item, 1));

                        _draggedStack.Amount--;
                        if (_draggedStack.Amount <= 0) ClearDraggedItem();
                        else UpdateDraggedIcon();
                    }
                    // Якщо клікаємо по такому ж предмету - додаємо 1 штуку в стак
                    else if (targetSlotStack.Item == _draggedStack.Item && targetSlotStack.Item.IsStackable)
                    {
                        if (targetSlotStack.Amount < targetSlotStack.Item.MaxStack)
                        {
                            targetSlotStack.Amount++;
                            _inventory.SetSlot(slotIndex, targetSlotStack);

                            _draggedStack.Amount--;
                            if (_draggedStack.Amount <= 0) ClearDraggedItem();
                            else UpdateDraggedIcon();
                        }
                    }
                }
            }
        }

        private void DropDraggedStackToWorld()
        {
            if (_draggedStack == null || _draggedStack.Item.WorldPickupPrefab == null) return;

            Transform p = _inventory.transform;
            Vector3 pos = p.position + p.forward * 1.5f + Vector3.up * 0.5f;

            GameObject dropped = Instantiate(_draggedStack.Item.WorldPickupPrefab, pos, Quaternion.identity);
            if (dropped.TryGetComponent(out ItemPickup pickup))
            {
                pickup.Amount = _draggedStack.Amount;
            }

            ClearDraggedItem();
        }

        private void UpdateDraggedIcon()
        {
            if (_draggedStack != null && _draggedStack.Item != null)
            {
                _draggedItemIcon.sprite = _draggedStack.Item.Icon;
                if (_draggedItemAmount != null)
                    _draggedItemAmount.text = _draggedStack.Amount > 1 ? _draggedStack.Amount.ToString() : "";

                SetDraggedIconActive(true);
            }
            else ClearDraggedItem();
        }

        private void ClearDraggedItem()
        {
            _draggedStack = null;
            SetDraggedIconActive(false);
        }

        private void SetDraggedIconActive(bool active)
        {
            if (_draggedItemIcon != null)
            {
                _draggedItemIcon.gameObject.SetActive(active);
                _draggedItemIcon.raycastTarget = false;
            }
        }

        public void Refresh()
        {
            if (_inventory == null || _createdSlots == null) return;

            var slots = _inventory.GetAllSlots();
            // Захист: якщо масив ще не встиг створитися, перериваємо виконання
            if (slots == null) return;

            for (int i = 0; i < _createdSlots.Length; i++)
            {
                if (_createdSlots[i] != null)
                {
                    // Захист від виходу за межі масиву
                    _createdSlots[i].UpdateView(i < slots.Length ? slots[i] : null);
                }
            }

            if (_weightText != null)
            {
                _weightText.text = $"Вага: {_inventory.GetCurrentWeight():F1} / {_inventory.GetMaxWeight():F1} кг";
            }
        }
    }
}