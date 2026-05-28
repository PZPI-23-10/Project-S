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

        [Header("New Asset Design")]
        [SerializeField] private Image _itemIcon;

        [Header("Settings")]
        [SerializeField] private Vector2 _slotOffset = new Vector2(0f, 8f);
        [SerializeField] private Vector2 _mouseOffset = new Vector2(18f, -18f);

        private RectTransform _rectTransform;
        private RectTransform _canvasRect;
        private Canvas _canvas;
        private Image _backgroundImage;

        private void Awake()
        {
            Instance = this;
            EnsureReferences();
            Hide();
        }

        public static TooltipUI GetOrCreate(Canvas preferredCanvas = null)
        {
            if (Instance != null) return Instance;
            foreach (var tooltip in Resources.FindObjectsOfTypeAll<TooltipUI>())
            {
                if (tooltip == null || !tooltip.gameObject.scene.IsValid()) continue;
                Instance = tooltip;
                tooltip.EnsureReferences();
                return tooltip;
            }
            return CreateFallback(preferredCanvas);
        }

        public void Show(ItemData item)
        {
            if (item == null) { Hide(); return; }
            ShowAtScreenPoint(item, (Vector2)Input.mousePosition + _mouseOffset);
        }

        public void Show(ItemData item, RectTransform anchor)
        {
            if (item == null) { Hide(); return; }

            EnsureReferences();
            SetContent(item);
            ActivateForShow();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
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
            if (item == null) { Hide(); return; }

            EnsureReferences();
            SetContent(item);
            ActivateForShow();

            LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
            Canvas.ForceUpdateCanvases();

            PositionAtScreenPoint(screenPoint);
        }

        public void Hide()
        {
            if (gameObject != null) gameObject.SetActive(false);
        }

        public static string FormatHeader(ItemData item)
        {
            if (item == null) return string.Empty;
            string stackText = item.IsStackable ? $"Стак: {Mathf.Max(1, item.MaxStack)}" : "Не складається в стак";
            return $"Тип: {LocalizeItemKind(item.Kind)}\nВага: {FormatFloat(item.Weight)}\n{stackText}";
        }

        public static string FormatDetails(ItemData item)
        {
            if (item == null) return string.Empty;

            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(item.Description)) lines.Add(item.Description);
            if (item.MaxInventoryStacks > 0) lines.Add($"Ліміт стаків в інвентарі: {item.MaxInventoryStacks}");
            if (item is WeaponItemData weapon) AppendWeaponDetails(lines, weapon);
            AppendConsumableDetails(lines, item);
            AppendStatEffects(lines, item.StatEffects);
            AppendBuff(lines, item.TimedBuffType, item.TimedBuffCategory, item.TimedBuffMultiplier, item.TimedBuffDurationSeconds);
            AppendBuff(lines, item.SecondaryTimedBuffType, item.SecondaryTimedBuffCategory, item.SecondaryTimedBuffMultiplier, item.SecondaryTimedBuffDurationSeconds);
            AppendBuffs(lines, item.TimedBuffs);
            AppendDamageConversions(lines, item.DamageConversions);

            if (item.SpecialEffect == ConsumableSpecialEffectType.HomeTeleport)
            {
                float delay = item.SpecialEffectDelaySeconds > 0f ? item.SpecialEffectDelaySeconds : 5f;
                lines.Add($"Особливе: повернення додому через {FormatFloat(delay)} с");
            }

            AppendSpecialEffects(lines, item.SpecialEffects);
            return string.Join("\n", lines);
        }

        private static void AppendWeaponDetails(List<string> lines, WeaponItemData weapon)
        {
            if (weapon.DamageProfile != null && weapon.DamageProfile.Count > 0)
            {
                var damageLines = new List<string>();
                foreach (var damage in weapon.DamageProfile) damageLines.Add($"{LocalizeDamageType(damage.Type)} {FormatFloat(damage.Amount)}");
                lines.Add("Урон: " + string.Join(", ", damageLines));
            }
            lines.Add($"Рівновага: {FormatFloat(weapon.PoiseDamage)}");
            lines.Add($"Швидкість атаки: x{FormatFloat(weapon.AttackSpeedMultiplier)}");
            lines.Add($"Комбо: {weapon.MaxComboHits}");
            lines.Add($"Блок: {FormatFloat(weapon.BlockMitigation * 100f)}%");
            lines.Add($"Парирування: {FormatFloat(weapon.ParryWindow)} с");
            if (weapon.HarvestTool != HarvestToolType.None) lines.Add($"Інструмент: {LocalizeHarvestTool(weapon.HarvestTool)}");
        }

        private static void AppendConsumableDetails(List<string> lines, ItemData item)
        {
            if (!Mathf.Approximately(item.HealthRestoreAmount, 0f)) lines.Add($"Здоров'я: {Signed(item.HealthRestoreAmount)}");
            if (!Mathf.Approximately(item.HungerRestoreAmount, 0f)) lines.Add($"Голод: +{FormatFloat(item.HungerRestoreAmount)}");
            if (!Mathf.Approximately(item.StaminaRestoreAmount, 0f)) lines.Add($"Витривалість: {Signed(item.StaminaRestoreAmount)}");
        }

        private static void AppendStatEffects(List<string> lines, IReadOnlyList<ConsumableStatEffect> effects)
        {
            if (effects == null) return;
            foreach (var effect in effects)
            {
                if (effect == null || Mathf.Approximately(effect.Amount, 0f)) continue;
                lines.Add($"{LocalizeStatType(effect.StatType)}: {Signed(effect.Amount)}");
            }
        }

        private static void AppendBuff(List<string> lines, TimedBuffType type, TimedBuffCategory category, float multiplier, float durationSeconds)
        {
            if (type == TimedBuffType.None || durationSeconds <= 0f) return;
            lines.Add($"Ефект: {LocalizeTimedBuff(type)} x{FormatFloat(multiplier)} на {FormatFloat(durationSeconds)} с ({LocalizeBuffCategory(category)})");
        }

        private static void AppendBuffs(List<string> lines, IReadOnlyList<ConsumableTimedBuffEffect> buffs)
        {
            if (buffs == null) return;
            foreach (var buff in buffs)
            {
                if (buff == null) continue;
                AppendBuff(lines, buff.Type, buff.Category, buff.Multiplier, buff.DurationSeconds);
            }
        }

        private static void AppendDamageConversions(List<string> lines, IReadOnlyList<DamageConversionEffect> conversions)
        {
            if (conversions == null) return;
            foreach (var conversion in conversions)
            {
                if (conversion == null || !conversion.IsValid()) continue;
                string source = conversion.Source == DamageConversionSource.Physical ? "фізичного урону" : LocalizeDamageType(conversion.FromType);
                lines.Add($"Перетворення: {FormatFloat(conversion.SourceFraction * 100f)}% {source} -> {FormatFloat(conversion.ConvertedDamageFraction * 100f)}% {LocalizeDamageType(conversion.ToType)} на {FormatFloat(conversion.DurationSeconds)} с");
            }
        }

        private static void AppendSpecialEffects(List<string> lines, IReadOnlyList<ConsumableSpecialEffect> specialEffects)
        {
            if (specialEffects == null) return;
            foreach (var specialEffect in specialEffects)
            {
                if (specialEffect == null || specialEffect.Type == ConsumableSpecialEffectType.None) continue;
                if (specialEffect.Type == ConsumableSpecialEffectType.HomeTeleport)
                {
                    float delay = specialEffect.DelaySeconds > 0f ? specialEffect.DelaySeconds : 5f;
                    lines.Add($"Особливе: повернення додому через {FormatFloat(delay)} с");
                }
            }
        }

        private void SetContent(ItemData item)
        {
            if (_titleText != null) _titleText.text = item.ItemName;
            if (_weightText != null) _weightText.text = FormatHeader(item);
            if (_descriptionText != null) _descriptionText.text = FormatDetails(item);

            if (_itemIcon != null)
            {
                _itemIcon.sprite = item.Icon;
                _itemIcon.gameObject.SetActive(item.Icon != null);
            }
        }

        private void ActivateForShow()
        {
            gameObject.SetActive(true);
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        private void PositionAtScreenPoint(Vector2 screenPoint)
        {
            EnsureReferences();
            if (_canvasRect == null || _rectTransform == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, GetCanvasCamera(), out var localPoint);

            float width = Mathf.Max(_rectTransform.rect.width, LayoutUtility.GetPreferredWidth(_rectTransform), 240f);
            float height = Mathf.Max(_rectTransform.rect.height, LayoutUtility.GetPreferredHeight(_rectTransform), 40f);
            Vector2 size = new Vector2(width, height);

            _rectTransform.anchoredPosition = ClampAnchoredPosition(localPoint, _canvasRect.rect.size, size, _rectTransform.pivot);
        }

        public static Vector2 ClampAnchoredPosition(Vector2 desired, Vector2 canvasSize, Vector2 tooltipSize, Vector2 pivot)
        {
            Vector2 halfCanvas = canvasSize * 0.5f;
            float width = Mathf.Max(1f, tooltipSize.x);
            float height = Mathf.Max(1f, tooltipSize.y);

            float minX = -halfCanvas.x + width * pivot.x;
            float maxX = halfCanvas.x - width * (1f - pivot.x);
            float minY = -halfCanvas.y + height * pivot.y;
            float maxY = halfCanvas.y - height * (1f - pivot.y);

            if (minX > maxX) desired.x = 0f; else desired.x = Mathf.Clamp(desired.x, minX, maxX);
            if (minY > maxY) desired.y = 0f; else desired.y = Mathf.Clamp(desired.y, minY, maxY);

            return desired;
        }

        private Camera GetCanvasCamera()
        {
            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null) _rectTransform = transform as RectTransform;
            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) _canvas = ResolveCanvas(null);
            if (_canvasRect == null && _canvas != null) _canvasRect = _canvas.transform as RectTransform;
            EnsureBackground();
        }

        // ВОСЬ ЦЯ ФУНКЦІЯ ПОВЕРНУЛАСЯ НА МІСЦЕ!
        private static Canvas ResolveCanvas(Canvas preferredCanvas)
        {
            if (preferredCanvas != null) return preferredCanvas;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas;
            var canvasObject = new GameObject("[Runtime] HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private void EnsureBackground()
        {
            if (_backgroundImage == null) _backgroundImage = GetComponent<Image>();
            if (_backgroundImage == null) _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.raycastTarget = false;
        }

        private static string LocalizeItemKind(ItemKind kind)
        {
            return kind switch
            {
                ItemKind.Resource => "Ресурс",
                ItemKind.Weapon => "Зброя",
                ItemKind.Consumable => "Їжа/зілля",
                ItemKind.Tool => "Інструмент",
                ItemKind.Accessory => "Аксесуар",
                ItemKind.Material => "Матеріал",
                _ => kind.ToString()
            };
        }

        private static string LocalizeDamageType(DamageType type)
        {
            return type switch
            {
                DamageType.Piercing => "колючий",
                DamageType.Blunt => "дробильний",
                DamageType.Slashing => "рубальний",
                DamageType.Ice => "крижаний",
                DamageType.Lightning => "блискавка",
                DamageType.Fire => "вогонь",
                DamageType.Holy => "священний",
                _ => type.ToString()
            };
        }

        private static string LocalizeHarvestTool(HarvestToolType tool)
        {
            return tool switch
            {
                HarvestToolType.Axe => "сокира",
                HarvestToolType.Pickaxe => "кирка",
                HarvestToolType.Knife => "ніж",
                _ => tool.ToString()
            };
        }

        private static string LocalizeTimedBuff(TimedBuffType type)
        {
            return type switch
            {
                TimedBuffType.AttackDamage => "урон атаки",
                TimedBuffType.SoulAshReward => "здобуток попелу душ",
                TimedBuffType.AttackSpeed => "швидкість атаки",
                TimedBuffType.StaminaCost => "витрати витривалості",
                TimedBuffType.MaxHealth => "максимальне здоров'я",
                _ => type.ToString()
            };
        }

        private static string LocalizeBuffCategory(TimedBuffCategory category)
        {
            return category switch
            {
                TimedBuffCategory.Food => "їжа",
                TimedBuffCategory.Healing => "лікування",
                TimedBuffCategory.Potion => "зілля",
                TimedBuffCategory.Weapon => "зброя",
                TimedBuffCategory.Debuff => "послаблення",
                _ => category.ToString()
            };
        }

        private static string LocalizeStatType(StatType type)
        {
            return type switch
            {
                StatType.MaxHealth => "Максимальне здоров'я",
                StatType.Health => "Здоров'я",
                StatType.MaxStamina => "Максимальна витривалість",
                StatType.Stamina => "Витривалість",
                StatType.StaminaRegen => "Відновлення витривалості",
                StatType.MoveSpeed => "Швидкість руху",
                StatType.SprintSpeed => "Швидкість бігу",
                StatType.AttackPower => "Сила атаки",
                StatType.CarryWeight => "Переносима вага",
                StatType.Fear => "Страх",
                StatType.Curse => "Прокляття",
                StatType.Hunger => "Голод",
                StatType.Thirst => "Спрага",
                StatType.Poise => "Рівновага",
                StatType.MaxPoise => "Максимальна рівновага",
                StatType.PhylacteryCharge => "Заряд філактерії",
                StatType.MaxPhylacteryCharge => "Макс. заряд філактерії",
                _ => type.ToString()
            };
        }
        private static string FormatFloat(float value)
        {
            return value.ToString("0.##");
        }

        private static string Signed(float value)
        {
            return value > 0f ? "+" + FormatFloat(value) : FormatFloat(value);
        }

        private static TooltipUI CreateFallback(Canvas preferredCanvas)
        {
            var go = new GameObject("[Runtime] TooltipUI", typeof(RectTransform));
            var tooltip = go.AddComponent<TooltipUI>();
            tooltip.Hide();
            return tooltip;
        }
    }
}