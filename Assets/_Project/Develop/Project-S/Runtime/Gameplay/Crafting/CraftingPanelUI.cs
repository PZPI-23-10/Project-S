/*using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CraftingPanelUI : MonoBehaviour
    {
        private readonly List<Button> _recipeButtons = new List<Button>();

        private InventoryController _inventory;
        private SoulAshWallet _wallet;
        private CraftingService _craftingService;
        private TimedCraftingStation _activeStation;
        private List<CraftingRecipeData> _allRecipes = new List<CraftingRecipeData>();
        private CraftingContext _context = CraftingContext.Hand;
        private CraftingRecipeData _selectedRecipe;

        private Transform _recipeListRoot;
        private TMP_Text _titleText;
        private TMP_Text _walletText;
        private TMP_Text _fuelText;
        private TMP_Text _progressText;
        private TMP_Text _detailsText;
        private Button _addFuelButton;
        private TMP_Text _addFuelButtonText;
        private Button _craftButton;
        private TMP_Text _craftButtonText;
        private bool _built;
        private bool _subscribed;
        private GameObject _rootObject;

        public void Initialize(InventoryController inventory, SoulAshWallet wallet, CraftingContext context)
        {
            Unsubscribe();

            _inventory = inventory;
            _wallet = wallet;
            _craftingService = new CraftingService(_inventory, _wallet);
            _allRecipes = CraftingService.LoadRecipes();

            BuildLayout();
            Subscribe();
            SetContext(context);
        }

        public void SetPanelVisible(bool visible)
        {
            if (_rootObject != null)
                _rootObject.SetActive(visible);
        }

        public void SetContext(CraftingContext context)
        {
            _context = context;
            ResolveActiveStation();
            Refresh();
        }

        public void Refresh()
        {
            if (!_built || _craftingService == null)
                return;

            ResolveActiveStation();

            _titleText.text = GetTitle();
            _walletText.text = $"Попіл душ: {(_wallet != null ? _wallet.Amount : 0)}";
            RefreshStationHeader();

            var contextRecipes = GetContextRecipes();
            if (_selectedRecipe == null || _selectedRecipe.Context != _context || !contextRecipes.Contains(_selectedRecipe))
                _selectedRecipe = contextRecipes.FirstOrDefault();

            RebuildRecipeButtons(contextRecipes);
            RefreshDetails();
        }

        private List<CraftingRecipeData> GetContextRecipes()
        {
            return _allRecipes
                .Where(x => x != null && x.Context == _context)
                .OrderBy(x => x.RecipeName)
                .ToList();
        }

        private void RebuildRecipeButtons(IReadOnlyList<CraftingRecipeData> recipes)
        {
            foreach (var button in _recipeButtons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            _recipeButtons.Clear();

            foreach (var recipe in recipes)
            {
                var button = CreateButton(_recipeListRoot, recipe.RecipeName);
                var capturedRecipe = recipe;
                button.onClick.AddListener(() =>
                {
                    _selectedRecipe = capturedRecipe;
                    Refresh();
                });

                var label = button.GetComponentInChildren<TMP_Text>();
                var check = CheckRecipe(recipe);
                label.text = check.CanCraft ? recipe.RecipeName : $"{recipe.RecipeName} *";
                label.color = recipe == _selectedRecipe ? new Color(1f, 0.88f, 0.35f) : Color.white;
                button.interactable = !IsStationContext(_context) || (_activeStation != null && !_activeStation.IsCooking);

                _recipeButtons.Add(button);
            }
        }

        private void RefreshDetails()
        {
            if (_selectedRecipe == null)
            {
                _detailsText.text = "Немає доступних рецептів.";
                _craftButton.interactable = false;
                _craftButtonText.text = GetActionLabel();
                return;
            }

            CraftingCheck check = CheckRecipe(_selectedRecipe);
            _detailsText.text = BuildDetailsText(_selectedRecipe, check);
            _craftButton.interactable = check.CanCraft;
            _craftButtonText.text = _context switch
            {
                _ when IsStationContext(_context) && _activeStation != null && _activeStation.IsCooking => "В роботі",
                _ when IsStationContext(_context) => check.CanCraft ? GetActionLabel() : "Не вистачає",
                _ => check.CanCraft ? "Створити" : "Не вистачає"
            };
        }

        private string BuildDetailsText(CraftingRecipeData recipe, CraftingCheck check)
        {
            var lines = new List<string>
            {
                $"<b>{recipe.RecipeName}</b>",
                recipe.Output != null && recipe.Output.Item != null
                    ? $"Створює: {recipe.Output.Item.ItemName} x{recipe.Output.Amount}"
                    : "Результат: не налаштовано"
            };

            if (!string.IsNullOrWhiteSpace(recipe.Description))
                lines.Add(recipe.Description);

            if (IsStationContext(_context))
            {
                lines.Add("Час: 0 с");
                if (_activeStation != null && _activeStation.UsesFuel)
                    lines.Add($"Паливо: {Mathf.CeilToInt(recipe.FuelSecondsCost)} с");
            }

            lines.Add("");
            lines.Add("<b>Cost</b>");

            foreach (var ingredient in (recipe.Ingredients ?? Enumerable.Empty<CraftingItemAmount>()).Where(IsValidAmount))
            {
                int owned = GetOwnedItemCount(ingredient.Item);
                lines.Add($"{ingredient.Item.ItemName}: {owned}/{ingredient.Amount}");
            }

            if (recipe.SoulAshCost > 0)
                lines.Add($"Попіл душ: {GetOwnedSoulAsh()}/{recipe.SoulAshCost}");

            foreach (var requirement in (recipe.RequiredItems ?? Enumerable.Empty<CraftingItemAmount>()).Where(IsValidAmount))
            {
                int owned = GetOwnedItemCount(requirement.Item);
                lines.Add($"Потрібно {requirement.Item.ItemName}: {owned}/{requirement.Amount}");
            }

            if (!check.CanCraft)
            {
                lines.Add("");
                lines.Add("<color=#ff8d74>" + check.Message + "</color>");
            }

            return string.Join("\n", lines);
        }

        private void CraftSelected()
        {
            if (_selectedRecipe == null || _craftingService == null)
                return;

            if (IsStationContext(_context))
                _activeStation?.TryStartRecipe(_selectedRecipe, _inventory, _wallet, out _);
            else
                _craftingService.TryCraft(_selectedRecipe, out _);

            Refresh();
        }

        private CraftingCheck CheckRecipe(CraftingRecipeData recipe)
        {
            if (IsStationContext(_context))
            {
                if (_activeStation == null)
                {
                    var check = new CraftingCheck();
                    check.AddProblem("Станцію не вибрано.");
                    return check;
                }

                return _activeStation.CheckRecipe(recipe, _inventory, _wallet);
            }

            return _craftingService.Check(recipe);
        }

        private int GetOwnedItemCount(ItemData item)
        {
            int owned = _inventory != null ? _inventory.GetItemCount(item) : 0;
            if (IsStationContext(_context) && BaseResourceStorage.Active != null)
                owned += BaseResourceStorage.Active.GetItemCount(item);

            return owned;
        }

        private int GetOwnedSoulAsh()
        {
            return (_wallet != null ? _wallet.Amount : 0)
                + (IsStationContext(_context) && BaseResourceStorage.Active != null ? BaseResourceStorage.Active.SoulAshAmount : 0);
        }

        private void RefreshStationHeader()
        {
            bool showStation = IsStationContext(_context);
            if (_fuelText != null) _fuelText.gameObject.SetActive(showStation);
            if (_progressText != null) _progressText.gameObject.SetActive(showStation);
            if (_addFuelButton != null) _addFuelButton.gameObject.SetActive(showStation);

            if (!showStation)
                return;

            if (_activeStation == null)
            {
                _fuelText.text = "Паливо: станцію не вибрано";
                _progressText.text = "";
                _addFuelButton.interactable = false;
                _addFuelButtonText.text = "Додати деревину";
                return;
            }

            if (_activeStation.UsesFuel)
            {
                _fuelText.text = $"Паливо: {Mathf.FloorToInt(_activeStation.FuelSeconds)} / {Mathf.FloorToInt(_activeStation.MaxFuelSeconds)} с";
                _addFuelButton.gameObject.SetActive(true);
                _addFuelButton.interactable = _inventory != null && _activeStation.FuelSeconds < _activeStation.MaxFuelSeconds;
                _addFuelButtonText.text = "Додати деревину";
            }
            else
            {
                _fuelText.text = "Паливо не потрібне";
                _addFuelButton.gameObject.SetActive(false);
            }

            if (_activeStation.IsCooking)
            {
                string recipeName = _activeStation.ActiveRecipe != null ? _activeStation.ActiveRecipe.RecipeName : "Рецепт";
                _progressText.text = $"{GetWorkingLabel()}: {recipeName} {Mathf.CeilToInt(_activeStation.RemainingCraftSeconds)} с ({Mathf.RoundToInt(_activeStation.ActiveProgress01 * 100f)}%)";
            }
            else
            {
                _progressText.text = "Готово";
            }
        }

        private void AddStationFuel()
        {
            ResolveActiveStation();
            _activeStation?.TryAddFuel(_inventory);
            Refresh();
        }

        private void ResolveActiveStation()
        {
            var nextStation = IsStationContext(_context) && TimedCraftingStation.Active != null && TimedCraftingStation.Active.Context == _context
                ? TimedCraftingStation.Active
                : null;

            if (_activeStation == nextStation)
                return;

            if (_activeStation != null)
                _activeStation.Changed -= OnStationChanged;

            _activeStation = nextStation;

            if (_activeStation != null)
                _activeStation.Changed += OnStationChanged;
        }

        private void OnStationChanged()
        {
            if (_activeStation != null && _activeStation.IsCooking)
            {
                RefreshStationHeader();
                RefreshDetails();
                return;
            }

            Refresh();
        }

        private string GetTitle()
        {
            if (_activeStation != null)
                return _activeStation.DisplayName;

            return _context switch
            {
                CraftingContext.Workbench => "Верстак",
                CraftingContext.Campfire => "Багаття",
                CraftingContext.CharcoalPit => "Вуглярня",
                CraftingContext.Cauldron => "Казан",
                CraftingContext.Furnace => "Піч",
                CraftingContext.Anvil => "Ковадло",
                _ => "Ручне ремесло"
            };
        }

        private string GetActionLabel()
        {
            if (_activeStation != null)
                return _activeStation.ActionLabel;

            return _context switch
            {
                CraftingContext.CharcoalPit => "Випалити",
                CraftingContext.Cauldron => "Зварити",
                CraftingContext.Furnace => "Переплавити",
                CraftingContext.Anvil => "Викувати",
                CraftingContext.Campfire => "Приготувати",
                _ => "Створити"
            };
        }

        private string GetWorkingLabel()
        {
            if (_activeStation != null)
            {
                return _activeStation.ActionLabel switch
                {
                    "Приготувати" => "Готується",
                    "Випалити" => "Випалюється",
                    "Зварити" => "Вариться",
                    "Переплавити" => "Плавиться",
                    "Викувати" => "Кується",
                    _ => "В роботі"
                };
            }

            return "В роботі";
        }

        private static bool IsStationContext(CraftingContext context)
        {
            return context == CraftingContext.Campfire
                || context == CraftingContext.CharcoalPit
                || context == CraftingContext.Cauldron
                || context == CraftingContext.Furnace
                || context == CraftingContext.Anvil;
        }

        private void BuildLayout()
        {
            if (_built)
                return;

            _built = true;

            var root = CreateRect("CraftingRuntimeRoot", transform);
            _rootObject = root.gameObject;
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

            _titleText = CreateText(root, "CraftingTitle", 24, FontStyles.Bold);
            _walletText = CreateText(root, "SoulAshText", 16, FontStyles.Normal);
            _fuelText = CreateText(root, "StationFuelText", 16, FontStyles.Normal);
            _progressText = CreateText(root, "StationProgressText", 16, FontStyles.Normal);
            _addFuelButton = CreateButton(root, "Додати деревину");
            _addFuelButtonText = _addFuelButton.GetComponentInChildren<TMP_Text>();
            _addFuelButton.onClick.AddListener(AddStationFuel);

            var recipeScroll = CreateScrollArea(root, "RecipeList", 150f);
            _recipeListRoot = recipeScroll.content;

            var detailsObject = CreateRect("RecipeDetails", root);
            detailsObject.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var detailsImage = detailsObject.gameObject.AddComponent<Image>();
            detailsImage.color = new Color(0f, 0f, 0f, 0.18f);

            _detailsText = CreateText(detailsObject, "RecipeDetailsText", 16, FontStyles.Normal);
            var detailsRect = _detailsText.rectTransform;
            detailsRect.anchorMin = Vector2.zero;
            detailsRect.anchorMax = Vector2.one;
            detailsRect.offsetMin = new Vector2(10f, 10f);
            detailsRect.offsetMax = new Vector2(-10f, -10f);
            _detailsText.alignment = TextAlignmentOptions.TopLeft;

            _craftButton = CreateButton(root, "Створити");
            _craftButtonText = _craftButton.GetComponentInChildren<TMP_Text>();
            _craftButton.onClick.AddListener(CraftSelected);
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

        private static ScrollRect CreateScrollArea(Transform parent, string name, float preferredHeight)
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
            content.offsetMin = new Vector2(6f, 0f);
            content.offsetMax = new Vector2(-6f, 0f);

            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
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

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (_inventory != null)
                _inventory.OnInventoryChanged += Refresh;

            if (_wallet != null)
                _wallet.Changed += OnWalletChanged;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (_inventory != null)
                _inventory.OnInventoryChanged -= Refresh;

            if (_wallet != null)
                _wallet.Changed -= OnWalletChanged;

            if (_activeStation != null)
                _activeStation.Changed -= OnStationChanged;

            _activeStation = null;
            _subscribed = false;
        }

        private void OnWalletChanged(int amount)
        {
            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private static bool IsValidAmount(CraftingItemAmount amount)
        {
            return amount != null && amount.Item != null && amount.Amount > 0;
        }
    }
}
*/

