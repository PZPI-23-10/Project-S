using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class WorldInteractionPromptUI : MonoBehaviour
    {
        private const string RuntimeCanvasName = "[Runtime] Interaction Prompt Canvas";

        private static WorldInteractionPromptUI _instance;
        private static Canvas _runtimeCanvas;

        public static WorldInteractionPromptUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = null;
                    return null;
                }

                return _instance;
            }

            private set => _instance = value;
        }

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _actionText;
        [SerializeField] private Vector2 _screenOffset = new Vector2(0f, 18f);

        private RectTransform _rectTransform;
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            Instance = this;
            EnsureReferences();
            Hide();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_instance, this))
                _instance = null;
        }

        public static WorldInteractionPromptUI GetOrCreate(Canvas preferredCanvas = null)
        {
            if (Instance != null)
                return Instance;

            var prompt = Object.FindFirstObjectByType<WorldInteractionPromptUI>(FindObjectsInactive.Include);
            if (prompt != null && prompt.gameObject.scene.IsValid())
            {
                Instance = prompt;
                prompt.EnsureReferences();
                return prompt;
            }

            return CreateFallback(preferredCanvas);
        }

        public static void HideCurrent()
        {
            WorldInteractionPromptUI instance = Instance;
            if (instance == null)
                return;

            instance.Hide();
        }

        public void Show(Vector3 worldPosition, string title, string actionText, Camera worldCamera)
        {
            if (!this)
                return;

            if (worldCamera == null || string.IsNullOrWhiteSpace(title))
            {
                Hide();
                return;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0f)
            {
                Hide();
                return;
            }

            EnsureReferences();
            if (_titleText != null)
                _titleText.text = title;

            if (_actionText != null)
                _actionText.text = actionText;

            ActivateForShow();
            Canvas.ForceUpdateCanvases();
            PositionAtScreenPoint((Vector2)screenPoint + _screenOffset);
        }

        public void Hide()
        {
            if (!this)
                return;

            CanvasGroup canvasGroup = EnsureCanvasGroup();
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private static WorldInteractionPromptUI CreateFallback(Canvas preferredCanvas)
        {
            Canvas canvas = ResolveCanvas(preferredCanvas);
            var go = new GameObject("[Runtime] WorldInteractionPromptUI", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(canvas.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(260f, 0f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.04f, 0.045f, 0.05f, 0.86f);
            image.raycastTarget = false;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 7);
            layout.spacing = 2f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var prompt = go.AddComponent<WorldInteractionPromptUI>();
            prompt._titleText = CreateText(rect, "Title", 17, FontStyles.Bold);
            prompt._actionText = CreateText(rect, "Action", 13, FontStyles.Normal);
            prompt.EnsureReferences();
            prompt.Hide();
            return prompt;
        }

        private static Canvas ResolveCanvas(Canvas preferredCanvas)
        {
            if (preferredCanvas != null)
                return preferredCanvas;

            if (_runtimeCanvas != null)
                return _runtimeCanvas;

            var canvasObject = new GameObject("[Runtime] HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.name = RuntimeCanvasName;
            Object.DontDestroyOnLoad(canvasObject);

            _runtimeCanvas = canvasObject.GetComponent<Canvas>();
            _runtimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _runtimeCanvas.sortingOrder = short.MaxValue;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return _runtimeCanvas;
        }

        private static TMP_Text CreateText(Transform parent, string name, int size, FontStyles style)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private void PositionAtScreenPoint(Vector2 screenPoint)
        {
            EnsureReferences();
            if (_canvasRect == null || _rectTransform == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, GetCanvasCamera(), out var localPoint);
            Vector2 size = CurrentSize();
            _rectTransform.anchoredPosition = TooltipUI.ClampAnchoredPosition(localPoint, _canvasRect.rect.size, size, _rectTransform.pivot);
        }

        private void ActivateForShow()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            CanvasGroup canvasGroup = EnsureCanvasGroup();
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        private Vector2 CurrentSize()
        {
            float width = Mathf.Max(_rectTransform.rect.width, LayoutUtility.GetPreferredWidth(_rectTransform), 180f);
            float height = Mathf.Max(_rectTransform.rect.height, LayoutUtility.GetPreferredHeight(_rectTransform), 36f);
            return new Vector2(width, height);
        }

        private Camera GetCanvasCamera()
        {
            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (_canvas == null || !_canvas.isActiveAndEnabled)
            {
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                _canvas = parentCanvas != null && parentCanvas.isActiveAndEnabled
                    ? parentCanvas
                    : ResolveCanvas(null);

                _canvasRect = null;
            }

            if (_canvas != null && !transform.IsChildOf(_canvas.transform))
            {
                transform.SetParent(_canvas.transform, false);
                _canvasRect = null;
            }

            if (_canvas != null && (_canvasRect == null || _canvasRect != _canvas.transform as RectTransform))
                _canvasRect = _canvas.transform as RectTransform;

            EnsureCanvasGroup();
        }

        private CanvasGroup EnsureCanvasGroup()
        {
            if (_canvasGroup != null)
                return _canvasGroup;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            return _canvasGroup;
        }
    }
}
