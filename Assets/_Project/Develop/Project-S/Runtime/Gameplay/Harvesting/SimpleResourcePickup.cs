using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public class SimpleResourcePickup : MonoBehaviour, IInteractable, IInteractionActionText
    {
        [SerializeField] private ItemData _item;
        [SerializeField] private int _amount = 1;
        [SerializeField] private string _displayName;
        [SerializeField] private string _interactionActionText = "E - Подобрать";

        private bool _collected;

        public string InteractionPrompt
        {
            get
            {
                string itemName = !string.IsNullOrWhiteSpace(_displayName)
                    ? _displayName
                    : _item != null ? _item.ItemName : name;

                return _amount > 1 ? $"{itemName} x{_amount}" : itemName;
            }
        }

        public string InteractionActionText => _interactionActionText;

        public void Configure(ItemData item, int amount = 1, string displayName = null)
        {
            _item = item;
            _amount = Mathf.Max(1, amount);
            _displayName = displayName;
        }

        private void Awake()
        {
            if (_amount <= 0)
                _amount = 1;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (_collected || _item == null || _amount <= 0)
                return;

            _collected = true;
            InventoryController inventory = interactor != null ? interactor.Inventory : null;
            WorldItemDropUtility.GrantOrDrop(_item, _amount, inventory, transform.position, "[Harvesting]");

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }
    }
}
