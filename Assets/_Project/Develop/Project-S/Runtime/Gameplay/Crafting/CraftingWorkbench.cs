using Project_S.Runtime.Gameplay.Character.Interaction;
using Project_S.Runtime.Gameplay.HUD;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CraftingWorkbench : MonoBehaviour, IInteractable
    {
        public string InteractionPrompt => "Верстак";

        public void Interact(PlayerInteractor interactor)
        {
            var inventoryUI = FindFirstObjectByType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogWarning("[Crafting] Inventory UI is missing.");
                return;
            }

            if (interactor != null)
            {
                inventoryUI.OpenWithCraftingContext(
                    CraftingContext.Workbench,
                    transform,
                    interactor.transform,
                    interactor.MenuCloseDistance);
            }
            else
            {
                inventoryUI.OpenWithCraftingContext(CraftingContext.Workbench);
            }
        }
    }
}
