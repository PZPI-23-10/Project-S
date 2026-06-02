using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public class ItemPickup : MonoBehaviour
    {
        public ItemData Item;
        public int Amount = 1;
        [SerializeField] private string _interactionActionText = "E - Подобрать";

        [Header("Візуал (для дефолтних предметів)")]
        [SerializeField] private SpriteRenderer _iconRenderer;

        public string InteractionActionText => _interactionActionText;

        private void Start()
        {
            if (_iconRenderer != null && Item != null && Item.Icon != null)
            {
                _iconRenderer.sprite = Item.Icon;
            }
        }

        public void Collect(InventoryController inventory)
        {
            if (inventory.AddItem(Item, Amount))
            {
                Destroy(gameObject);
            }
        }
    }
}