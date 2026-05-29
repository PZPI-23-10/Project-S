using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public class BerryBushResourceNode : MonoBehaviour, IInteractable, IInteractionActionText
    {
        [SerializeField] private ItemData _berryItem;
        [SerializeField] private int _minAmount = 1;
        [SerializeField] private int _maxAmount = 2;
        [SerializeField] private int _minHarvests = 1;
        [SerializeField] private int _maxHarvests = 2;
        [SerializeField] private string _displayName = "Berry Bush";
        [SerializeField] private string _interactionActionText = "E - Подобрать";

        // ==========================================
        // ДОДАНО: Звук збирання ягід
        // ==========================================
        [Header("Аудіо")]
        [SerializeField] private AudioClip _harvestSound; // Звук шурхоту листя
        // ==========================================

        private bool _depleted;
        private int _remainingHarvests;

        public string InteractionPrompt => _depleted ? $"{_displayName} (Empty)" : _displayName;
        public string InteractionActionText => _interactionActionText;
        public bool IsDepleted => _depleted;
        public int RemainingHarvests => _remainingHarvests;

        public void Configure(
            ItemData berryItem,
            int minAmount = 1,
            int maxAmount = 2,
            string displayName = "Berry Bush",
            int minHarvests = 1,
            int maxHarvests = 2)
        {
            _berryItem = berryItem;
            _minAmount = Mathf.Max(0, minAmount);
            _maxAmount = Mathf.Max(_minAmount, maxAmount);
            _displayName = displayName;
            _minHarvests = Mathf.Max(1, minHarvests);
            _maxHarvests = Mathf.Max(_minHarvests, maxHarvests);
            ResetHarvests();
            EnsureTriggerColliders();
        }

        private void Awake()
        {
            if (_remainingHarvests <= 0)
                ResetHarvests();

            EnsureTriggerColliders();
        }

        private void OnValidate()
        {
            EnsureTriggerColliders();
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (_depleted || _berryItem == null)
                return;

            int amount = Random.Range(Mathf.Max(0, _minAmount), Mathf.Max(_minAmount, _maxAmount) + 1);
            if (amount <= 0)
                return;

            InventoryController inventory = interactor != null ? interactor.Inventory : null;
            WorldItemDropUtility.GrantOrDrop(_berryItem, amount, inventory, transform.position, "[Harvesting]");
            _remainingHarvests = Mathf.Max(0, _remainingHarvests - 1);

            // ==========================================
            // ГРАЄМО ЗВУК ШУРХОТУ ЛИСТЯ
            // ==========================================
            if (_harvestSound != null)
            {
                AudioSource.PlayClipAtPoint(_harvestSound, transform.position, 0.8f);
            }
            // ==========================================

            if (_remainingHarvests <= 0)
            {
                _depleted = true;
                MarkDepleted();
            }
        }

        private void ResetHarvests()
        {
            int minHarvests = Mathf.Max(1, _minHarvests);
            int maxHarvests = Mathf.Max(minHarvests, _maxHarvests);
            _remainingHarvests = Random.Range(minHarvests, maxHarvests + 1);
            _depleted = false;
        }

        private void MarkDepleted()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.18f, 0.24f, 0.16f);
        }

        private void EnsureTriggerColliders()
        {
            foreach (var collider in GetComponentsInChildren<Collider>(true))
                collider.isTrigger = true;
        }
    }
}