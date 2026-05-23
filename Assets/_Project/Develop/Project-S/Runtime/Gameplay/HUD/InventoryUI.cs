/*using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _inventoryPanel;
        [SerializeField] private GameObject _contextPanel;

        [Header("Inventory")]
        [SerializeField] private Transform _slotsGrid;
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private TMP_Text _weightText;

        [Header("Drag and drop")]
        [SerializeField] private Image _draggedItemIcon;
        [SerializeField] private TMP_Text _draggedItemAmount;

        [Header("Interaction")]
        [SerializeField] private float _defaultInteractionCloseDistance = 3f;
        [SerializeField] private PlayerActionGate _actionGate;

        private InventorySlotUI[] _createdSlots;
        private ItemStack _draggedStack;
        private CraftingPanelUI _craftingPanel;
        private StoragePanelUI _storagePanel;
        private AccessoryPanelUI _accessoryPanel;
        private AccessorySlotController _accessories;
        private IItemStorage _activeStorage;
        private SoulAshWallet _soulAshWallet;
        private CraftingContext _currentCraftingContext = CraftingContext.Hand;
        private Transform _distanceCloseTarget;
        private Transform _distanceCloseObserver;
        private float _distanceCloseRange;
        private bool _hasDistanceCloseTarget;
       
        public bool IsOpen => _inventoryPanel != null && _inventoryPanel.activeSelf;
        public bool IsStorageOpen => IsOpen && _activeStorage != null;

        private void Awake()
        {
            if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
            if (_contextPanel != null) _contextPanel.SetActive(false);
            SetDraggedIconActive(false);
        }

        private void Start()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += Refresh;
                GenerateSlots();
                InitializeCraftingPanel();
                InitializeAccessoryPanel();
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= Refresh;

            _storagePanel?.ClearStorage();
        }

        private void GenerateSlots()
        {
            if (_slotsGrid == null || _slotPrefab == null || _inventory == null) return;

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
            UpdateDraggedItem();
            if (SwitchToHandCraftingIfTooFarFromInteractionTarget())
                return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) || UnityEngine.Input.GetKeyDown(KeyCode.I))
            {
                bool isOpen = _inventoryPanel != null && _inventoryPanel.activeSelf;
                SetInventoryOpen(!isOpen, CraftingContext.Hand);
            }
        }

        public void OpenWithCraftingContext(CraftingContext context)
        {
            _activeStorage = null;
            ClearDistanceCloseTarget();
            SetInventoryOpen(true, context);
        }

        public void OpenWithCraftingContext(
            CraftingContext context,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            _activeStorage = null;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, context);
        }

        public void OpenWithStorage(
            BaseResourceStorage storage,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            if (storage == null)
                return;

            _activeStorage = storage;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, CraftingContext.Hand);
        }

        public void OpenWithGeneralStorage(
            GeneralItemStorage storage,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            if (storage == null)
                return;

            _activeStorage = storage;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, CraftingContext.Hand);
        }

        public void SetCraftingContext(CraftingContext context)
        {
            _currentCraftingContext = context;
            if (_craftingPanel != null)
                _craftingPanel.SetContext(context);
        }

        public void SetInventoryOpen(bool open, CraftingContext context)
        {
            _activeStorage = null;

            if (open)
                ClearDistanceCloseTarget();

            SetInventoryOpenInternal(open, context);
        }

        private void SetInventoryOpenInternal(bool open, CraftingContext context)
        {
            if (_inventoryPanel == null)
                return;

            _currentCraftingContext = context;
            _inventoryPanel.SetActive(open);
            if (_contextPanel != null) _contextPanel.SetActive(open);
            ResolveActionGate()?.SetInventoryOpen(open);

            if (open)
            {
                InitializeCraftingPanel();
                InitializeStoragePanel();
                InitializeAccessoryPanel();

                bool storageMode = _activeStorage != null;
                _craftingPanel?.SetPanelVisible(!storageMode);
                if (storageMode)
                    _storagePanel?.SetStorage(_activeStorage);
                else
                {
                    _storagePanel?.ClearStorage();
                    _craftingPanel?.SetContext(_currentCraftingContext);
                }

                Refresh();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                ClearDistanceCloseTarget();
                ReturnDraggedStackToInventory();
                _storagePanel?.ClearStorage();
                _craftingPanel?.SetPanelVisible(true);
                _activeStorage = null;
                TooltipUI.Instance?.Hide();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void OnSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            ItemStack targetSlotStack = _inventory.GetSlot(slotIndex);

            if (_activeStorage != null
                && button == PointerEventData.InputButton.Right
                && _draggedStack == null
                && targetSlotStack != null)
            {
                _activeStorage.TryDepositFromInventory(_inventory, slotIndex, int.MaxValue);
                Refresh();
                return;
            }

            if (button == PointerEventData.InputButton.Left)
            {
                if (_draggedStack == null && targetSlotStack != null)
                {
                    if (Input.GetKey(KeyCode.LeftShift) && targetSlotStack.Item.IsStackable && targetSlotStack.Amount > 1)
                    {
                        int takeAmount = targetSlotStack.Amount / 2;
                        int leaveAmount = targetSlotStack.Amount - takeAmount;

                        _draggedStack = new ItemStack(targetSlotStack.Item, takeAmount);
                        targetSlotStack.Amount = leaveAmount;
                        _inventory.SetSlot(slotIndex, targetSlotStack);
                        UpdateDraggedIcon();
                    }
                    else
                    {
                        _draggedStack = targetSlotStack;
                        _inventory.SetSlot(slotIndex, null);
                        UpdateDraggedIcon();
                    }
                }
                else if (_draggedStack != null)
                {
                    if (targetSlotStack == null)
                    {
                        _inventory.SetSlot(slotIndex, _draggedStack);
                        ClearDraggedItem();
                    }
                    else
                    {
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

                        ItemStack temp = targetSlotStack;
                        _inventory.SetSlot(slotIndex, _draggedStack);
                        _draggedStack = temp;
                        UpdateDraggedIcon();
                    }
                }
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                if (targetSlotStack != null)
                {
                    if (targetSlotStack.Item.IsStackable)
                    {
                        if (_draggedStack == null)
                        {
                            _draggedStack = new ItemStack(targetSlotStack.Item, 1);
                            targetSlotStack.Amount--;
                        }
                        else if (_draggedStack.Item == targetSlotStack.Item && _draggedStack.Amount < _draggedStack.Item.MaxStack)
                        {
                            _draggedStack.Amount++;
                            targetSlotStack.Amount--;
                        }

                        if (targetSlotStack.Amount <= 0)
                            _inventory.SetSlot(slotIndex, null);
                        else
                            _inventory.SetSlot(slotIndex, targetSlotStack);

                        UpdateDraggedIcon();
                    }
                    else if (_draggedStack == null && targetSlotStack.Item is AccessoryItemData)
                    {
                        if (ResolveAccessorySlots()?.TryEquipFromInventory(slotIndex) == true)
                        {
                            Refresh();
                            return;
                        }
                    }
                    else if (_draggedStack == null)
                    {
                        EquipmentSlots eq = FindFirstObjectByType<EquipmentSlots>();
                        if (eq != null) eq.EquipItem(targetSlotStack.Item);
                    }
                }
                else if (_draggedStack != null)
                {
                    _inventory.SetSlot(slotIndex, new ItemStack(_draggedStack.Item, 1));

                    _draggedStack.Amount--;
                    if (_draggedStack.Amount <= 0) ClearDraggedItem();
                    else UpdateDraggedIcon();
                }
            }
        }

        public void Refresh()
        {
            if (_inventory == null) return;

            var slots = _inventory.GetAllSlots();
            if (slots == null) return;

            if (_createdSlots != null)
            {
                for (int i = 0; i < _createdSlots.Length; i++)
                {
                    if (_createdSlots[i] != null)
                        _createdSlots[i].UpdateView(i < slots.Length ? slots[i] : null);
                }
            }

            if (_weightText != null)
                _weightText.text = $"Вага: {_inventory.GetCurrentWeight():F1} / {_inventory.GetMaxWeight():F1}";

            _craftingPanel?.Refresh();
            _storagePanel?.Refresh();
            _accessoryPanel?.Refresh();
        }

        private void InitializeCraftingPanel()
        {
            if (_inventory == null || _contextPanel == null)
                return;

            EnsureSoulAshWallet();

            if (_craftingPanel == null)
            {
                _craftingPanel = _contextPanel.GetComponent<CraftingPanelUI>();
                if (_craftingPanel == null)
                    _craftingPanel = _contextPanel.AddComponent<CraftingPanelUI>();

                _craftingPanel.Initialize(_inventory, _soulAshWallet, _currentCraftingContext);
            }
        }

        private void InitializeStoragePanel()
        {
            if (_inventory == null || _contextPanel == null)
                return;

            EnsureSoulAshWallet();

            if (_storagePanel == null)
            {
                _storagePanel = _contextPanel.GetComponentInChildren<StoragePanelUI>(true);
                if (_storagePanel == null)
                {
                    var storageObject = new GameObject("StoragePanel", typeof(RectTransform));
                    storageObject.transform.SetParent(_contextPanel.transform, false);

                    var rect = (RectTransform)storageObject.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    _storagePanel = storageObject.AddComponent<StoragePanelUI>();
                }

                _storagePanel.Initialize(_inventory, _soulAshWallet, _slotPrefab);
                _storagePanel.gameObject.SetActive(false);
            }
        }

        private void InitializeAccessoryPanel()
        {
            if (_inventory == null || _inventoryPanel == null)
                return;

            var accessories = ResolveAccessorySlots();
            if (accessories == null)
                return;

            if (_accessoryPanel == null)
            {
                _accessoryPanel = _inventoryPanel.GetComponentInChildren<AccessoryPanelUI>(true);
                if (_accessoryPanel == null)
                {
                    var accessoryObject = new GameObject("AccessoryPanel", typeof(RectTransform));
                    accessoryObject.transform.SetParent(_inventoryPanel.transform, false);

                    var rect = (RectTransform)accessoryObject.transform;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    _accessoryPanel = accessoryObject.AddComponent<AccessoryPanelUI>();
                }

                _accessoryPanel.Initialize(accessories, _slotPrefab);
            }
        }

        private AccessorySlotController ResolveAccessorySlots()
        {
            if (_accessories != null)
                return _accessories;

            if (_inventory != null)
                _accessories = _inventory.GetComponent<AccessorySlotController>() ?? _inventory.GetComponentInParent<AccessorySlotController>();

            if (_accessories == null && _inventory != null)
                _accessories = _inventory.gameObject.AddComponent<AccessorySlotController>();

            if (_accessories == null)
                _accessories = FindFirstObjectByType<AccessorySlotController>();

            return _accessories;
        }

        private void EnsureSoulAshWallet()
        {
            if (_soulAshWallet != null || _inventory == null)
                return;

            _soulAshWallet = _inventory.GetComponent<SoulAshWallet>();
            if (_soulAshWallet == null)
                _soulAshWallet = _inventory.gameObject.AddComponent<SoulAshWallet>();
        }

        private void UpdateDraggedItem()
        {
            if (_draggedStack == null || _draggedItemIcon == null || !_draggedItemIcon.gameObject.activeSelf)
                return;

            _draggedItemIcon.transform.position = UnityEngine.Input.mousePosition;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Mouse0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                DropDraggedStackToWorld();
        }

        private bool SwitchToHandCraftingIfTooFarFromInteractionTarget()
        {
            if (!IsOpen || !_hasDistanceCloseTarget)
                return false;

            if (_distanceCloseTarget == null || !_distanceCloseTarget.gameObject.activeInHierarchy)
            {
                CloseDistanceBoundContext();
                return true;
            }

            Transform observer = _distanceCloseObserver != null
                ? _distanceCloseObserver
                : (_inventory != null ? _inventory.transform : null);

            if (observer == null)
            {
                CloseDistanceBoundContext();
                return true;
            }

            float closeDistance = Mathf.Max(0.1f,
                _distanceCloseRange > 0f ? _distanceCloseRange : _defaultInteractionCloseDistance);
            float sqrDistance = (observer.position - _distanceCloseTarget.position).sqrMagnitude;
            if (sqrDistance <= closeDistance * closeDistance)
                return false;

            CloseDistanceBoundContext();
            return true;
        }

        private void CloseDistanceBoundContext()
        {
            if (_activeStorage != null)
            {
                SetInventoryOpenInternal(false, CraftingContext.Hand);
                return;
            }

            SwitchStationContextBackToHand();
        }

        private void SwitchStationContextBackToHand()
        {
            ClearDistanceCloseTarget();
            SetCraftingContext(CraftingContext.Hand);
            TooltipUI.Instance?.Hide();
        }

        private void SetDistanceCloseTarget(Transform closeTarget, Transform closeObserver, float closeDistance)
        {
            if (closeTarget == null)
            {
                ClearDistanceCloseTarget();
                return;
            }

            _distanceCloseTarget = closeTarget;
            _distanceCloseObserver = closeObserver;
            _distanceCloseRange = closeDistance > 0f ? closeDistance : _defaultInteractionCloseDistance;
            _hasDistanceCloseTarget = true;
        }

        private void ClearDistanceCloseTarget()
        {
            _distanceCloseTarget = null;
            _distanceCloseObserver = null;
            _distanceCloseRange = 0f;
            _hasDistanceCloseTarget = false;
        }

        private void DropDraggedStackToWorld()
        {
            if (_draggedStack == null || _draggedStack.Item.WorldPickupPrefab == null) return;

            Transform p = _inventory.transform;
            Vector3 pos = p.position + p.forward * 1.5f + Vector3.up * 0.5f;

            GameObject dropped = Instantiate(_draggedStack.Item.WorldPickupPrefab, pos, Quaternion.identity);
            if (dropped.TryGetComponent(out ItemPickup pickup))
                pickup.Amount = _draggedStack.Amount;

            ClearDraggedItem();
        }

        private void ReturnDraggedStackToInventory()
        {
            if (_draggedStack == null)
                return;

            _inventory.AddItem(_draggedStack.Item, _draggedStack.Amount);
            ClearDraggedItem();
        }

        private void UpdateDraggedIcon()
        {
            if (_draggedItemIcon == null)
                return;

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

        private PlayerActionGate ResolveActionGate()
        {
            if (_actionGate != null)
                return _actionGate;

            if (_inventory != null)
                _actionGate = _inventory.GetComponentInParent<PlayerActionGate>();

            if (_actionGate == null)
                _actionGate = FindFirstObjectByType<PlayerActionGate>();

            return _actionGate;
        }
    }
}
*/

