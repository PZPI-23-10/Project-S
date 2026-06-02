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
        [SerializeField] private GameObject _mainInventoryWindow;
        [SerializeField] private GameObject _inventoryPanel;
        [SerializeField] private GameObject _contextPanel;
        [SerializeField] private GameObject _lootingPanel;
        [SerializeField] private GameObject _tabButtons;

        [Header("Inventory")]
        [SerializeField] private Transform _slotsGrid; // Основна сітка рюкзака
        [SerializeField] private Transform _hotbarGrid; // НОВЕ: Сітка для хотбару в інвентарі
        [SerializeField] private int _hotbarSize = 6;   // НОВЕ: Кількість слотів у хотбарі
        [SerializeField] private InventorySlotUI _slotPrefab;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private TMP_Text _weightText;

        [Header("HUD Elements")]
        [Tooltip("Текст попередження 'У тебе перегруз!' на екрані")]
        [SerializeField] private TMP_Text _overloadHUDText;
        [Tooltip("Об'єкт прицілу (крапка по центру)")]
        [SerializeField] private GameObject _crosshairDot;

        [Header("Drag and drop")]
        [SerializeField] private Image _draggedItemIcon;
        [SerializeField] private TMP_Text _draggedItemAmount;

        [Header("Interaction")]
        [SerializeField] private float _defaultInteractionCloseDistance = 3f;
        [SerializeField] private PlayerActionGate _actionGate;

        private InventorySlotUI[] _createdSlots;
        private ItemStack _draggedStack;
        private DragSourceType _dragSourceType;
        private int _dragSourceSlotIndex = -1;
        private IItemStorage _dragSourceStorage;
        private CraftingPanelUI _craftingPanel;
        private StoragePanelUI _storagePanel;
        private AccessoryPanelUI _accessoryPanel;
        private AccessorySlotController _accessories;
        private IItemStorage _activeStorage;
        private ICraftingRecipeProvider _activeCraftingRecipeProvider;
        private SoulAshWallet _soulAshWallet;
        private CraftingContext _currentCraftingContext = CraftingContext.Hand;
        private Transform _distanceCloseTarget;
        private Transform _distanceCloseObserver;
        private float _distanceCloseRange;
        private bool _hasDistanceCloseTarget;

        private enum DragSourceType
        {
            None,
            PlayerInventory,
            ExternalStorage
        }

        public bool IsOpen => _mainInventoryWindow != null ? _mainInventoryWindow.activeSelf : (_inventoryPanel != null && _inventoryPanel.activeSelf);
        public bool IsStorageOpen => IsOpen && _activeStorage != null;

        private void Awake()
        {
            if (_mainInventoryWindow != null) _mainInventoryWindow.SetActive(false);
            else if (_inventoryPanel != null) _inventoryPanel.SetActive(false);

            if (_contextPanel != null) _contextPanel.SetActive(false);
            SetDraggedIconActive(false);

            if (_overloadHUDText != null) _overloadHUDText.gameObject.SetActive(false);
            if (_crosshairDot != null) _crosshairDot.SetActive(true);
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

            // Очищаємо обидві сітки
            foreach (Transform child in _slotsGrid) Destroy(child.gameObject);
            if (_hotbarGrid != null)
            {
                foreach (Transform child in _hotbarGrid) Destroy(child.gameObject);
            }

            int size = _inventory.GetSize();
            _createdSlots = new InventorySlotUI[size];

            for (int i = 0; i < size; i++)
            {
                // МАГІЯ ТУТ: Якщо індекс менший за розмір хотбару, кидаємо слот у сітку хотбару. Інакше - в основну.
                Transform targetGrid = (_hotbarGrid != null && i < _hotbarSize) ? _hotbarGrid : _slotsGrid;

                InventorySlotUI newSlot = Instantiate(_slotPrefab, targetGrid);
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
            _activeCraftingRecipeProvider = null;
            ClearDistanceCloseTarget();
            SetInventoryOpen(true, context);
        }

        public void OpenWithCraftingContext(CraftingContext context, ICraftingRecipeProvider recipeProvider)
        {
            _activeStorage = null;
            _activeCraftingRecipeProvider = recipeProvider;
            ClearDistanceCloseTarget();
            SetInventoryOpenInternal(true, context);
        }

        public void OpenWithCraftingContext(
            CraftingContext context,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            OpenWithCraftingContext(context, closeTarget, closeObserver, closeDistance, null);
        }

        public void OpenWithCraftingContext(
            CraftingContext context,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance,
            ICraftingRecipeProvider recipeProvider)
        {
            _activeStorage = null;
            _activeCraftingRecipeProvider = recipeProvider;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, context);
        }

        public void OpenWithStorage(
            IItemStorage storage,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            if (storage == null)
                return;

            _activeStorage = storage;
            _activeCraftingRecipeProvider = null;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, CraftingContext.Hand);
        }

        public void OpenWithStorage(
            BaseResourceStorage storage,
            Transform closeTarget,
            Transform closeObserver,
            float closeDistance)
        {
            OpenWithStorage((IItemStorage)storage, closeTarget, closeObserver, closeDistance);
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
            _activeCraftingRecipeProvider = null;
            SetDistanceCloseTarget(closeTarget, closeObserver, closeDistance);
            SetInventoryOpenInternal(true, CraftingContext.Hand);
        }

        public void SetCraftingContext(CraftingContext context)
        {
            _activeCraftingRecipeProvider = null;
            _currentCraftingContext = context;
            if (_craftingPanel != null)
                _craftingPanel.SetContext(context);
        }

        public void SetInventoryOpen(bool open, CraftingContext context)
        {
            _activeStorage = null;
            _activeCraftingRecipeProvider = null;

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
                    RectTransform mainWinRect = _mainInventoryWindow.GetComponent<RectTransform>();

                    if (_activeStorage != null)
                    {
                        if (mainWinRect != null) mainWinRect.anchoredPosition = new Vector2(-515f, mainWinRect.anchoredPosition.y);

                        if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
                        if (_contextPanel != null) _contextPanel.SetActive(false);
                        if (_lootingPanel != null) _lootingPanel.SetActive(true);
                        if (_tabButtons != null) _tabButtons.SetActive(false); // <--- ХОВАЄМО
                    }
                    else if (context != CraftingContext.Hand)
                    {
                        if (mainWinRect != null) mainWinRect.anchoredPosition = new Vector2(0f, mainWinRect.anchoredPosition.y);

                        if (_inventoryPanel != null) _inventoryPanel.SetActive(false);
                        if (_contextPanel != null) _contextPanel.SetActive(true);
                        if (_lootingPanel != null) _lootingPanel.SetActive(false);
                        if (_tabButtons != null) _tabButtons.SetActive(true); // <--- ПОКАЗУЄМО
                    }
                    else
                    {
                        if (mainWinRect != null) mainWinRect.anchoredPosition = new Vector2(0f, mainWinRect.anchoredPosition.y);

                        if (_inventoryPanel != null) _inventoryPanel.SetActive(true);
                        if (_contextPanel != null) _contextPanel.SetActive(false);
                        if (_lootingPanel != null) _lootingPanel.SetActive(false);
                        if (_tabButtons != null) _tabButtons.SetActive(true);
                    }
                }
            }
            else
            {
                ClearDistanceCloseTarget();

                RectTransform mainWinRect = _mainInventoryWindow.GetComponent<RectTransform>();
                if (mainWinRect != null) mainWinRect.anchoredPosition = new Vector2(0f, mainWinRect.anchoredPosition.y);

                if (_lootingPanel != null) _lootingPanel.SetActive(false);

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
                    _craftingPanel.SetContext(_currentCraftingContext);
                }

                Refresh();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (_crosshairDot != null) _crosshairDot.SetActive(false);
            }
            else
            {
                ClearDistanceCloseTarget();
                ReturnDraggedStackToSource();
                _storagePanel?.ClearStorage();
                _craftingPanel?.SetPanelVisible(true);
                _activeStorage = null;
                _activeCraftingRecipeProvider = null;
                TooltipUI.Instance?.Hide();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (_lootingPanel != null) _lootingPanel.SetActive(false);
                if (_crosshairDot != null) _crosshairDot.SetActive(true);
            }

            UpdateOverloadHUDVisibility(open);
        }

        public void OnSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (_inventory == null) return;

            ItemStack targetSlotStack = _inventory.GetSlot(slotIndex);

            // 1. Правий клік у скрині - перекинути все
            if (_activeStorage != null && button == PointerEventData.InputButton.Right && _draggedStack == null && targetSlotStack != null)
            {
                _activeStorage.TryDepositFromInventory(_inventory, slotIndex, int.MaxValue);
                Refresh();
                return;
            }

            // 2. SHIFT + Лівий клік у скрині - швидке перекидання в скриню
            if (_activeStorage != null && button == PointerEventData.InputButton.Left && IsShiftDown() && _draggedStack == null && targetSlotStack != null)
            {
                _activeStorage.TryDepositFromInventory(_inventory, slotIndex, targetSlotStack.Amount);
                Refresh();
                return;
            }

            // 3. SHIFT + Лівий клік БЕЗ скрині - швидке перекидання між хотбаром і рюкзаком (Minecraft style)
            if (_activeStorage == null && button == PointerEventData.InputButton.Left && IsShiftDown() && _draggedStack == null && targetSlotStack != null)
            {
                QuickTransferWithinInventory(slotIndex);
                return;
            }

            // 4. Звичайні кліки по слотах
            if (button == PointerEventData.InputButton.Left)
            {
                if (_draggedStack == null && targetSlotStack != null)
                {
                    // CTRL + Клік - розділити навпіл!
                    if (IsCtrlDown() && targetSlotStack.Item.IsStackable && targetSlotStack.Amount > 1)
                    {
                        BeginDragFromInventory(slotIndex, targetSlotStack.Amount / 2);
                    }
                    else
                    {
                        BeginDragFromInventory(slotIndex, targetSlotStack.Amount);
                    }
                }
                else if (_draggedStack != null)
                {
                    TryPlaceDraggedIntoInventorySlot(slotIndex, _draggedStack.Amount);
                }
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                // Правий клік - взяти/покласти 1 шт, або екіпірувати
                if (targetSlotStack != null)
                {
                    if (targetSlotStack.Item.IsStackable)
                    {
                        if (_draggedStack == null)
                            BeginDragFromInventory(slotIndex, 1);
                        else if (_draggedStack.Item == targetSlotStack.Item && _draggedStack.Amount < _draggedStack.Item.MaxStack)
                            TryPlaceDraggedIntoInventorySlot(slotIndex, 1);
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
                    TryPlaceDraggedIntoInventorySlot(slotIndex, 1);
                }
            }
        }

        public void OnStorageSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (_activeStorage == null || _inventory == null)
                return;

            ItemStack targetSlotStack = _activeStorage.GetSlot(slotIndex);

            if (button == PointerEventData.InputButton.Left)
            {
                if (IsShiftDown() && _draggedStack == null && targetSlotStack != null)
                {
                    _activeStorage.TryWithdrawToInventory(slotIndex, targetSlotStack.Amount, _inventory);
                    Refresh();
                    return;
                }

                if (_draggedStack == null && targetSlotStack != null)
                    BeginDragFromStorage(slotIndex, targetSlotStack.Amount);
                else if (_draggedStack != null)
                    TryPlaceDraggedIntoStorageSlot(slotIndex, _draggedStack.Amount);
            }
            else if (button == PointerEventData.InputButton.Right)
            {
                if (_draggedStack == null && targetSlotStack != null)
                {
                    _activeStorage.TryWithdrawToInventory(slotIndex, 1, _inventory);
                    Refresh();
                    return;
                }

                if (_draggedStack != null)
                    TryPlaceDraggedIntoStorageSlot(slotIndex, 1);
            }
        }

        private void BeginDragFromInventory(int slotIndex, int amount)
        {
            var slot = _inventory.GetSlot(slotIndex);
            if (slot == null || slot.Item == null || amount <= 0)
                return;

            int take = Mathf.Min(amount, slot.Amount);
            ItemStack dragged = take >= slot.Amount
                ? slot
                : new ItemStack(slot.Item, take);

            if (take >= slot.Amount)
                _inventory.SetSlot(slotIndex, null);
            else
            {
                slot.Amount -= take;
                _inventory.SetSlot(slotIndex, slot);
            }

            SetDraggedStack(dragged, DragSourceType.PlayerInventory, slotIndex, null);
        }

        private void BeginDragFromStorage(int slotIndex, int amount)
        {
            if (_activeStorage == null)
                return;

            var slot = _activeStorage.GetSlot(slotIndex);
            if (slot == null || slot.Item == null || amount <= 0)
                return;

            int take = Mathf.Min(amount, slot.Amount);
            ItemStack dragged = take >= slot.Amount
                ? slot
                : new ItemStack(slot.Item, take);

            if (take >= slot.Amount)
                _activeStorage.SetSlot(slotIndex, null);
            else
            {
                slot.Amount -= take;
                _activeStorage.SetSlot(slotIndex, slot);
            }

            SetDraggedStack(dragged, DragSourceType.ExternalStorage, slotIndex, _activeStorage);
        }

        private bool TryPlaceDraggedIntoInventorySlot(int slotIndex, int amount)
        {
            if (_draggedStack == null || _inventory == null || amount <= 0)
                return false;

            var target = _inventory.GetSlot(slotIndex);
            int moveAmount = Mathf.Min(amount, _draggedStack.Amount);

            if (target == null || target.Item == null)
            {
                _inventory.SetSlot(slotIndex, new ItemStack(_draggedStack.Item, moveAmount));
                ReduceDraggedStack(moveAmount);
                Refresh();
                return true;
            }

            if (target.Item == _draggedStack.Item && target.Item.IsStackable)
            {
                int space = Mathf.Max(0, target.Item.MaxStack - target.Amount);
                int toAdd = Mathf.Min(space, moveAmount);
                if (toAdd <= 0)
                    return false;

                target.Amount += toAdd;
                _inventory.SetSlot(slotIndex, target);
                ReduceDraggedStack(toAdd);
                Refresh();
                return true;
            }

            if (moveAmount != _draggedStack.Amount)
                return false;

            ItemStack swapped = target;
            _inventory.SetSlot(slotIndex, _draggedStack);
            SetDraggedStack(swapped, DragSourceType.PlayerInventory, slotIndex, null);
            Refresh();
            return true;
        }

        private bool TryPlaceDraggedIntoStorageSlot(int slotIndex, int amount)
        {
            if (_draggedStack == null || _activeStorage == null || amount <= 0)
                return false;

            if (!_activeStorage.CanStoreItem(_draggedStack.Item))
                return false;

            var target = _activeStorage.GetSlot(slotIndex);
            int moveAmount = Mathf.Min(amount, _draggedStack.Amount);

            if (target == null || target.Item == null)
            {
                _activeStorage.SetSlot(slotIndex, new ItemStack(_draggedStack.Item, moveAmount));
                ReduceDraggedStack(moveAmount);
                Refresh();
                return true;
            }

            if (target.Item == _draggedStack.Item && target.Item.IsStackable)
            {
                int space = Mathf.Max(0, target.Item.MaxStack - target.Amount);
                int toAdd = Mathf.Min(space, moveAmount);
                if (toAdd <= 0)
                    return false;

                target.Amount += toAdd;
                _activeStorage.SetSlot(slotIndex, target);
                ReduceDraggedStack(toAdd);
                Refresh();
                return true;
            }

            if (moveAmount != _draggedStack.Amount)
                return false;

            ItemStack swapped = target;
            _activeStorage.SetSlot(slotIndex, _draggedStack);
            SetDraggedStack(swapped, DragSourceType.ExternalStorage, slotIndex, _activeStorage);
            Refresh();
            return true;
        }

        private void SetDraggedStack(ItemStack stack, DragSourceType sourceType, int sourceSlotIndex, IItemStorage sourceStorage)
        {
            _draggedStack = stack;
            _dragSourceType = sourceType;
            _dragSourceSlotIndex = sourceSlotIndex;
            _dragSourceStorage = sourceStorage;
            UpdateDraggedIcon();
        }

        private void ReduceDraggedStack(int amount)
        {
            if (_draggedStack == null)
                return;

            _draggedStack.Amount -= amount;
            if (_draggedStack.Amount <= 0)
                ClearDraggedItem();
            else
                UpdateDraggedIcon();
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
            {
                float currentWeight = _inventory.GetCurrentWeight();
                float maxWeight = _inventory.GetMaxWeight();
                _weightText.text = $"Вага: {currentWeight:F1} / {maxWeight:F1}";

                if (currentWeight > maxWeight)
                    _weightText.color = new Color(1f, 0.3f, 0.3f);
                else
                    _weightText.color = Color.white;
            }

            _craftingPanel?.Refresh();
            _storagePanel?.Refresh();
            _accessoryPanel?.Refresh();

            UpdateOverloadHUDVisibility(IsOpen);
        }

        private void UpdateOverloadHUDVisibility(bool isInventoryOpen)
        {
            if (_overloadHUDText == null || _inventory == null) return;
            bool isOverloaded = _inventory.GetCurrentWeight() > _inventory.GetMaxWeight();
            _overloadHUDText.gameObject.SetActive(!isInventoryOpen && isOverloaded);
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
            if (_inventory == null || _lootingPanel == null)
                return;

            EnsureSoulAshWallet();

            if (_storagePanel == null)
            {
                _storagePanel = _lootingPanel.GetComponentInChildren<StoragePanelUI>(true);
                if (_storagePanel == null)
                {
                    var storageObject = new GameObject("StoragePanel", typeof(RectTransform));
                    storageObject.transform.SetParent(_lootingPanel.transform, false);

                    var rect = (RectTransform)storageObject.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;

                    _storagePanel = storageObject.AddComponent<StoragePanelUI>();
                }

                _storagePanel.Initialize(_inventory, _soulAshWallet, _slotPrefab, this);
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
            if (_draggedStack == null || _draggedStack.Item == null) return;

            Transform p = _inventory.transform;
            Vector3 pos = p.position + p.forward * 1.5f + Vector3.up * 0.5f;

            WorldItemDropUtility.SpawnPickup(_draggedStack.Item, _draggedStack.Amount, pos, 0.5f);

            ClearDraggedItem();
        }

        private void ReturnDraggedStackToInventory()
        {
            if (_draggedStack == null)
                return;

            _inventory.AddItem(_draggedStack.Item, _draggedStack.Amount);
            ClearDraggedItem();
        }

        private void ReturnDraggedStackToSource()
        {
            if (_draggedStack == null)
                return;

            ItemStack stack = _draggedStack;
            DragSourceType sourceType = _dragSourceType;
            int sourceSlotIndex = _dragSourceSlotIndex;
            IItemStorage sourceStorage = _dragSourceStorage;

            ClearDraggedItem();

            if (sourceType == DragSourceType.ExternalStorage
                && sourceStorage != null
                && sourceStorage.CanStoreItem(stack.Item))
            {
                var sourceSlot = sourceStorage.GetSlot(sourceSlotIndex);
                if (sourceSlot == null || sourceSlot.Item == null)
                {
                    sourceStorage.SetSlot(sourceSlotIndex, stack);
                    return;
                }

                if (sourceSlot.Item == stack.Item && sourceSlot.Item.IsStackable)
                {
                    int space = Mathf.Max(0, sourceSlot.Item.MaxStack - sourceSlot.Amount);
                    int toAdd = Mathf.Min(space, stack.Amount);
                    if (toAdd > 0)
                    {
                        sourceSlot.Amount += toAdd;
                        stack.Amount -= toAdd;
                        sourceStorage.SetSlot(sourceSlotIndex, sourceSlot);
                    }
                }

                if (stack.Amount <= 0)
                    return;

                if (sourceStorage.CanAddItem(stack.Item, stack.Amount) && sourceStorage.AddItem(stack.Item, stack.Amount))
                    return;
            }

            if (_inventory == null)
                return;

            if (sourceType == DragSourceType.PlayerInventory)
            {
                var sourceSlot = _inventory.GetSlot(sourceSlotIndex);
                if (sourceSlot == null || sourceSlot.Item == null)
                {
                    _inventory.SetSlot(sourceSlotIndex, stack);
                    return;
                }
            }

            _inventory.AddItem(stack.Item, stack.Amount);
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
            _dragSourceType = DragSourceType.None;
            _dragSourceSlotIndex = -1;
            _dragSourceStorage = null;
            SetDraggedIconActive(false);
        }

        private static bool IsShiftDown()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
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

        // Перевірка на натиснутий CTRL
        private static bool IsCtrlDown()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        // Логіка швидкого перекидання між Хотбаром і Рюкзаком
        private void QuickTransferWithinInventory(int sourceSlotIndex)
        {
            var sourceSlot = _inventory.GetSlot(sourceSlotIndex);
            if (sourceSlot == null || sourceSlot.Item == null) return;

            // Якщо клікнули по хотбару - шукаємо місце в рюкзаку. Інакше - шукаємо в хотбарі.
            bool isHotbar = sourceSlotIndex < _hotbarSize;
            int startIndex = isHotbar ? _hotbarSize : 0;
            int endIndex = isHotbar ? _inventory.GetSize() : _hotbarSize;

            // 1. Спочатку пробуємо докинути в існуючі неповні стаки
            for (int i = startIndex; i < endIndex; i++)
            {
                var targetSlot = _inventory.GetSlot(i);
                if (targetSlot != null && targetSlot.Item == sourceSlot.Item && targetSlot.Item.IsStackable)
                {
                    int space = Mathf.Max(0, targetSlot.Item.MaxStack - targetSlot.Amount);
                    int toAdd = Mathf.Min(space, sourceSlot.Amount);

                    if (toAdd > 0)
                    {
                        targetSlot.Amount += toAdd;
                        sourceSlot.Amount -= toAdd;
                        _inventory.SetSlot(i, targetSlot);

                        if (sourceSlot.Amount <= 0)
                        {
                            _inventory.SetSlot(sourceSlotIndex, null);
                            Refresh();
                            return;
                        }
                    }
                }
            }

            // 2. Якщо залишилися предмети, шукаємо першу повністю вільну комірку
            for (int i = startIndex; i < endIndex; i++)
            {
                var targetSlot = _inventory.GetSlot(i);
                if (targetSlot == null || targetSlot.Item == null)
                {
                    _inventory.SetSlot(i, new ItemStack(sourceSlot.Item, sourceSlot.Amount));
                    _inventory.SetSlot(sourceSlotIndex, null);
                    Refresh();
                    return;
                }
            }

            // 3. Зберігаємо залишок, якщо місця не вистачило
            _inventory.SetSlot(sourceSlotIndex, sourceSlot);
            Refresh();
        }
    }
}
