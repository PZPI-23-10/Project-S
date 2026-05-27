using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    [Serializable]
    public class ItemGrant
    {
        public ItemData Item;
        public int Amount = 1;

        public ItemGrant() { }

        public ItemGrant(ItemData item, int amount)
        {
            Item = item;
            Amount = amount;
        }
    }

    public class TestResourceChest : MonoBehaviour, IInteractable
    {
        [SerializeField] private List<ItemGrant> _itemGrants = new List<ItemGrant>();
        [SerializeField] private int _soulAshGrant = 500;
        [SerializeField] private bool _grantOnce = true;
        [SerializeField] private string _interactionPrompt = "Скриня ресурсів";

        private bool _granted;

        public string InteractionPrompt => _interactionPrompt;

        public void Configure(IEnumerable<ItemGrant> itemGrants, int soulAshGrant, bool grantOnce, string interactionPrompt = null)
        {
            _itemGrants = new List<ItemGrant>(itemGrants);
            _soulAshGrant = soulAshGrant;
            _grantOnce = grantOnce;
            if (!string.IsNullOrWhiteSpace(interactionPrompt))
                _interactionPrompt = interactionPrompt;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (_grantOnce && _granted)
            {
                Debug.Log("[Crafting] Test resource chest is empty.");
                return;
            }

            if (interactor == null || interactor.Inventory == null)
            {
                Debug.LogWarning("[Crafting] Cannot grant resources without player inventory.");
                return;
            }

            foreach (var grant in _itemGrants)
            {
                if (grant == null || grant.Item == null || grant.Amount <= 0)
                    continue;

                if (!interactor.Inventory.CanAddItem(grant.Item, grant.Amount))
                {
                    Debug.LogWarning("[Crafting] Not enough inventory space for the test resource grant.");
                    return;
                }
            }

            foreach (var grant in _itemGrants)
            {
                if (grant == null || grant.Item == null || grant.Amount <= 0)
                    continue;

                interactor.Inventory.AddItem(grant.Item, grant.Amount);
            }

            interactor.SoulAshWallet?.Add(_soulAshGrant);
            _granted = true;
            Debug.Log("[Crafting] Test resources granted.");
        }
    }
}
