using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Inventory
{
    public static class WorldItemDropUtility
    {
        public static bool GrantOrDrop(
            ItemData item,
            int amount,
            InventoryController inventory,
            Vector3 origin,
            string logPrefix = "[Loot]",
            float dropRadius = 0.55f)
        {
            if (item == null || amount <= 0)
                return false;

            if (inventory != null && inventory.AddItem(item, amount))
            {
                Debug.Log($"{logPrefix} Added {item.ItemName} x{amount} to inventory.");
                return true;
            }

            SpawnPickup(item, amount, origin, dropRadius);
            Debug.Log($"{logPrefix} Dropped {item.ItemName} x{amount} as pickup.");
            return false;
        }

        public static ItemPickup SpawnPickup(ItemData item, int amount, Vector3 origin, float dropRadius = 0.55f)
        {
            if (item == null || amount <= 0)
                return null;

            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, dropRadius);
            Vector3 position = origin + new Vector3(offset.x, 0.45f, offset.y);

            GameObject pickupObject;

            if (item.WorldPickupPrefab != null)
            {
                pickupObject = Object.Instantiate(item.WorldPickupPrefab, position, Quaternion.identity);
            }
            else
            {
                GameObject defaultPrefab = Resources.Load<GameObject>("DefaultItemDrop");

                if (defaultPrefab != null)
                {
                    pickupObject = Object.Instantiate(defaultPrefab, position, Quaternion.identity);
                }
                else
                {
                    pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pickupObject.transform.position = position;
                    pickupObject.transform.localScale = Vector3.one * 0.35f;
                }
            }

            pickupObject.name = amount > 1
                ? $"{item.ItemName} x{amount} Pickup"
                : $"{item.ItemName} Pickup";

            var pickup = pickupObject.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = pickupObject.AddComponent<ItemPickup>();

            if (pickupObject.GetComponentInChildren<Collider>() == null)
                pickupObject.AddComponent<SphereCollider>();

            pickup.Item = item;
            pickup.Amount = amount;
            return pickup;
        }
    }
}