using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CraftingPanelUI : MonoBehaviour
    {
        [Header("UI Prefabs (New Design)")]
        [SerializeField] private GameObject _recipeSlotPrefab;
        [SerializeField] private GameObject _ingredientSlotPrefab;

        private readonly List<GameObject> _spawnedRecipeSlots = new List<GameObject>();
        private readonly List<GameObject> _spawnedIngredientSlots = new List<GameObject>();

        private InventoryController _inventory;
        private SoulAshWallet _wallet;
        private CraftingService _craftingService;
        private TimedCraftingStation _activeStation;
        private List<CraftingRecipeData> _allRecipes = new List<CraftingRecipeData>();
        private CraftingContext _context = CraftingContext.Hand;
        private CraftingRecipeData _selectedRecipe;

        private Transform _recipeListRoot;
        private Transform _ingredientsListRoot;

        private TMP_Text _titleText;
        private TMP_Text _walletText;
        private TMP_Text _fuelText;
        private TMP_Text _progressText;
        private TMP_Text _detailsText;
        private Button _addFuelButton;
        private TMP_Text _addFuelButtonText;
        private Button _craftButton;
        private TMP_Text _craftButtonText;
        private bool _built;
        private bool _subscribed;
        private GameObject _rootObject;

        public void Initialize(InventoryController inventory, SoulAshWallet wallet, CraftingContext context)
        {
            Unsubscribe();
            _inventory = inventory;
            _wallet = wallet;
            _craftingService = new CraftingService(_inventory, _wallet);
            _allRecipes = CraftingService.LoadRecipes();
            BuildLayout();
            Subscribe();
            SetContext(context);
        }

        public void SetPanelVisible(bool visible)
        {
            if (_rootObject != null) _rootObject.SetActive(visible);
        }

        public void SetContext(CraftingContext context)
        {
            _context = context;
            ResolveActiveStation();
            Refresh();
        }

        public void Refresh()
        {
            if (!_built || _craftingService == null) return;

            ResolveActiveStation();
            _titleText.text = GetTitle();
            _walletText.text = $"Попіл душ: {(_wallet != null ? _wallet.Amount : 0)}";
            RefreshStationHeader();

            var contextRecipes = GetContextRecipes();
            if (_selectedRecipe == null || _selectedRecipe.Context != _context || !contextRecipes.Contains(_selectedRecipe))
                _selectedRecipe = contextRecipes.FirstOrDefault();

            RebuildRecipeButtons(contextRecipes);
            RefreshDetails();
        }

        private List<CraftingRecipeData> GetContextRecipes()
        {
            return _allRecipes.Where(x => x != null && x.Context == _context).OrderBy(x => x.RecipeName).ToList();
        }

        private void RebuildRecipeButtons(IReadOnlyList<CraftingRecipeData> recipes)
        {
            foreach (var slot in _spawnedRecipeSlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _spawnedRecipeSlots.Clear();

            foreach (var recipe in recipes)
            {
                if (_recipeSlotPrefab == null) return;

                var obj = Instantiate(_recipeSlotPrefab, _recipeListRoot);
                var button = obj.GetComponent<Button>();

                if (button != null)
                {
                    var capturedRecipe = recipe;
                    button.onClick.AddListener(() =>
                    {
                        _selectedRecipe = capturedRecipe;
                        Refresh();
                    });
                    button.interactable = !IsStationContext(_context) || (_activeStation != null && !_activeStation.IsCooking);
                }

                Transform iconTrans = obj.transform.Find("Icon");
                if (iconTrans != null && iconTrans.TryGetComponent<Image>(out var img) && recipe.Output?.Item != null)
                {
                    img.sprite = recipe.Output.Item.Icon;
                }

                _spawnedRecipeSlots.Add(obj);
            }
        }

        private void RefreshDetails()
        {
            foreach (var slot in _spawnedIngredientSlots)
            {
                if (slot != null) Destroy(slot.gameObject);
            }
            _spawnedIngredientSlots.Clear();

            if (_selectedRecipe == null)
            {
                _detailsText.text = "Немає доступних рецептів.";
                _craftButton.interactable = false;
                _craftButtonText.text = GetActionLabel();
                return;
            }

            CraftingCheck check = CheckRecipe(_selectedRecipe);
            _detailsText.text = BuildDetailsText(_selectedRecipe, check);

            _craftButton.interactable = check.CanCraft;
            _craftButtonText.text = _context switch
            {
                _ when IsStationContext(_context) && _activeStation != null && _activeStation.IsCooking => "В роботі",
                _ when IsStationContext(_context) => check.CanCraft ? GetActionLabel() : "Не вистачає",
                _ => check.CanCraft ? "Створити" : "Не вистачає"
            };

            if (_ingredientSlotPrefab != null && _ingredientsListRoot != null)
            {
                foreach (var ingredient in (_selectedRecipe.Ingredients ?? Enumerable.Empty<CraftingItemAmount>()).Where(IsValidAmount))
                {
                    SpawnIngredientSlot(ingredient.Item, ingredient.Amount, GetOwnedItemCount(ingredient.Item), false);
                }

                if (_selectedRecipe.Output != null && _selectedRecipe.Output.Item != null)
                {
                    SpawnArrow();
                    SpawnIngredientSlot(_selectedRecipe.Output.Item, _selectedRecipe.Output.Amount, _selectedRecipe.Output.Amount, true);
                }
            }
        }

        private void SpawnArrow()
        {
            var arrowObj = new GameObject("Arrow", typeof(RectTransform));
            arrowObj.transform.SetParent(_ingredientsListRoot, false);
            var text = arrowObj.AddComponent<TextMeshProUGUI>();
            text.text = "=>"; // ФІКС: Звичайна текстова стрілка, яка є в усіх шрифтах
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            var layout = arrowObj.AddComponent<LayoutElement>();
            layout.minWidth = 30f;
            _spawnedIngredientSlots.Add(arrowObj);
        }

        private void SpawnIngredientSlot(ItemData item, int required, int owned, bool isResult)
        {
            if (item == null) return;
            var obj = Instantiate(_ingredientSlotPrefab, _ingredientsListRoot);

            // ФІКС: Жорстко фіксуємо розмір нижніх іконок (щоб не розтягувало)
            var layout = obj.GetComponent<LayoutElement>();
            if (layout == null) layout = obj.AddComponent<LayoutElement>();
            layout.preferredWidth = 100f;
            layout.preferredHeight = 100f;

            Transform iconTrans = obj.transform.Find("Icon");
            if (iconTrans != null && iconTrans.TryGetComponent<Image>(out var img))
            {
                img.sprite = item.Icon;
            }

            var text = obj.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                if (isResult)
                {
                    text.text = $"x{required}";
                    text.color = new Color(1f, 0.88f, 0.35f);
                }
                else
                {
                    text.text = $"{owned}/{required}";
                    text.color = owned >= required ? Color.white : new Color(1f, 0.4f, 0.4f);
                }
            }
            _spawnedIngredientSlots.Add(obj);
        }

        private string BuildDetailsText(CraftingRecipeData recipe, CraftingCheck check)
        {
            var lines = new List<string> { $"<b>{recipe.RecipeName}</b>" };
            if (!string.IsNullOrWhiteSpace(recipe.Description)) lines.Add(recipe.Description);
            if (IsStationContext(_context) && _activeStation != null && _activeStation.UsesFuel)
                lines.Add($"Час: {Mathf.CeilToInt(recipe.FuelSecondsCost)} с");
            if (recipe.SoulAshCost > 0) lines.Add($"Попіл душ: {GetOwnedSoulAsh()}/{recipe.SoulAshCost}");
            if (!check.CanCraft) lines.Add("<color=#ff8d74>" + check.Message + "</color>");
            return string.Join("\n", lines);
        }

        private void CraftSelected()
        {
            if (_selectedRecipe == null || _craftingService == null) return;
            if (IsStationContext(_context)) _activeStation?.TryStartRecipe(_selectedRecipe, _inventory, _wallet, out _);
            else _craftingService.TryCraft(_selectedRecipe, out _);
            Refresh();
        }

        private CraftingCheck CheckRecipe(CraftingRecipeData recipe)
        {
            if (IsStationContext(_context))
            {
                if (_activeStation == null)
                {
                    var check = new CraftingCheck();
                    check.AddProblem("Станцію не вибрано.");
                    return check;
                }
                return _activeStation.CheckRecipe(recipe, _inventory, _wallet);
            }
            return _craftingService.Check(recipe);
        }

        private int GetOwnedItemCount(ItemData item)
        {
            int owned = _inventory != null ? _inventory.GetItemCount(item) : 0;
            if (IsStationContext(_context) && BaseResourceStorage.Active != null)
                owned += BaseResourceStorage.Active.GetItemCount(item);
            return owned;
        }

        private int GetOwnedSoulAsh()
        {
            return (_wallet != null ? _wallet.Amount : 0) + (IsStationContext(_context) && BaseResourceStorage.Active != null ? BaseResourceStorage.Active.SoulAshAmount : 0);
        }

        private void RefreshStationHeader()
        {
            bool showStation = IsStationContext(_context);
            if (_fuelText != null) _fuelText.gameObject.SetActive(showStation);
            if (_progressText != null) _progressText.gameObject.SetActive(showStation);
            if (_addFuelButton != null) _addFuelButton.gameObject.SetActive(showStation);
            if (!showStation) return;

            if (_activeStation == null)
            {
                _fuelText.text = "Паливо: станцію не вибрано";
                _progressText.text = "";
                _addFuelButton.interactable = false;
                _addFuelButtonText.text = "Додати деревину";
                return;
            }

            if (_activeStation.UsesFuel)
            {
                _fuelText.text = $"Паливо: {Mathf.FloorToInt(_activeStation.FuelSeconds)} / {Mathf.FloorToInt(_activeStation.MaxFuelSeconds)} с";
                _addFuelButton.gameObject.SetActive(true);
                _addFuelButton.interactable = _inventory != null && _activeStation.FuelSeconds < _activeStation.MaxFuelSeconds;
                _addFuelButtonText.text = "Додати деревину";
            }
            else
            {
                _fuelText.text = "Паливо не потрібне";
                _addFuelButton.gameObject.SetActive(false);
            }

            if (_activeStation.IsCooking)
            {
                string recipeName = _activeStation.ActiveRecipe != null ? _activeStation.ActiveRecipe.RecipeName : "Рецепт";
                _progressText.text = $"{GetWorkingLabel()}: {recipeName} {Mathf.CeilToInt(_activeStation.RemainingCraftSeconds)} с ({Mathf.RoundToInt(_activeStation.ActiveProgress01 * 100f)}%)";
            }
            else
            {
                _progressText.text = "Готово";
            }
        }

        private void AddStationFuel()
        {
            ResolveActiveStation();
            _activeStation?.TryAddFuel(_inventory);
            Refresh();
        }

        private void ResolveActiveStation()
        {
            var nextStation = IsStationContext(_context) && TimedCraftingStation.Active != null && TimedCraftingStation.Active.Context == _context ? TimedCraftingStation.Active : null;
            if (_activeStation == nextStation) return;
            if (_activeStation != null) _activeStation.Changed -= OnStationChanged;
            _activeStation = nextStation;
            if (_activeStation != null) _activeStation.Changed += OnStationChanged;
        }

        private void OnStationChanged()
        {
            if (_activeStation != null && _activeStation.IsCooking)
            {
                RefreshStationHeader();
                RefreshDetails();
                return;
            }
            Refresh();
        }

        private string GetTitle()
        {
            if (_activeStation != null) return _activeStation.DisplayName;
            return _context switch { CraftingContext.Workbench => "Верстак", CraftingContext.Campfire => "Багаття", CraftingContext.CharcoalPit => "Вуглярня", CraftingContext.Cauldron => "Казан", CraftingContext.Furnace => "Піч", CraftingContext.Anvil => "Ковадло", _ => "Ручне ремесло" };
        }

        private string GetActionLabel()
        {
            if (_activeStation != null) return _activeStation.ActionLabel;
            return _context switch { CraftingContext.CharcoalPit => "Випалити", CraftingContext.Cauldron => "Зварити", CraftingContext.Furnace => "Переплавити", CraftingContext.Anvil => "Викувати", CraftingContext.Campfire => "Приготувати", _ => "Створити" };
        }

        private string GetWorkingLabel()
        {
            if (_activeStation != null)
            {
                return _activeStation.ActionLabel switch { "Приготувати" => "Готується", "Випалити" => "Випалюється", "Зварити" => "Вариться", "Переплавити" => "Плавиться", "Викувати" => "Кується", _ => "В роботі" };
            }
            return "В роботі";
        }

        private static bool IsStationContext(CraftingContext context)
        {
            return context == CraftingContext.Campfire || context == CraftingContext.CharcoalPit || context == CraftingContext.Cauldron || context == CraftingContext.Furnace || context == CraftingContext.Anvil;
        }

        private void BuildLayout()
        {
            if (_built) return;
            _built = true;

            var root = CreateRect("CraftingRuntimeRoot", transform);
            _rootObject = root.gameObject;
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

            _titleText = CreateText(root, "CraftingTitle", 24, FontStyles.Bold);
            _walletText = CreateText(root, "SoulAshText", 16, FontStyles.Normal);
            _fuelText = CreateText(root, "StationFuelText", 16, FontStyles.Normal);
            _progressText = CreateText(root, "StationProgressText", 16, FontStyles.Normal);
            _addFuelButton = CreateButton(root, "Додати деревину");
            _addFuelButtonText = _addFuelButton.GetComponentInChildren<TMP_Text>();
            _addFuelButton.onClick.AddListener(AddStationFuel);

            var recipeScroll = CreateGridArea(root, "RecipeList", 220f);
            _recipeListRoot = recipeScroll.content;

            var detailsObject = CreateRect("RecipeDetails", root);
            detailsObject.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            var detailsImage = detailsObject.gameObject.AddComponent<Image>();
            detailsImage.color = new Color(0f, 0f, 0f, 0.18f);

            var detailsLayout = detailsObject.gameObject.AddComponent<VerticalLayoutGroup>();
            detailsLayout.padding = new RectOffset(10, 10, 10, 10);
            detailsLayout.spacing = 10f;
            detailsLayout.childControlWidth = true;
            detailsLayout.childControlHeight = true;
            detailsLayout.childForceExpandWidth = true;
            detailsLayout.childForceExpandHeight = false;

            _detailsText = CreateText(detailsObject, "RecipeDetailsText", 16, FontStyles.Normal);
            _detailsText.alignment = TextAlignmentOptions.TopLeft;

            var ingredientsRootRect = CreateRect("IngredientsList", detailsObject);
            ingredientsRootRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 90f;
            var hLayout = ingredientsRootRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;

            // ФІКС: Вимикаємо примусове розтягнення дочірніх елементів!
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            _ingredientsListRoot = ingredientsRootRect;

            _craftButton = CreateButton(root, "Створити");
            _craftButtonText = _craftButton.GetComponentInChildren<TMP_Text>();
            _craftButton.onClick.AddListener(CraftSelected);
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

        private static ScrollRect CreateGridArea(Transform parent, string name, float preferredHeight)
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
            content.offsetMin = new Vector2(6f, 0f);
            content.offsetMax = new Vector2(-6f, 0f);

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            // ФІКС: Робимо верхні слоти більшими
            grid.cellSize = new Vector2(120f, 120f);
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(10, 10, 10, 10);
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

        private void Subscribe()
        {
            if (_subscribed) return;
            if (_inventory != null) _inventory.OnInventoryChanged += Refresh;
            if (_wallet != null) _wallet.Changed += OnWalletChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;
            if (_wallet != null) _wallet.Changed -= OnWalletChanged;
            if (_activeStation != null) _activeStation.Changed -= OnStationChanged;
            _activeStation = null;
            _subscribed = false;
        }

        private void OnWalletChanged(int amount) => Refresh();
        private void OnDestroy() => Unsubscribe();
        private static bool IsValidAmount(CraftingItemAmount amount) => amount != null && amount.Item != null && amount.Amount > 0;
    }
}