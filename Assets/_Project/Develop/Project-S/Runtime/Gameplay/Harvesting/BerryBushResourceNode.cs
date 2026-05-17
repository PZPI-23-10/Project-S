using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public class BerryBushResourceNode : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemData _berryItem;
        [SerializeField] private int _minAmount = 3;
        [SerializeField] private int _maxAmount = 6;
        [SerializeField] private string _displayName = "Berry Bush";

        private bool _depleted;

        public string InteractionPrompt => _depleted ? $"{_displayName} (Empty)" : _displayName;
        public bool IsDepleted => _depleted;

        public void Configure(ItemData berryItem, int minAmount = 3, int maxAmount = 6, string displayName = "Berry Bush")
        {
            _berryItem = berryItem;
            _minAmount = Mathf.Max(0, minAmount);
            _maxAmount = Mathf.Max(_minAmount, maxAmount);
            _displayName = displayName;
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
            _depleted = true;
            MarkDepleted();
        }

        private void MarkDepleted()
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.18f, 0.24f, 0.16f);
        }
    }
}