using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Crafting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mainInventoryWindow; // Твоя нова головна коробка!
        [SerializeField] private GameObject _inventoryPanel;
        [SerializeField] private GameObject _contextPanel;

        [Header("Inventory")]
        [SerializeField] private Transform _slotsGrid;
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private TMP_Text _weightText;

        [Header("Drag and drop")]
        [SerializeField] private Image _draggedItemIcon;
        [SerializeField] private TMP_Text _draggedItemAmount;

        [Header("Interaction")]
        [SerializeField] private float _defaultInteractionCloseDistance = 3f;
        [SerializeField] private PlayerActionGate _actionGate;

        private InventorySlotUI[] _createdSlots;
        private ItemStack _draggedStack;
        private CraftingPanelUI _craftingPanel;
        private StoragePanelUI _storagePanel;
        private AccessoryPanelUI _accessoryPanel;
        private AccessorySlotController _accessories;
        private IItemStorage _activeStorage;
        private SoulAshWallet _soulAshWallet;
        private CraftingContext _currentCraftingContext = CraftingContext.Hand;
        private Transform _distanceCloseTarget;
        private Transform _distanceCloseObserver;
        private float _distanceCloseRange;
        private bool _hasDistanceCloseTarget;

        // Оновлена перевірка, чи відкритий інвентар
        public bool IsOpen => _mainInventoryWindow != null ? _mainInventoryWindow.activeSelf : (_inventoryPanel != null && _inventoryPanel.activeSelf);
        public bool IsStorageOpen => IsOpen && _activeStorage != null;

        private void Awake()
        {
            // Вимикаємо головне вікно на старті
            if (_mainInventoryWindow != null) _mainInventoryWindow.SetActive(false);
            else if (_inventoryPanel != null) _inventoryPanel.SetActive(false);

            if (_contextPanel != null) _contextPanel.SetActive(false);
            SetDraggedIconActive(false);
        }

        private void Start()
        {
            if (_inventory != null)
            {
                _inventory.OnInventoryChanged += Refresh;
                GenerateSlots();
                InitializeCraftingPanel();
                InitializeAccessoryPanel();
            }
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= Refresh;

            _storagePanel?.ClearStorage();
        }

        private void GenerateSlots()
        {
            if (_slotsGrid == null || _slotPrefab == null || _inventory == null) return;

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
            UpdateDraggedItem();
            if (SwitchToHandCraftingIfTooFarFromInteractionTarget())
                return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab) || UnityEngine.Input.GetKeyDown(KeyCode.I))
            {
                SetInventoryOpen(!IsOpen, CraftingContext.Hand);
            }
        }

        public void OpenWithCraftingContext(CraftingContext context)
        {
            _activeStorage = null;
            ClearDistanceCloseTarget();
            SetInventoryOpen(true, context);
        }

        public void OpenWithCraftingContext(
            CraftingContext context,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            _activeStorage = null;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, context);
        }

        public void OpenWithStorage(
            BaseResourceStorage storage,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            if (storage == null)
                return;

            _activeStorage = storage;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, CraftingContext.Hand);
        }

        public void OpenWithGeneralStorage(
            GeneralItemStorage storage,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            if (storage == null)
                return;

            _activeStorage = storage;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, CraftingContext.Hand);
        }

        public void SetCraftingContext(CraftingContext context)
        {
            _currentCraftingContext = context;
            if (_craftingPanel != null)
                _craftingPanel.SetContext(context);
        }

        public void SetInventoryOpen(bool open, CraftingContext context)
        {
            _activeStorage = null;

            if (open)
                ClearDistanceCloseTarget();

            SetInventoryOpenInternal(open, context);
        }
        private void SetInventoryOpenInternal(bool open, CraftingContext context)
        {
            if (_mainInventoryWindow != null)
            {
                _mainInventoryWindow.SetActive(open);

                if (open)
                {
                    if (_activeStorage != null)
                    {
                        // Якщо це СКРИНЯ: показуємо і інвентар, і саму скриню (обидві панелі)
                        if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
                        if (_contextPanel != null) _contextPanel.SetActive(true);
                    }
                    else if (context != CraftingContext.Hand)
                    {
                        // Якщо це ВЕРСТАК (Campfire, Workbench і т.д.): автоматично вмикаємо вкладку Крафт
                        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
                        if (_contextPanel != null) _contextPanel.SetActive(true);
                    }
                    else
                    {
                        // Якщо просто натиснули TAB у полі: показуємо базовий Інвентар
                        if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
                        if (_contextPanel != null) _contextPanel.SetActive(false);
                    }
                }
            }
            else // Резервний варіант, якщо коробки немає
            {
                if (_inventoryPanel != null) _inventoryPanel.SetActive(open);
                if (_contextPanel != null) _contextPanel.SetActive(open);
            }

            if (_inventoryPanel == null && _mainInventoryWindow == null)
                return;

            _currentCraftingContext = context;
            ResolveActionGate()?.SetInventoryOpen(open);

            if (open)
            {
                InitializeCraftingPanel();
                InitializeStoragePanel();
                InitializeAccessoryPanel();

                bool storageMode = _activeStorage != null;
                _craftingPanel?.SetPanelVisible(!storageMode);

                if (storageMode)
                {
                    _storagePanel?.SetStorage(_activeStorage);
                }
                else
                {
                    _storagePanel?.ClearStorage();
                    _craftingPanel?.SetContext(_currentCraftingContext);
                }

                Refresh();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                ClearDistanceCloseTarget();
                ReturnDraggedStackToInventory();
                _storagePanel?.ClearStorage();
                _craftingPanel?.SetPanelVisible(true);
                _activeStorage = null;
                TooltipUI.Instance?.Hide();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        /*private void SetInventoryOpenInternal(bool open, CraftingContext context)
        {
            // ЛОГІКА ДЛЯ НОВОГО ІНТЕРФЕЙСУ З КОРОБКОЮ
            if (_mainInventoryWindow != null)
            {
                _mainInventoryWindow.SetActive(open);

                // Якщо ми просто відкриваємо меню (не скриню) - показуємо тільки інвентар
                if (open && _activeStorage == null)
                {
                    if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
                    if (_contextPanel != null) _contextPanel.SetActive(false);
                }
            }
            else // Якщо коробки ще немає, працюємо по-старому
            {
                if (_inventoryPanel != null) _inventoryPanel.SetActive(open);
                if (_contextPanel != null) _contextPanel.SetActive(open);
            }

            // Якщо ми відкрили СКРИНЮ, примусово показуємо обидві панелі
            if (open && _activeStorage != null)
            {
                if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
                if (_contextPanel != null) _contextPanel.SetActive(true);
            }

            if (_inventoryPanel == null && _mainInventoryWindow == null)
                return;

            _currentCraftingContext = context;
            ResolveActionGate()?.SetInventoryOpen(open);

            if (open)
            {
                InitializeCraftingPanel();
                InitializeStoragePanel();
                InitializeAccessoryPanel();

                bool storageMode = _activeStorage != null;
                _craftingPanel?.SetPanelVisible(!storageMode);
                if (storageMode)
                    _storagePanel?.SetStorage(_activeStorage);
                else
                {
                    _storagePanel?.ClearStorage();
                    _craftingPanel?.SetContext(_currentCraftingContext);
                }

                Refresh();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                ClearDistanceCloseTarget();
                ReturnDraggedStackToInventory();
                _storagePanel?.ClearStorage();
                _craftingPanel?.SetPanelVisible(true);
                _activeStorage = null;
                TooltipUI.Instance?.Hide();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }*/

        public void OnSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            ItemStack targetSlotStack = _inventory.GetSlot(slotIndex);

            if (_activeStorage != null
                && button == PointerEventData.InputButton.Right
                && _draggedStack == null
                && targetSlotStack != null)
            {
                _activeStorage.TryDepositFromInventory(_inventory, slotIndex, int.MaxValue);
                Refresh();
                return;
            }

            if (button == PointerEventData.InputButton.Left)
            {
                if (_draggedStack == null && targetSlotStack != null)
                {
                    if (Input.GetKey(KeyCode.LeftShift) && targetSlotStack.Item.IsStackable && targetSlotStack.Amount > 1)
                    {
                        int takeAmount = targetSlotStack.Amount / 2;
                        int leaveAmount = targetSlotStack.Amount - takeAmount;

                        _draggedStack = new ItemStack(targetSlotStack.Item, takeAmount);
                        targetSlotStack.Amount = leaveAmount;
                        _inventory.SetSlot(slotIndex, targetSlotStack);
                        UpdateDraggedIcon();
                    }
                    else
                    {
                        _draggedStack = targetSlotStack;
                        _inventory.SetSlot(slotIndex, null);
                        UpdateDraggedIcon();
                    }
                }
                else if (_draggedStack != null)
                {
                    if (targetSlotStack == null)
                    {
                        _inventory.SetSlot(slotIndex, _draggedStack);
                        ClearDraggedItem();
                    }
                    else
                    {
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

                        ItemStack temp = targetSlotStack;
                        _inventory.SetSlot(slotIndex, _draggedStack);
                        _draggedStack = temp;
                        UpdateDraggedIcon();
                    }
                }
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                if (targetSlotStack != null)
                {
                    if (targetSlotStack.Item.IsStackable)
                    {
                        if (_draggedStack == null)
                        {
                            _draggedStack = new ItemStack(targetSlotStack.Item, 1);
                            targetSlotStack.Amount--;
                        }
                        else if (_draggedStack.Item == targetSlotStack.Item && _draggedStack.Amount < _draggedStack.Item.MaxStack)
                        {
                            _draggedStack.Amount++;
                            targetSlotStack.Amount--;
                        }

                        if (targetSlotStack.Amount <= 0)
                            _inventory.SetSlot(slotIndex, null);
                        else
                            _inventory.SetSlot(slotIndex, targetSlotStack);

                        UpdateDraggedIcon();
                    }
                    else if (_draggedStack == null && targetSlotStack.Item is AccessoryItemData)
                    {
                        if (ResolveAccessorySlots()?.TryEquipFromInventory(slotIndex) == true)
                        {
                            Refresh();
                            return;
                        }
                    }
                    else if (_draggedStack == null)
                    {
                        EquipmentSlots eq = FindFirstObjectByType<EquipmentSlots>();
                        if (eq != null) eq.EquipItem(targetSlotStack.Item);
                    }
                }
                else if (_draggedStack != null)
                {
                    _inventory.SetSlot(slotIndex, new ItemStack(_draggedStack.Item, 1));

                    _draggedStack.Amount--;
                    if (_draggedStack.Amount <= 0) ClearDraggedItem();
                    else UpdateDraggedIcon();
                }
            }
        }

        public void Refresh()
        {
            if (_inventory == null) return;

            var slots = _inventory.GetAllSlots();
            if (slots == null) return;

            if (_createdSlots != null)
            {
                for (int i = 0; i < _createdSlots.Length; i++)
                {
                    if (_createdSlots[i] != null)
                        _createdSlots[i].UpdateView(i < slots.Length ? slots[i] : null);
                }
            }

            if (_weightText != null)
                _weightText.text = $"Вага: {_inventory.GetCurrentWeight():F1} / {_inventory.GetMaxWeight():F1}";

            _craftingPanel?.Refresh();
            _storagePanel?.Refresh();
            _accessoryPanel?.Refresh();
        }

        private void InitializeCraftingPanel()
        {
            if (_inventory == null || _contextPanel == null)
                return;

            EnsureSoulAshWallet();

            if (_craftingPanel == null)
            {
                _craftingPanel = _contextPanel.GetComponent<CraftingPanelUI>();
                if (_craftingPanel == null)
                    _craftingPanel = _contextPanel.AddComponent<CraftingPanelUI>();

                _craftingPanel.Initialize(_inventory, _soulAshWallet, _currentCraftingContext);
            }
        }

        private void InitializeStoragePanel()
        {
            if (_inventory == null || _contextPanel == null)
                return;

            EnsureSoulAshWallet();

            if (_storagePanel == null)
            {
                _storagePanel = _contextPanel.GetComponentInChildren<StoragePanelUI>(true);
                if (_storagePanel == null)
                {
                    var storageObject = new GameObject("StoragePanel", typeof(RectTransform));
                    storageObject.transform.SetParent(_contextPanel.transform, false);

                    var rect = (RectTransform)storageObject.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    _storagePanel = storageObject.AddComponent<StoragePanelUI>();
                }

                _storagePanel.Initialize(_inventory, _soulAshWallet, _slotPrefab);
                _storagePanel.gameObject.SetActive(false);
            }
        }

        private void InitializeAccessoryPanel()
        {
            if (_inventory == null || _inventoryPanel == null)
                return;

            var accessories = ResolveAccessorySlots();
            if (accessories == null)
                return;

            if (_accessoryPanel == null)
            {
                _accessoryPanel = _inventoryPanel.GetComponentInChildren<AccessoryPanelUI>(true);
                if (_accessoryPanel == null)
                {
                    var accessoryObject = new GameObject("AccessoryPanel", typeof(RectTransform));
                    accessoryObject.transform.SetParent(_inventoryPanel.transform, false);

                    var rect = (RectTransform)accessoryObject.transform;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    _accessoryPanel = accessoryObject.AddComponent<AccessoryPanelUI>();
                }

                _accessoryPanel.Initialize(accessories, _slotPrefab);
            }
        }

        private AccessorySlotController ResolveAccessorySlots()
        {
            if (_accessories != null)
                return _accessories;

            if (_inventory != null)
                _accessories = _inventory.GetComponent<AccessorySlotController>() ?? _inventory.GetComponentInParent<AccessorySlotController>();

            if (_accessories == null && _inventory != null)
                _accessories = _inventory.gameObject.AddComponent<AccessorySlotController>();

            if (_accessories == null)
                _accessories = FindFirstObjectByType<AccessorySlotController>();

            return _accessories;
        }

        private void EnsureSoulAshWallet()
        {
            if (_soulAshWallet != null || _inventory == null)
                return;

            _soulAshWallet = _inventory.GetComponent<SoulAshWallet>();
            if (_soulAshWallet == null)
                _soulAshWallet = _inventory.gameObject.AddComponent<SoulAshWallet>();
        }

        private void UpdateDraggedItem()
        {
            if (_draggedStack == null || _draggedItemIcon == null || !_draggedItemIcon.gameObject.activeSelf)
                return;

            _draggedItemIcon.transform.position = UnityEngine.Input.mousePosition;

            if (UnityEngine.Input.GetKeyDown(KeyCode.Mouse0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                DropDraggedStackToWorld();
        }

        private bool SwitchToHandCraftingIfTooFarFromInteractionTarget()
        {
            if (!IsOpen || !_hasDistanceCloseTarget)
                return false;

            if (_distanceCloseTarget == null || !_distanceCloseTarget.gameObject.activeInHierarchy)
            {
                CloseDistanceBoundContext();
                return true;
            }

            Transform observer = _distanceCloseObserver != null
                ? _distanceCloseObserver
                : (_inventory != null ? _inventory.transform : null);

            if (observer == null)
            {
                CloseDistanceBoundContext();
                return true;
            }

            float closeDistance = Mathf.Max(0.1f,
                _distanceCloseRange > 0f ? _distanceCloseRange : _defaultInteractionCloseDistance);
            float sqrDistance = (observer.position - _distanceCloseTarget.position).sqrMagnitude;
            if (sqrDistance <= closeDistance * closeDistance)
                return false;

            CloseDistanceBoundContext();
            return true;
        }

        private void CloseDistanceBoundContext()
        {
            if (_activeStorage != null)
            {
                SetInventoryOpenInternal(false, CraftingContext.Hand);
                return;
            }

            SwitchStationContextBackToHand();
        }

        private void SwitchStationContextBackToHand()
        {
            ClearDistanceCloseTarget();
            SetCraftingContext(CraftingContext.Hand);
            TooltipUI.Instance?.Hide();
        }

        private void SetDistanceCloseTarget(Transform closeTarget, Transform closeObserver, float closeDistance)
        {
            if (closeTarget == null)
            {
                ClearDistanceCloseTarget();
                return;
            }

            _distanceCloseTarget = closeTarget;
            _distanceCloseObserver = closeObserver;
            _distanceCloseRange = closeDistance > 0f ? closeDistance : _defaultInteractionCloseDistance;
            _hasDistanceCloseTarget = true;
        }

        private void ClearDistanceCloseTarget()
        {
            _distanceCloseTarget = null;
            _distanceCloseObserver = null;
            _distanceCloseRange = 0f;
            _hasDistanceCloseTarget = false;
        }

        private void DropDraggedStackToWorld()
        {
            if (_draggedStack == null || _draggedStack.Item.WorldPickupPrefab == null) return;

            Transform p = _inventory.transform;
            Vector3 pos = p.position + p.forward * 1.5f + Vector3.up * 0.5f;

            GameObject dropped = Instantiate(_draggedStack.Item.WorldPickupPrefab, pos, Quaternion.identity);
            if (dropped.TryGetComponent(out ItemPickup pickup))
                pickup.Amount = _draggedStack.Amount;

            ClearDraggedItem();
        }

        private void ReturnDraggedStackToInventory()
        {
            if (_draggedStack == null)
                return;

            _inventory.AddItem(_draggedStack.Item, _draggedStack.Amount);
            ClearDraggedItem();
        }

        private void UpdateDraggedIcon()
        {
            if (_draggedItemIcon == null)
                return;

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

        private PlayerActionGate ResolveActionGate()
        {
            if (_actionGate != null)
                return _actionGate;

            if (_inventory != null)
                _actionGate = _inventory.GetComponentInParent<PlayerActionGate>();

            if (_actionGate == null)
                _actionGate = FindFirstObjectByType<PlayerActionGate>();

            return _actionGate;
        }
    }
}