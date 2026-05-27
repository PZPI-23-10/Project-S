using System.Collections;
using Project_S.Runtime.Gameplay.Character.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class InventoryItemNotificationUI : MonoBehaviour
    {
        public static InventoryItemNotificationUI Instance { get; private set; }

        [SerializeField] private InventoryController _inventory;
        [SerializeField] private RectTransform _container;
        [SerializeField] private float _displayDuration = 3f;
        [SerializeField] private float _fadeDuration = 0.35f;
        [SerializeField] private Vector2 _screenOffset = new Vector2(-28f, -120f);
        [SerializeField] private Vector2 _itemSize = new Vector2(260f, 64f);
        [SerializeField] private Color _backgroundColor = new Color(0.04f, 0.045f, 0.05f, 0.9f);
        [SerializeField] private Color _accentColor = new Color(0.92f, 0.72f, 0.28f, 1f);

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private InventoryController _boundInventory;

        private void Awake()
        {
            Instance = this;
            EnsureReferences();
        }

        private void OnEnable()
        {
            BindInventory(_inventory != null ? _inventory : FindFirstObjectByType<InventoryController>());
        }

        private void OnDisable()
        {
            UnbindInventory();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            UnbindInventory();
        }

        public static InventoryItemNotificationUI GetOrCreate(Canvas preferredCanvas = null)
        {
            if (Instance != null)
                return Instance;

            foreach (var notificationUI in Resources.FindObjectsOfTypeAll<InventoryItemNotificationUI>())
            {
                if (notificationUI == null || !notificationUI.gameObject.scene.IsValid())
                    continue;

                Instance = notificationUI;
                notificationUI.EnsureReferences();
                return notificationUI;
            }

            return CreateFallback(preferredCanvas);
        }

        public void BindInventory(InventoryController inventory)
        {
            if (_boundInventory == inventory)
            {
                _inventory = inventory;
                return;
            }

            UnbindInventory();
            _inventory = inventory;

            if (_inventory != null)
            {
                _inventory.OnItemAdded += ShowItemAdded;
                _boundInventory = _inventory;
            }
        }

        public void ShowItemAdded(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return;

            EnsureReferences();
            var row = CreateNotificationRow(item, amount);

            if (Application.isPlaying)
                StartCoroutine(FadeAndDestroy(row));
        }

        private static InventoryItemNotificationUI CreateFallback(Canvas preferredCanvas)
        {
            Canvas canvas = ResolveCanvas(preferredCanvas);
            var go = new GameObject("[Runtime] InventoryItemNotificationUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            var notificationUI = go.AddComponent<InventoryItemNotificationUI>();
            notificationUI.EnsureReferences();
            return notificationUI;
        }

        private static Canvas ResolveCanvas(Canvas preferredCanvas)
        {
            if (preferredCanvas != null)
                return preferredCanvas;

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
                return canvas;

            var canvasObject = new GameObject("[Runtime] HUD Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private RectTransform CreateNotificationRow(ItemData item, int amount)
        {
            var rowObject = new GameObject("ItemNotification", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            rowObject.transform.SetParent(_container, false);

            var rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.sizeDelta = _itemSize;

            var background = rowObject.GetComponent<Image>();
            background.color = _backgroundColor;
            background.raycastTarget = false;

            var group = rowObject.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var layout = rowObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 12, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateIcon(rowRect, item);
            CreateTextBlock(rowRect, item, amount);
            return rowRect;
        }

        private void CreateIcon(Transform parent, ItemData item)
        {
            if (item.Icon == null)
                return;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(parent, false);

            var layout = iconObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 44f;
            layout.preferredHeight = 44f;
            layout.minWidth = 44f;
            layout.minHeight = 44f;

            var image = iconObject.GetComponent<Image>();
            image.sprite = item.Icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void CreateTextBlock(Transform parent, ItemData item, int amount)
        {
            var blockObject = new GameObject("TextBlock", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(LayoutElement));
            blockObject.transform.SetParent(parent, false);

            var layoutElement = blockObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = item.Icon != null ? 180f : 234f;
            layoutElement.flexibleWidth = 1f;

            var layout = blockObject.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 1f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateText(blockObject.transform, "ItemName", 16, FontStyles.Bold);
            title.text = string.IsNullOrWhiteSpace(item.ItemName) ? "Предмет" : item.ItemName;
            title.enableWordWrapping = false;
            title.overflowMode = TextOverflowModes.Ellipsis;

            var amountText = CreateText(blockObject.transform, "Amount", 13, FontStyles.Normal);
            amountText.text = $"x{amount}";
            amountText.color = _accentColor;
        }

        private static TMP_Text CreateText(Transform parent, string name, int size, FontStyles style)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private IEnumerator FadeAndDestroy(RectTransform row)
        {
            if (row == null)
                yield break;

            yield return new WaitForSeconds(Mathf.Max(0f, _displayDuration));

            var group = row.GetComponent<CanvasGroup>();
            float duration = Mathf.Max(0.01f, _fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration && row != null)
            {
                elapsed += Time.deltaTime;
                if (group != null)
                    group.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);

                yield return null;
            }

            if (row != null)
                Destroy(row.gameObject);
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            if (_canvas == null)
                _canvas = ResolveCanvas(null);

            EnsureContainer();
        }

        private void EnsureContainer()
        {
            if (_container == null)
            {
                var containerObject = new GameObject("ItemNotificationsContainer", typeof(RectTransform),
                    typeof(VerticalLayoutGroup));
                containerObject.transform.SetParent(transform, false);
                _container = containerObject.GetComponent<RectTransform>();
            }
            else if (_container.parent != transform)
            {
                _container.SetParent(transform, false);
            }

            _container.anchorMin = new Vector2(1f, 1f);
            _container.anchorMax = new Vector2(1f, 1f);
            _container.pivot = new Vector2(1f, 1f);
            _container.anchoredPosition = _screenOffset;
            _container.sizeDelta = new Vector2(_itemSize.x, 0f);

            var layout = _container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = _container.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void UnbindInventory()
        {
            if (_boundInventory != null)
                _boundInventory.OnItemAdded -= ShowItemAdded;

            _boundInventory = null;
        }
    }
}
