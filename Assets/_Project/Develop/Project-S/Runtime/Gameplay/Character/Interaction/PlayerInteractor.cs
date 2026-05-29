using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Input;
using Project_S.Runtime.Gameplay.Crafting;
using Project_S.Runtime.Gameplay.HUD;
using Project_S.Runtime.Services.SceneManagement;
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
        private const int MaxInteractionHits = 16;
        private const string PickupActionText = "E - Підняти";
        private const string InteractActionText = "E - Взаємодіяти";

        [SerializeField] private float _interactDistance = 2.5f;
        [SerializeField] private InventoryController _inventory;
        [SerializeField] private float _promptWorldYOffset = 0.35f;
        [SerializeField] private float _menuCloseDistanceBuffer = 0.5f;
        [SerializeField] private string _pickupActionText = PickupActionText;
        [SerializeField] private string _interactActionText = InteractActionText;

        // ==========================================
        // ДОДАНО: Звук підняття предмета
        // ==========================================
        [Header("Аудіо")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _pickupSound;
        // ==========================================

        private UnityEngine.Camera _cam;
        private SoulAshWallet _soulAshWallet;
        private InventoryUI _inventoryUI;
        private InteractionHoverInfo _currentHover;
        private bool _hasCurrentHover;
        private IHoverableInteractable _hoveredInteractable;
        private readonly RaycastHit[] _interactionHits = new RaycastHit[MaxInteractionHits];

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

        private void OnEnable()
        {
            SceneTransitionRequestBus.TransitionStarted += HandleSceneTransitionStarted;
            SceneTransitionRequestBus.TransitionCompleted += HandleSceneTransitionCompleted;
        }

        private void OnDisable()
        {
            SceneTransitionRequestBus.TransitionStarted -= HandleSceneTransitionStarted;
            SceneTransitionRequestBus.TransitionCompleted -= HandleSceneTransitionCompleted;
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

            int hitCount = RaycastInteractionHits();
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _interactionHits[i];
                if (ShouldSkipInteractionHit(hit))
                    continue;

                if (TryCreateHoverInfo(hit, out hoverInfo))
                    return true;

                if (hit.collider != null && !hit.collider.isTrigger)
                    return false;
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
            WorldInteractionPromptUI.HideCurrent();
        }

        private void InteractWithCurrentHover()
        {
            if (!_hasCurrentHover && !TryGetHoverInfo(out _currentHover))
                return;

            if (_currentHover.Pickup != null)
            {
                if (_inventory != null)
                {
                    _currentHover.Pickup.Collect(_inventory);

                    // ==========================================
                    // ДОДАНО: Граємо звук підняття предмета
                    // ==========================================
                    if (_pickupSound != null && _audioSource != null)
                    {
                        _audioSource.pitch = Random.Range(0.9f, 1.15f);
                        _audioSource.PlayOneShot(_pickupSound);
                    }
                    // ==========================================
                }
            }
            else
            {
                _currentHover.Interactable?.Interact(this);
            }

            _hasCurrentHover = false;
            UpdateHoveredInteractable(null);
            WorldInteractionPromptUI.HideCurrent();
        }

        private void HandleSceneTransitionStarted()
        {
            ClearInteractionState();
            WorldInteractionPromptUI.HideCurrent();
        }

        private void HandleSceneTransitionCompleted()
        {
            _cam = null;
            _inventoryUI = null;
            ClearInteractionState();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            EnsureReferences();
        }

        private void ClearInteractionState()
        {
            _currentHover = default;
            _hasCurrentHover = false;
            UpdateHoveredInteractable(null);
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

            int hitCount = RaycastInteractionHits();
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _interactionHits[i];
                if (ShouldSkipInteractionHit(hit))
                    continue;

                foreach (var behaviour in hit.collider.GetComponentsInParent<MonoBehaviour>())
                {
                    if (behaviour is IInteractable)
                        return null;

                    if (behaviour is IHoverableInteractable hoverable)
                        return hoverable;
                }

                if (hit.collider != null && !hit.collider.isTrigger)
                    return null;
            }

            return null;
        }

        private int RaycastInteractionHits()
        {
            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _interactionHits,
                _interactDistance,
                ~0,
                QueryTriggerInteraction.Collide);

            System.Array.Sort(_interactionHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            return hitCount;
        }

        private bool ShouldSkipInteractionHit(RaycastHit hit)
        {
            return hit.collider == null || hit.collider.transform.root == transform.root;
        }

        private bool TryCreateHoverInfo(RaycastHit hit, out InteractionHoverInfo hoverInfo)
        {
            hoverInfo = default;

            if (hit.collider == null)
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

        private sealed class RaycastHitDistanceComparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit left, RaycastHit right)
            {
                return left.distance.CompareTo(right.distance);
            }
        }

        private string ResolvePickupActionText(ItemPickup pickup)
        {
            if (pickup != null && !string.IsNullOrWhiteSpace(pickup.InteractionActionText))
                return pickup.InteractionActionText;

            return !string.IsNullOrWhiteSpace(_pickupActionText)
                    ? _pickupActionText
                    : PickupActionText; // <-- прибрали Default
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
                : InteractActionText; // <-- прибрали Default
        }

        private bool ShouldSuppressHover()
        {
            if (_cam == null)
                return true;

            if (_inventoryUI != null && _inventoryUI.IsOpen)
                return true;

            return Application.isPlaying
                && Cursor.lockState != CursorLockMode.Locked
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
