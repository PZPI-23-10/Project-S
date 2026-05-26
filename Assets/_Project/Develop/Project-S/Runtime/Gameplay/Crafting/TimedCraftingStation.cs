using System.Collections.Generic;
using System.Linq;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class TimedCraftingStation : MonoBehaviour, IInteractable, ICraftingRecipeProvider
    {
        [SerializeField] private CraftingContext _context = CraftingContext.Campfire;
        [SerializeField] private string _displayName = "Station";
        [SerializeField] private string _actionLabel = "Craft";
        [SerializeField] private bool _usesFuel = true;
        [SerializeField] private ItemData _fuelItem;
        [SerializeField] private float _secondsPerFuelItem = 300f;
        [SerializeField] private float _maxFuelSeconds = 900f;
        [SerializeField] private float _fuelSeconds;
        [SerializeField] private BaseResourceStorage _baseStorage;
        [SerializeField] private List<CraftingRecipeData> _availableRecipes = new List<CraftingRecipeData>();

        private CraftingRecipeData _activeRecipe;
        private InventoryController _activeInventory;
        private float _activeDurationSeconds;
        private float _remainingCraftSeconds;

        public static TimedCraftingStation Active { get; private set; }

        public string InteractionPrompt => _displayName;
        public CraftingContext Context => _context;
        public string DisplayName => _displayName;
        public string ActionLabel => _actionLabel;
        public IReadOnlyList<CraftingRecipeData> AvailableRecipes => _availableRecipes;
        public bool UsesFuel => _usesFuel;
        public float FuelSeconds => _fuelSeconds;
        public float MaxFuelSeconds => _maxFuelSeconds;
        public float RemainingCraftSeconds => _remainingCraftSeconds;
        public float ActiveDurationSeconds => _activeDurationSeconds;
        public CraftingRecipeData ActiveRecipe => _activeRecipe;
        public bool IsCooking => _activeRecipe != null;
        public float ActiveProgress01 => _activeRecipe == null || _activeDurationSeconds <= 0f
            ? 0f
            : Mathf.Clamp01(1f - (_remainingCraftSeconds / _activeDurationSeconds));

        public event System.Action Changed;

        public void ConfigureStation(
            CraftingContext context,
            string displayName,
            string actionLabel,
            bool usesFuel,
            ItemData fuelItem,
            float secondsPerFuelItem,
            float maxFuelSeconds)
        {
            _context = context;
            _displayName = displayName;
            _actionLabel = actionLabel;
            _usesFuel = usesFuel;
            _fuelItem = fuelItem;
            _secondsPerFuelItem = secondsPerFuelItem;
            _maxFuelSeconds = maxFuelSeconds;
        }

        public bool AllowsRecipe(CraftingRecipeData recipe)
        {
            return recipe != null
                && recipe.Context == _context
                && _availableRecipes != null
                && _availableRecipes.Contains(recipe);
        }

        public void ConfigureRecipes(IEnumerable<CraftingRecipeData> recipes)
        {
            _availableRecipes = recipes?
                .Where(x => x != null)
                .ToList() ?? new List<CraftingRecipeData>();
        }

        protected virtual void Awake()
        {
            if (_usesFuel && _fuelItem == null)
                _fuelItem = Resources.Load<ItemData>("Crafting/Items/Resources/Wood");
        }

        protected virtual void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        protected virtual void Update()
        {
            Tick(Time.deltaTime);
        }

        public virtual void Interact(PlayerInteractor interactor)
        {
            Active = this;

            var inventoryUI = FindFirstObjectByType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogWarning("[Crafting] Inventory UI is missing.");
                return;
            }

            if (interactor != null)
            {
                inventoryUI.OpenWithCraftingContext(
                    _context,
                    transform,
                    interactor.transform,
                    interactor.MenuCloseDistance,
                    this);
            }
            else
            {
                inventoryUI.OpenWithCraftingContext(_context, this);
            }
        }

        public bool TryAddFuel(InventoryController inventory)
        {
            if (!_usesFuel || inventory == null || _fuelItem == null)
                return false;

            if (_fuelSeconds >= _maxFuelSeconds)
                return false;

            if (!inventory.TryRemoveItem(_fuelItem, 1))
                return false;

            _fuelSeconds = Mathf.Min(_maxFuelSeconds, _fuelSeconds + _secondsPerFuelItem);
            NotifyChanged();
            return true;
        }

        public CraftingCheck CheckRecipe(CraftingRecipeData recipe, InventoryController inventory, SoulAshWallet wallet)
        {
            var check = new CraftingCheck();

            if (IsCooking)
                check.AddProblem($"{_displayName} is already working.");

            if (recipe == null)
            {
                check.AddProblem("Recipe is missing.");
                return check;
            }

            if (recipe.Context != _context)
                check.AddProblem($"Recipe is not a {_displayName} recipe.");
            else if (!AllowsRecipe(recipe))
                check.AddProblem("Recipe is not available at this station.");

            if (_usesFuel && recipe.FuelSecondsCost > 0f && _fuelSeconds < recipe.FuelSecondsCost)
                check.AddProblem($"Need {Mathf.CeilToInt(recipe.FuelSecondsCost - _fuelSeconds)} more fuel seconds.");

            var crafting = new CraftingService(inventory, wallet, ResolveBaseStorage());
            var recipeCheck = crafting.Check(recipe);
            foreach (var problem in recipeCheck.Problems)
                check.AddProblem(problem);

            return check;
        }

        public bool TryStartRecipe(CraftingRecipeData recipe, InventoryController inventory, SoulAshWallet wallet, out CraftingCheck check)
        {
            check = CheckRecipe(recipe, inventory, wallet);
            if (!check.CanCraft)
                return false;

            var crafting = new CraftingService(inventory, wallet, ResolveBaseStorage());
            if (!crafting.TryConsumeCosts(recipe, out var consumeCheck))
            {
                foreach (var problem in consumeCheck.Problems)
                    check.AddProblem(problem);
                return false;
            }

            if (_usesFuel)
                _fuelSeconds = Mathf.Max(0f, _fuelSeconds - recipe.FuelSecondsCost);

            if (GetEffectiveCraftDuration(recipe) <= 0f)
            {
                GrantRecipeOutput(recipe, inventory);
                NotifyChanged();
                return true;
            }

            _activeRecipe = recipe;
            _activeInventory = inventory;
            _activeDurationSeconds = GetEffectiveCraftDuration(recipe);
            _remainingCraftSeconds = _activeDurationSeconds;
            NotifyChanged();

            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_activeRecipe == null || deltaTime <= 0f)
                return;

            _remainingCraftSeconds = Mathf.Max(0f, _remainingCraftSeconds - deltaTime);
            if (_remainingCraftSeconds <= 0f)
                CompleteRecipe();
            else
                NotifyChanged();
        }

        private void CompleteRecipe()
        {
            var completedRecipe = _activeRecipe;
            var targetInventory = _activeInventory;

            _activeRecipe = null;
            _activeInventory = null;
            _activeDurationSeconds = 0f;
            _remainingCraftSeconds = 0f;

            GrantRecipeOutput(completedRecipe, targetInventory);

            NotifyChanged();
        }

        private float GetEffectiveCraftDuration(CraftingRecipeData recipe)
        {
            return recipe != null ? Mathf.Max(0f, recipe.CraftDurationSeconds) : 0f;
        }

        private void GrantRecipeOutput(CraftingRecipeData recipe, InventoryController targetInventory)
        {
            if (recipe == null || recipe.Output == null || recipe.Output.Item == null || recipe.Output.Amount <= 0)
                return;

            WorldItemDropUtility.GrantOrDrop(
                recipe.Output.Item,
                recipe.Output.Amount,
                targetInventory,
                transform.position,
                "[Crafting]");
        }

        private BaseResourceStorage ResolveBaseStorage()
        {
            if (_baseStorage != null)
                return _baseStorage;

            return BaseResourceStorage.Active;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
