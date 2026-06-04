using System;
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

        public event Action<ItemPickup> Collected;

        public string InteractionActionText => _interactionActionText;
        public bool IsCollected { get; private set; }

        private void Start()
        {
            RefreshVisual();
        }

        public void Collect(InventoryController inventory)
        {
            if (inventory.AddItem(Item, Amount))
            {
                IsCollected = true;
                Collected?.Invoke(this);

                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        public void RestoreSaveState(ItemData item, int amount, bool collected)
        {
            Item = item;
            Amount = amount;
            IsCollected = collected;
            gameObject.SetActive(!collected);
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (_iconRenderer != null && Item != null && Item.Icon != null)
                _iconRenderer.sprite = Item.Icon;
        }
    }
}
