using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using Project_S.Runtime.Gameplay.Harvesting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.HUD
{
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _weightText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Vector2 _slotOffset = new Vector2(0f, 8f);
        [SerializeField] private Vector2 _mouseOffset = new Vector2(18f, -18f);
        [SerializeField] private Color _backgroundColor = new Color(0.035f, 0.04f, 0.045f, 0.96f);
        [SerializeField] private float _panelWidth = 340f;
        [SerializeField] private float _panelPadding = 12f;
        [SerializeField] private float _lineSpacing = 4f;

        private RectTransform _rectTransform;
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private Image _backgroundImage;
        private VerticalLayoutGroup _layoutGroup;

        private void Awake()
        {
            Instance = this;
            EnsureReferences();
            Hide();
        }

        public static TooltipUI GetOrCreate(Canvas preferredCanvas = null)
        {
            if (Instance != null)
                return Instance;

            foreach (var tooltip in Resources.FindObjectsOfTypeAll<TooltipUI>())
            {
                if (tooltip == null || !tooltip.gameObject.scene.IsValid())
                    continue;

                Instance = tooltip;
                tooltip.EnsureReferences();
                return tooltip;
            }

            return CreateFallback(preferredCanvas);
        }

        public void Show(ItemData item)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            ShowAtScreenPoint(item, (Vector2)Input.mousePosition + _mouseOffset);
        }

        public void Show(ItemData item, RectTransform anchor)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            EnsureReferences();
            SetContent(item);
            ResizePanelToContent();
            ActivateForShow();
            Canvas.ForceUpdateCanvases();

            if (anchor == null)
            {
                PositionAtScreenPoint((Vector2)Input.mousePosition + _mouseOffset);
                return;
            }

            var corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(), topCenter) + _slotOffset;
            PositionAtScreenPoint(screenPoint);
        }

        public void ShowAtScreenPoint(ItemData item, Vector2 screenPoint)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            EnsureReferences();
            SetContent(item);
            ResizePanelToContent();
            ActivateForShow();
            Canvas.ForceUpdateCanvases();
            PositionAtScreenPoint(screenPoint);
        }

        public void Hide()
        {
            if (gameObject != null)
                gameObject.SetActive(false);
        }

        public static string FormatHeader(ItemData item)
        {
            if (item == null)
                return string.Empty;

            string stackText = item.IsStackable ? $"Стак: {Mathf.Max(1, item.MaxStack)}" : "Не стакается";
            return $"Тип: {item.Kind}\nВес: {FormatFloat(item.Weight)}\n{stackText}";
        }

        public static string FormatDetails(ItemData item)
        {
            if (item == null)
                return string.Empty;

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Description))
                lines.Add(item.Description);

            if (item.MaxInventoryStacks > 0)
                lines.Add($"Лимит стаков в инвентаре: {item.MaxInventoryStacks}");

            if (item is WeaponItemData weapon)
                AppendWeaponDetails(lines, weapon);

            AppendConsumableDetails(lines, item);
            AppendStatEffects(lines, item.StatEffects);
            AppendBuff(lines, item.TimedBuffType, item.TimedBuffCategory, item.TimedBuffMultiplier,
                item.TimedBuffDurationSeconds);
            AppendBuff(lines, item.SecondaryTimedBuffType, item.SecondaryTimedBuffCategory,
                item.SecondaryTimedBuffMultiplier, item.SecondaryTimedBuffDurationSeconds);
            AppendBuffs(lines, item.TimedBuffs);
            AppendDamageConversions(lines, item.DamageConversions);

            if (item.SpecialEffect == ConsumableSpecialEffectType.HomeTeleport)
            {
                float delay = item.SpecialEffectDelaySeconds > 0f ? item.SpecialEffectDelaySeconds : 5f;
                lines.Add($"Особое: телепорт домой через {FormatFloat(delay)}с");
            }

            AppendSpecialEffects(lines, item.SpecialEffects);

            return string.Join("\n", lines);
        }

        public static Vector2 ClampAnchoredPosition(Vector2 desired, Vector2 canvasSize, Vector2 tooltipSize,
            Vector2 pivot)
        {
            Vector2 halfCanvas = canvasSize * 0.5f;
            float width = Mathf.Max(1f, tooltipSize.x);
            float height = Mathf.Max(1f, tooltipSize.y);

            float minX = -halfCanvas.x + width * pivot.x;
            float maxX = halfCanvas.x - width * (1f - pivot.x);
            float minY = -halfCanvas.y + height * pivot.y;
            float maxY = halfCanvas.y - height * (1f - pivot.y);

            if (minX > maxX)
                desired.x = 0f;
            else
                desired.x = Mathf.Clamp(desired.x, minX, maxX);

            if (minY > maxY)
                desired.y = 0f;
            else
                desired.y = Mathf.Clamp(desired.y, minY, maxY);

            return desired;
        }

        private static TooltipUI CreateFallback(Canvas preferredCanvas)
        {
            Canvas canvas = ResolveCanvas(preferredCanvas);
            var go = new GameObject("[Runtime] TooltipUI", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(280f, 0f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.05f, 0.055f, 0.06f, 0.94f);
            image.raycastTarget = false;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var tooltip = go.AddComponent<TooltipUI>();
            tooltip._titleText = CreateText(rect, "Title", 18, FontStyles.Bold);
            tooltip._weightText = CreateText(rect, "Meta", 13, FontStyles.Normal);
            tooltip._descriptionText = CreateText(rect, "Description", 13, FontStyles.Normal);
            tooltip.EnsureReferences();
            tooltip.Hide();
            return tooltip;
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

        private static TMP_Text CreateText(Transform parent, string name, int size, FontStyles style)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private static void AppendWeaponDetails(List<string> lines, WeaponItemData weapon)
        {
            if (weapon.DamageProfile != null && weapon.DamageProfile.Count > 0)
            {
                var damageLines = new List<string>();
                foreach (var damage in weapon.DamageProfile)
                    damageLines.Add($"{damage.Type} {FormatFloat(damage.Amount)}");

                lines.Add("Урон: " + string.Join(", ", damageLines));
            }

            lines.Add($"Баланс: {FormatFloat(weapon.PoiseDamage)}");
            lines.Add($"Скорость атаки: x{FormatFloat(weapon.AttackSpeedMultiplier)}");
            lines.Add($"Комбо: {weapon.MaxComboHits}");
            lines.Add($"Блок: {FormatFloat(weapon.BlockMitigation * 100f)}%");
            lines.Add($"Парирование: {FormatFloat(weapon.ParryWindow)}с");

            if (weapon.HarvestTool != HarvestToolType.None)
                lines.Add($"Инструмент: {weapon.HarvestTool}");
        }

        private static void AppendConsumableDetails(List<string> lines, ItemData item)
        {
            if (!Mathf.Approximately(item.HealthRestoreAmount, 0f))
                lines.Add($"Здоровье: {Signed(item.HealthRestoreAmount)}");

            if (!Mathf.Approximately(item.HungerRestoreAmount, 0f))
                lines.Add($"Голод: +{FormatFloat(item.HungerRestoreAmount)}");

            if (!Mathf.Approximately(item.StaminaRestoreAmount, 0f))
                lines.Add($"Стамина: {Signed(item.StaminaRestoreAmount)}");
        }

        private static void AppendStatEffects(List<string> lines, IReadOnlyList<ConsumableStatEffect> effects)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null || Mathf.Approximately(effect.Amount, 0f))
                    continue;

                lines.Add($"{effect.StatType}: {Signed(effect.Amount)}");
            }
        }

        private static void AppendBuff(List<string> lines, TimedBuffType type, TimedBuffCategory category,
            float multiplier, float durationSeconds)
        {
            if (type == TimedBuffType.None || durationSeconds <= 0f)
                return;

            lines.Add($"Бафф: {type} x{FormatFloat(multiplier)} на {FormatFloat(durationSeconds)}с ({category})");
        }

        private static void AppendBuffs(List<string> lines, IReadOnlyList<ConsumableTimedBuffEffect> buffs)
        {
            if (buffs == null)
                return;

            foreach (var buff in buffs)
            {
                if (buff == null)
                    continue;

                AppendBuff(lines, buff.Type, buff.Category, buff.Multiplier, buff.DurationSeconds);
            }
        }

        private static void AppendDamageConversions(List<string> lines, IReadOnlyList<DamageConversionEffect> conversions)
        {
            if (conversions == null)
                return;

            foreach (var conversion in conversions)
            {
                if (conversion == null || !conversion.IsValid())
                    continue;

                string source = conversion.Source == DamageConversionSource.Physical
                    ? "Physical"
                    : conversion.FromType.ToString();
                lines.Add(
                    $"Конверсия: {FormatFloat(conversion.SourceFraction * 100f)}% {source} -> {FormatFloat(conversion.ConvertedDamageFraction * 100f)}% {conversion.ToType} на {FormatFloat(conversion.DurationSeconds)}с");
            }
        }

        private static void AppendSpecialEffects(List<string> lines, IReadOnlyList<ConsumableSpecialEffect> specialEffects)
        {
            if (specialEffects == null)
                return;

            foreach (var specialEffect in specialEffects)
            {
                if (specialEffect == null || specialEffect.Type == ConsumableSpecialEffectType.None)
                    continue;

                if (specialEffect.Type == ConsumableSpecialEffectType.HomeTeleport)
                {
                    float delay = specialEffect.DelaySeconds > 0f ? specialEffect.DelaySeconds : 5f;
                    lines.Add($"Особое: телепорт домой через {FormatFloat(delay)}с");
                }
            }
        }

        private void SetContent(ItemData item)
        {
            if (_titleText != null)
                _titleText.text = item.ItemName;

            if (_weightText != null)
                _weightText.text = FormatHeader(item);

            if (_descriptionText != null)
                _descriptionText.text = FormatDetails(item);
        }

        private void ResizePanelToContent()
        {
            EnsureReferences();
            if (_rectTransform == null)
                return;

            float width = Mathf.Max(_panelWidth, 260f);
            float textWidth = Mathf.Max(40f, width - _panelPadding * 2f);
            float height = _panelPadding * 2f;

            height += PreferredHeight(_titleText, textWidth);
            height += PreferredHeight(_weightText, textWidth);
            height += PreferredHeight(_descriptionText, textWidth);

            if (_titleText != null && _weightText != null)
                height += _lineSpacing;

            if (_descriptionText != null)
                height += _lineSpacing;

            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(80f, height));
        }

        private void ActivateForShow()
        {
            gameObject.SetActive(true);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        private void PositionAtScreenPoint(Vector2 screenPoint)
        {
            EnsureReferences();
            if (_canvasRect == null || _rectTransform == null)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, GetCanvasCamera(),
                out var localPoint);
            Vector2 size = CurrentTooltipSize();
            _rectTransform.anchoredPosition =
                ClampAnchoredPosition(localPoint, _canvasRect.rect.size, size, _rectTransform.pivot);
        }

        private Vector2 CurrentTooltipSize()
        {
            float width = Mathf.Max(_rectTransform.rect.width, LayoutUtility.GetPreferredWidth(_rectTransform), 240f);
            float height = Mathf.Max(_rectTransform.rect.height, LayoutUtility.GetPreferredHeight(_rectTransform), 40f);
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

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            if (_canvas == null)
                _canvas = ResolveCanvas(null);

            if (_canvasRect == null && _canvas != null)
                _canvasRect = _canvas.transform as RectTransform;

            EnsureBackground();
            EnsureLayout();
        }

        private void EnsureBackground()
        {
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            if (_backgroundImage == null)
                _backgroundImage = gameObject.AddComponent<Image>();

            _backgroundImage.color = _backgroundColor;
            _backgroundImage.raycastTarget = false;
        }

        private void EnsureLayout()
        {
            if (_layoutGroup == null)
                _layoutGroup = GetComponent<VerticalLayoutGroup>();

            if (_layoutGroup == null)
                _layoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();

            int padding = Mathf.RoundToInt(_panelPadding);
            _layoutGroup.padding = new RectOffset(padding, padding, padding, padding);
            _layoutGroup.spacing = _lineSpacing;
            _layoutGroup.childControlWidth = true;
            _layoutGroup.childControlHeight = true;
            _layoutGroup.childForceExpandWidth = true;
            _layoutGroup.childForceExpandHeight = false;
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##");
        }

        private static float PreferredHeight(TMP_Text text, float width)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
                return 0f;

            return Mathf.Max(text.fontSize + 4f, text.GetPreferredValues(text.text, width, 0f).y);
        }

        private static string Signed(float value)
        {
            return value > 0f ? "+" + FormatFloat(value) : FormatFloat(value);
        }
    }
}
