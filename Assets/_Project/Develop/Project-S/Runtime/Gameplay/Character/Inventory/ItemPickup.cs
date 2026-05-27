using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public class ItemPickup : MonoBehaviour
    {
        public ItemData Item;
        public int Amount = 1;
        [SerializeField] private string _interactionActionText = "E - Подобрать";

        public string InteractionActionText => _interactionActionText;

        public void Collect(InventoryController inventory)
        {
            if (inventory.AddItem(Item, Amount))
            {
                Destroy(gameObject); 
            }
        }
    }
}
