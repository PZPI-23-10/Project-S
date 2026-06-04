using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class UpgradePanelUI : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject _root;
        [SerializeField] private UpgradeNodeView[] _nodes;
        [SerializeField] private Image _detailsIcon;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private Transform _costIconRoot;
        [SerializeField] private Sprite _soulAshIcon;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _purchaseButton;
        [SerializeField] private TMP_Text _purchaseButtonText;

        private readonly List<GameObject> _spawnedCostIconSlots = new List<GameObject>();

        private PlayerUpgradeController _controller;
        private InventoryController _inventory;
        private SoulAshWallet _wallet;
        private BaseResourceStorage _subscribedBaseStorage;
        private UpgradeDefinition _selectedUpgrade;
        private bool _subscribed;

        public void Initialize(PlayerUpgradeController controller, InventoryController inventory, SoulAshWallet wallet)
        {
            Unsubscribe();
            _controller = controller;
            _inventory = inventory;
            _wallet = wallet;
            _controller?.EnsureInitialized();

            ResolveSceneReferences();
            BindNodeButtons();
            BindPurchaseButton();
            Subscribe();
            SelectDefaultUpgrade();
            Refresh();
        }

        public void SetPanelVisible(bool visible)
        {
            GameObject target = _root != null ? _root : gameObject;
            if (target != null)
                target.SetActive(visible);

            if (visible)
                Refresh();
        }

        public void Refresh()
        {
            if (_controller == null)
                return;

            _controller.EnsureInitialized();
            RefreshBaseStorageSubscription();
            SelectDefaultUpgrade();
            RefreshNodes();
            RefreshDetails();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void ResolveSceneReferences()
        {
            if (_root == null)
                _root = gameObject;

            if (_nodes == null || _nodes.Length == 0)
                _nodes = GetComponentsInChildren<UpgradeNodeView>(true);

            if (_detailsIcon == null)
                _detailsIcon = FindChildComponent<Image>("DetailsIcon");

            if (_titleText == null)
                _titleText = FindChildComponent<TMP_Text>("TitleText");

            if (_descriptionText == null)
                _descriptionText = FindChildComponent<TMP_Text>("DescriptionText");

            if (_costText == null)
                _costText = FindChildComponent<TMP_Text>("CostText");

            if (_costIconRoot == null)
                _costIconRoot = FindChildComponent<Transform>("CostIconList");

            if (_statusText == null)
                _statusText = FindChildComponent<TMP_Text>("StatusText");

            if (_purchaseButton == null)
                _purchaseButton = FindChildComponent<Button>("PurchaseButton");

            if (_purchaseButtonText == null && _purchaseButton != null)
                _purchaseButtonText = _purchaseButton.GetComponentInChildren<TMP_Text>(true);
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var child in transforms)
            {
                if (child.name == childName && child.TryGetComponent(out T component))
                    return component;
            }

            return null;
        }

        private void BindNodeButtons()
        {
            foreach (var node in EnumerateNodes())
                node.SetClickHandler(SelectNode);
        }

        private void BindPurchaseButton()
        {
            if (_purchaseButton == null)
                return;

            _purchaseButton.onClick.RemoveListener(PurchaseSelected);
            _purchaseButton.onClick.AddListener(PurchaseSelected);
        }

        private void SelectNode(UpgradeNodeView node)
        {
            if (node == null || node.Definition == null)
                return;

            _selectedUpgrade = node.Definition;
            Refresh();
        }

        private void SelectDefaultUpgrade()
        {
            if (_selectedUpgrade != null && IsKnownUpgrade(_selectedUpgrade))
                return;

            var definitions = EnumerateNodes()
                .Select(x => x.Definition)
                .Concat(_controller.Upgrades)
                .Where(x => x != null)
                .Distinct()
                .ToList();

            _selectedUpgrade = definitions.FirstOrDefault(x => !_controller.HasUpgrade(x.Id) && _controller.Check(x).CanPurchase)
                ?? definitions.FirstOrDefault(x => !_controller.HasUpgrade(x.Id))
                ?? definitions.FirstOrDefault();
        }

        private bool IsKnownUpgrade(UpgradeDefinition upgrade)
        {
            return EnumerateNodes().Any(x => x.Definition == upgrade)
                   || _controller.Upgrades.Contains(upgrade);
        }

        private void RefreshNodes()
        {
            foreach (var node in EnumerateNodes())
                node.Refresh(_controller, node.Definition == _selectedUpgrade);
        }

        private IEnumerable<UpgradeNodeView> EnumerateNodes()
        {
            return (_nodes ?? System.Array.Empty<UpgradeNodeView>()).Where(x => x != null);
        }

        private void RefreshDetails()
        {
            if (_selectedUpgrade == null)
            {
                SetText(_titleText, "Апгрейди");
                SetText(_descriptionText, "Немає доступних апгрейдів.");
                SetText(_costText, string.Empty);
                SetText(_statusText, string.Empty);
                SetButtonState(false, "Купити");
                SetDetailsIcon(null);
                ClearCostIcons();
                return;
            }

            var check = _controller.Check(_selectedUpgrade);
            bool purchased = _controller.HasUpgrade(_selectedUpgrade.Id);

            SetText(_titleText, _selectedUpgrade.Title);
            SetText(_descriptionText, _selectedUpgrade.Description);
            SetText(_costText, BuildCostText(_selectedUpgrade));
            RefreshCostIcons(_selectedUpgrade);
            SetText(_statusText, purchased
                ? "<color=#82e6a2>Куплено</color>"
                : check.CanPurchase
                    ? "<color=#f2d079>Доступно</color>"
                    : "<color=#ff8d74>" + check.Message + "</color>");

            SetDetailsIcon(_selectedUpgrade.Icon);
            SetButtonState(!purchased && check.CanPurchase, purchased ? "Куплено" : check.CanPurchase ? "Купити" : "Закрито");
        }

        private void RefreshCostIcons(UpgradeDefinition upgrade)
        {
            ClearCostIcons();

            if (upgrade == null)
                return;

            EnsureCostIconRoot();
            if (_costIconRoot == null)
                return;

            if (upgrade.SoulAshCost > 0 && _soulAshIcon != null)
                SpawnCostIcon(_soulAshIcon, _controller.GetOwnedSoulAsh(), upgrade.SoulAshCost);

            foreach (var cost in upgrade.ItemCosts ?? Enumerable.Empty<UpgradeItemCost>())
            {
                if (cost == null || cost.Item == null || cost.Amount <= 0)
                    continue;

                SpawnCostIcon(cost.Item.Icon, _controller.GetOwnedItemCount(cost.Item), cost.Amount);
            }

            _costIconRoot.gameObject.SetActive(_spawnedCostIconSlots.Count > 0);
        }

        private void EnsureCostIconRoot()
        {
            if (_costIconRoot == null)
            {
                Transform parent = _costText != null && _costText.transform.parent != null
                    ? _costText.transform.parent
                    : transform;

                var root = new GameObject("CostIconList", typeof(RectTransform));
                root.transform.SetParent(parent, false);
                _costIconRoot = root.transform;

                var rect = (RectTransform)_costIconRoot;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(240f, 58f);

                if (_costText != null)
                {
                    var costRect = _costText.rectTransform;
                    rect.anchoredPosition = costRect.anchoredPosition + new Vector2(0f, -54f);
                }
            }

            var layout = _costIconRoot.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = _costIconRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        private void SpawnCostIcon(Sprite icon, int owned, int required)
        {
            var slot = new GameObject("CostIconSlot", typeof(RectTransform));
            slot.transform.SetParent(_costIconRoot, false);

            var slotRect = (RectTransform)slot.transform;
            slotRect.sizeDelta = new Vector2(54f, 54f);

            var slotLayout = slot.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = 54f;
            slotLayout.preferredHeight = 54f;

            var background = slot.AddComponent<Image>();
            background.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            background.raycastTarget = false;

            var iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(slot.transform, false);
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 4f);
            iconRect.offsetMax = new Vector2(-4f, -4f);

            var iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var amountObject = new GameObject("AmountText", typeof(RectTransform));
            amountObject.transform.SetParent(slot.transform, false);
            var amountRect = (RectTransform)amountObject.transform;
            amountRect.anchorMin = new Vector2(1f, 0f);
            amountRect.anchorMax = new Vector2(1f, 0f);
            amountRect.pivot = new Vector2(1f, 0f);
            amountRect.anchoredPosition = new Vector2(-3f, 3f);
            amountRect.sizeDelta = new Vector2(72f, 22f);

            var amountText = amountObject.AddComponent<TextMeshProUGUI>();
            amountText.text = $"{owned}/{required}";
            amountText.fontSize = 18f;
            amountText.fontStyle = FontStyles.Bold;
            amountText.alignment = TextAlignmentOptions.BottomRight;
            amountText.color = owned >= required ? Color.white : new Color(1f, 0.4f, 0.4f);
            amountText.enableWordWrapping = false;
            amountText.raycastTarget = false;

            _spawnedCostIconSlots.Add(slot);
        }

        private void ClearCostIcons()
        {
            foreach (var slot in _spawnedCostIconSlots)
            {
                if (slot != null)
                {
                    if (Application.isPlaying)
                        Destroy(slot);
                    else
                        DestroyImmediate(slot);
                }
            }

            _spawnedCostIconSlots.Clear();

            if (_costIconRoot != null)
                _costIconRoot.gameObject.SetActive(false);
        }

        private void SetDetailsIcon(Sprite icon)
        {
            if (_detailsIcon == null)
                return;

            _detailsIcon.sprite = icon;
            _detailsIcon.enabled = icon != null;
        }

        private void SetButtonState(bool interactable, string label)
        {
            if (_purchaseButton != null)
                _purchaseButton.interactable = interactable;

            SetText(_purchaseButtonText, label);
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private string BuildCostText(UpgradeDefinition upgrade)
        {
            var lines = new List<string>();

            if (upgrade.SoulAshCost > 0)
                lines.Add(FormatCost("Попіл душ", _controller.GetOwnedSoulAsh(), upgrade.SoulAshCost));

            foreach (var cost in upgrade.ItemCosts ?? Enumerable.Empty<UpgradeItemCost>())
            {
                if (cost == null || cost.Item == null || cost.Amount <= 0)
                    continue;

                lines.Add(FormatCost(cost.Item.ItemName, _controller.GetOwnedItemCount(cost.Item), cost.Amount));
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "Безкоштовно";
        }

        private static string FormatCost(string label, int owned, int required)
        {
            string color = owned >= required ? "#ffffff" : "#ff8d74";
            return $"<color={color}>{label}: {owned}/{required}</color>";
        }

        private void PurchaseSelected()
        {
            if (_selectedUpgrade == null || _controller == null)
                return;

            _controller.TryPurchase(_selectedUpgrade, out _);
            Refresh();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (_controller != null) _controller.Changed += Refresh;
            if (_inventory != null) _inventory.OnInventoryChanged += Refresh;
            if (_wallet != null) _wallet.Changed += OnWalletChanged;
            RefreshBaseStorageSubscription();
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (_controller != null) _controller.Changed -= Refresh;
            if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;
            if (_wallet != null) _wallet.Changed -= OnWalletChanged;
            if (_subscribedBaseStorage != null) _subscribedBaseStorage.Changed -= Refresh;
            _subscribedBaseStorage = null;
            _subscribed = false;
        }

        private void RefreshBaseStorageSubscription()
        {
            var active = BaseResourceStorage.Active;
            if (_subscribedBaseStorage == active)
                return;

            if (_subscribedBaseStorage != null)
                _subscribedBaseStorage.Changed -= Refresh;

            _subscribedBaseStorage = active;

            if (_subscribedBaseStorage != null)
                _subscribedBaseStorage.Changed += Refresh;
        }

        private void OnWalletChanged(int _)
        {
            Refresh();
        }
    }
}
