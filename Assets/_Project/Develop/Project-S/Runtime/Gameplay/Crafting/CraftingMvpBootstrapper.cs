using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public static class CraftingMvpBootstrapper
    {
        public static bool TryBootstrap()
        {
            if (!IsYavWorldLoaded())
                return false;

            var inventory = Object.FindFirstObjectByType<InventoryController>();
            if (inventory == null)
                return false;

            if (inventory.GetComponent<SoulAshWallet>() == null)
                inventory.gameObject.AddComponent<SoulAshWallet>();

            if (inventory.GetComponent<BuffController>() == null)
                inventory.gameObject.AddComponent<BuffController>();

            if (inventory.GetComponent<HomeTeleportController>() == null)
                inventory.gameObject.AddComponent<HomeTeleportController>();

            var stats = inventory.GetComponent<CharacterStats>();
            if (stats != null && stats.Get(StatType.CarryWeight) < 50f)
                stats.Set(StatType.CarryWeight, 130f);

            return true;
        }

        private static bool IsYavWorldLoaded()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name == "YavWorld")
                    return true;
            }

            return false;
        }
    }
}
