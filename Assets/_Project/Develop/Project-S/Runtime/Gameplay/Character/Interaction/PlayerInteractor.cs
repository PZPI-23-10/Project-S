using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project_S.Runtime.Gameplay.Character.Interaction
{
    public struct InteractionHoverInfo
    {
        public string Title;
        public string ActionText;
        public Vector3 PromptWorldPosition;
        public ItemPickup Pickup;
        public IInteractable Interactable;

        public InteractionHoverInfo(
            string title,
            string actionText,
            Vector3 promptWorldPosition,
            ItemPickup pickup,
            IInteractable interactable)
        {
            Title = title;
            ActionText = actionText;
            PromptWorldPosition = promptWorldPosition;
            Pickup = pickup;
            Interactable = interactable;
        }
    }

    public class PlayerInteractor : MonoBehaviour
    {
        private const string DefaultPickupActionText = "E - Подобрать";
        private const string DefaultInteractActionText = "E - Взаимодействовать";

        [SerializeField] private float _interactDistance = 2.5f;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private float _promptWorldYOffset = 0.35f;
        [SerializeField] private float _menuCloseDistanceBuffer = 0.5f;
        [SerializeField] private string _pickupActionText = DefaultPickupActionText;
        [SerializeField] private string _interactActionText = DefaultInteractActionText;

        private UnityEngine.Camera _cam;
        private SoulAshWallet _soulAshWallet;
        private InventoryUI _inventoryUI;
        private InteractionHoverInfo _currentHover;
        private bool _hasCurrentHover;
        private IHoverableInteractable _hoveredInteractable;

        public InventoryController Inventory => _inventory;
        public float InteractDistance => _interactDistance;
        public float MenuCloseDistance => Mathf.Max(_interactDistance, _interactDistance + _menuCloseDistanceBuffer);

        public SoulAshWallet SoulAshWallet
        {
            get
            {
                if (_soulAshWallet == null && _inventory != null)
                {
                    _soulAshWallet = _inventory.GetComponent<SoulAshWallet>();
                    if (_soulAshWallet == null)
                        _soulAshWallet = _inventory.gameObject.AddComponent<SoulAshWallet>();
                }

                return _soulAshWallet;
            }
        }

        private void Awake()
        {
            EnsureReferences();
        }

        public void Tick(PlayerInputSnapshot input)
        {
            RefreshHoverPrompt();

            if (input.InteractPressed)
                InteractWithCurrentHover();
        }

        public bool TryGetHoverInfo(out InteractionHoverInfo hoverInfo)
        {
            hoverInfo = default;
            EnsureReferences();

            if (ShouldSuppressHover())
                return false;

            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, _interactDistance, ~0, QueryTriggerInteraction.Collide))
                return false;

            var pickup = hit.collider.GetComponentInParent<ItemPickup>();
            if (pickup != null && pickup.Item != null)
            {
                string title = pickup.Item.ItemName;
                if (pickup.Amount > 1)
                    title += $" x{pickup.Amount}";

                hoverInfo = new InteractionHoverInfo(
                    title,
                    ResolvePickupActionText(pickup),
                    PromptPosition(hit.collider, hit.point),
                    pickup,
                    null);
                return true;
            }

            foreach (var behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IInteractable interactable)
                {
                    hoverInfo = new InteractionHoverInfo(
                        interactable.InteractionPrompt,
                        ResolveInteractActionText(interactable),
                        PromptPosition(hit.collider, hit.point),
                        null,
                        interactable);
                    return true;
                }
            }

            return false;
        }

        private void RefreshHoverPrompt()
        {
            if (TryGetHoverInfo(out var hoverInfo))
            {
                UpdateHoveredInteractable(hoverInfo.Interactable as IHoverableInteractable);
                _currentHover = hoverInfo;
                _hasCurrentHover = true;
                WorldInteractionPromptUI.GetOrCreate()?.Show(
                    hoverInfo.PromptWorldPosition,
                    hoverInfo.Title,
                    hoverInfo.ActionText,
                    _cam);
                return;
            }

            _hasCurrentHover = false;
            UpdateHoveredInteractable(FindHoveredOnlyInteractable());
            WorldInteractionPromptUI.Instance?.Hide();
        }

        private void InteractWithCurrentHover()
        {
            if (!_hasCurrentHover && !TryGetHoverInfo(out _currentHover))
                return;

            if (_currentHover.Pickup != null)
            {
                if (_inventory != null)
                    _currentHover.Pickup.Collect(_inventory);
            }
            else
            {
                _currentHover.Interactable?.Interact(this);
            }

            _hasCurrentHover = false;
            UpdateHoveredInteractable(null);
            WorldInteractionPromptUI.Instance?.Hide();
        }

        private void UpdateHoveredInteractable(IHoverableInteractable hoverable)
        {
            if (ReferenceEquals(_hoveredInteractable, hoverable))
                return;

            _hoveredInteractable?.SetHovered(false);
            _hoveredInteractable = hoverable;
            _hoveredInteractable?.SetHovered(true);
        }

        private IHoverableInteractable FindHoveredOnlyInteractable()
        {
            EnsureReferences();

            if (ShouldSuppressHover())
                return null;

            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, _interactDistance, ~0, QueryTriggerInteraction.Collide))
                return null;

            foreach (var behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IInteractable)
                    return null;

                if (behaviour is IHoverableInteractable hoverable)
                    return hoverable;
            }

            return null;
        }

        private string ResolvePickupActionText(ItemPickup pickup)
        {
            if (pickup != null && !string.IsNullOrWhiteSpace(pickup.InteractionActionText))
                return pickup.InteractionActionText;

            return !string.IsNullOrWhiteSpace(_pickupActionText)
                ? _pickupActionText
                : DefaultPickupActionText;
        }

        private string ResolveInteractActionText(IInteractable interactable)
        {
            if (interactable is IInteractionActionText customAction
                && !string.IsNullOrWhiteSpace(customAction.InteractionActionText))
            {
                return customAction.InteractionActionText;
            }

            return !string.IsNullOrWhiteSpace(_interactActionText)
                ? _interactActionText
                : DefaultInteractActionText;
        }

        private bool ShouldSuppressHover()
        {
            if (_cam == null)
                return true;

            if (_inventoryUI != null && _inventoryUI.IsOpen)
                return true;

            return Application.isPlaying
                && Cursor.visible
                && EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject();
        }

        private Vector3 PromptPosition(Collider hitCollider, Vector3 fallbackPoint)
        {
            if (hitCollider == null)
                return fallbackPoint + Vector3.up * _promptWorldYOffset;

            Bounds bounds = hitCollider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y + _promptWorldYOffset, bounds.center.z);
        }

        private void EnsureReferences()
        {
            if (_cam == null)
                _cam = GetComponent<UnityEngine.Camera>() ?? GetComponentInChildren<UnityEngine.Camera>() ?? UnityEngine.Camera.main;

            if (_inventory == null)
                _inventory = GetComponentInParent<InventoryController>();

            if (_inventoryUI == null)
                _inventoryUI = Object.FindFirstObjectByType<InventoryUI>();
        }
    }
}
