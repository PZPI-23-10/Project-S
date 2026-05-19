using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class StoragePanelUI : MonoBehaviour
    {
        private readonly List<InventorySlotUI> _slotViews = new List<InventorySlotUI>();

        private InventoryController _inventory;
        private SoulAshWallet _wallet;
        private IItemStorage _storage;
        private BaseResourceStorage _baseStorage;
        private InventorySlotUI _slotPrefab;

        private Transform _slotRoot;
        private TMP_Text _titleText;
        private TMP_Text _soulAshText;
        private GameObject _soulAshRow;
        private Button _depositResourcesButton;
        private TMP_Text _depositResourcesButtonText;
        private Button _depositSoulAshButton;
        private Button _withdrawSoulAshButton;
        private bool _built;

        public bool HasStorage => _storage != null;

        public void Initialize(InventoryController inventory, SoulAshWallet wallet, InventorySlotUI slotPrefab)
        {
            if (_wallet != null)
                _wallet.Changed -= OnWalletChanged;

            _inventory = inventory;
            _wallet = wallet;
            _slotPrefab = slotPrefab;

            if (_wallet != null)
                _wallet.Changed += OnWalletChanged;

            BuildLayout();
            Refresh();
        }

        public void SetStorage(IItemStorage storage)
        {
            if (_storage == storage)
            {
                gameObject.SetActive(storage != null);
                Refresh();
                return;
            }

            if (_storage != null)
                _storage.Changed -= OnStorageChanged;

            _storage = storage;
            _baseStorage = storage as BaseResourceStorage;

            if (_storage != null)
                _storage.Changed += OnStorageChanged;

            gameObject.SetActive(_storage != null);
            Refresh();
        }

        public void ClearStorage()
        {
            SetStorage(null);
        }

        public void Refresh()
        {
            if (!_built)
                return;

            bool hasStorage = _storage != null;
            if (_titleText != null)
                _titleText.text = hasStorage ? _storage.InteractionPrompt : "Storage";

            if (_soulAshText != null)
            {
                int walletAmount = _wallet != null ? _wallet.Amount : 0;
                int storageAmount = _baseStorage != null ? _baseStorage.SoulAshAmount : 0;
                _soulAshText.text = $"Soul Ash: {walletAmount} carried / {storageAmount} stored";
                _soulAshText.gameObject.SetActive(_baseStorage != null);
            }

            if (_soulAshRow != null)
                _soulAshRow.SetActive(_baseStorage != null);

            if (_depositResourcesButtonText != null)
                _depositResourcesButtonText.text = _baseStorage != null ? "Deposit Resources" : "Deposit All";

            if (_depositResourcesButton != null)
                _depositResourcesButton.interactable = hasStorage && _inventory != null;

            if (_depositSoulAshButton != null)
                _depositSoulAshButton.interactable = _baseStorage != null && _wallet != null && _wallet.Amount > 0;

            if (_withdrawSoulAshButton != null)
                _withdrawSoulAshButton.interactable = _baseStorage != null && _wallet != null && _baseStorage.SoulAshAmount > 0;

            int slotCount = hasStorage ? _storage.GetSize() : 0;
            EnsureSlotViews(slotCount);

            for (int i = 0; i < _slotViews.Count; i++)
            {
                if (_slotViews[i] != null)
                    _slotViews[i].UpdateView(hasStorage && i < slotCount ? _storage.GetSlot(i) : null);
            }
        }

        private void OnStorageSlotClicked(int slotIndex, PointerEventData.InputButton button)
        {
            if (_storage == null || _inventory == null)
                return;

            if (button == PointerEventData.InputButton.Left)
                _storage.TryWithdrawToInventory(slotIndex, int.MaxValue, _inventory);
            else if (button == PointerEventData.InputButton.Right)
                _storage.TryWithdrawToInventory(slotIndex, 1, _inventory);

            Refresh();
        }

        private void DepositResources()
        {
            if (_storage == null || _inventory == null)
                return;

            var slots = _inventory.GetAllSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.Item == null || slot.Amount <= 0)
                    continue;

                _storage.TryDepositFromInventory(_inventory, i, slot.Amount);
            }

            Refresh();
        }

        private void DepositSoulAsh()
        {
            _baseStorage?.DepositSoulAshFrom(_wallet);
            Refresh();
        }

        private void WithdrawSoulAsh()
        {
            _baseStorage?.WithdrawSoulAshTo(_wallet);
            Refresh();
        }

        private void EnsureSlotViews(int slotCount)
        {
            if (_slotRoot == null || _slotPrefab == null)
                return;

            while (_slotViews.Count < slotCount)
            {
                int index = _slotViews.Count;
                var slot = Instantiate(_slotPrefab, _slotRoot);
                slot.Init(index, null, OnStorageSlotClicked);
                _slotViews.Add(slot);
            }

            while (_slotViews.Count > slotCount)
            {
                int lastIndex = _slotViews.Count - 1;
                var slot = _slotViews[lastIndex];
                _slotViews.RemoveAt(lastIndex);

                if (slot != null)
                    Destroy(slot.gameObject);
            }
        }

        private void BuildLayout()
        {
            if (_built)
                return;

            _built = true;

            var root = CreateRect("StorageRuntimeRoot", transform);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(12f, 12f);
            root.offsetMax = new Vector2(-12f, -12f);

            var rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = 8f;
            rootLayout.padding = new RectOffset(8, 8, 8, 8);
            rootLayout.childControlHeight = true;
            rootLayout.childControlWidth = true;
            rootLayout.childForceExpandHeight = false;
            rootLayout.childForceExpandWidth = true;

            _titleText = CreateText(root, "StorageTitle", 24, FontStyles.Bold);
            _soulAshText = CreateText(root, "StorageSoulAshText", 16, FontStyles.Normal);

            var buttonRow = CreateRect("StorageButtonRow", root);
            buttonRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 84f;
            var rowLayout = buttonRow.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = true;

            _depositResourcesButton = CreateButton(buttonRow, "Deposit Resources");
            _depositResourcesButtonText = _depositResourcesButton.GetComponentInChildren<TMP_Text>();
            _depositResourcesButton.onClick.AddListener(DepositResources);

            var soulButtonRow = CreateRect("SoulAshButtonRow", buttonRow);
            _soulAshRow = soulButtonRow.gameObject;
            soulButtonRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            var soulRowLayout = soulButtonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            soulRowLayout.spacing = 6f;
            soulRowLayout.childControlHeight = true;
            soulRowLayout.childControlWidth = true;
            soulRowLayout.childForceExpandHeight = false;
            soulRowLayout.childForceExpandWidth = true;

            _depositSoulAshButton = CreateButton(soulButtonRow, "Deposit Soul Ash");
            _depositSoulAshButton.onClick.AddListener(DepositSoulAsh);
            _withdrawSoulAshButton = CreateButton(soulButtonRow, "Withdraw Soul Ash");
            _withdrawSoulAshButton.onClick.AddListener(WithdrawSoulAsh);

            var scroll = CreateSlotScrollArea(root, "StorageSlots", 260f);
            _slotRoot = scroll.content;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static TMP_Text CreateText(Transform parent, string name, int size, FontStyles style)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;

            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = size + 8f;

            return text;
        }

        private static ScrollRect CreateSlotScrollArea(Transform parent, string name, float preferredHeight)
        {
            var viewport = CreateRect(name, parent);
            viewport.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.16f);
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(8f, 0f);
            content.offsetMax = new Vector2(-8f, 0f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(52f, 52f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;

            return scrollRect;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var rect = CreateRect("Button", parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.22f, 0.96f);

            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.32f, 0.36f, 1f);
            colors.pressedColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            colors.disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.45f);
            button.colors = colors;

            var text = CreateText(rect, "Label", 16, FontStyles.Normal);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private void OnStorageChanged()
        {
            Refresh();
        }

        private void OnWalletChanged(int amount)
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (_storage != null)
                _storage.Changed -= OnStorageChanged;

            _baseStorage = null;

            if (_wallet != null)
                _wallet.Changed -= OnWalletChanged;
        }
    }
}
