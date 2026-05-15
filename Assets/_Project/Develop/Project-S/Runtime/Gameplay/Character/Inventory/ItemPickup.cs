using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public class ItemPickup : MonoBehaviour
    {
        public ItemData Item;
        public int Amount = 1;

        public void Collect(InventoryController inventory)
        {
            if (inventory.AddItem(Item, Amount))
            {
                Destroy(gameObject); 
            }
        }
    }
}