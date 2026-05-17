using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public static class CraftingMvpBootstrapper
    {
        private const string ChestName = "[DEBUG] Resource Crate";
        private const string WorkbenchName = "[MVP] Workbench";
        private const string CampfireName = "[MVP] Campfire";
        private const string FoodChestName = "[DEBUG] Food Crate";
        private const string CharcoalPitName = "[MVP] Charcoal Pit";
        private const string CauldronName = "[MVP] Cauldron";
        private const string FurnaceName = "[MVP] Furnace";
        private const string AnvilName = "[MVP] Anvil";
        private const string ForgeChestName = "[DEBUG] Forge Crate";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindFirstObjectByType<CraftingMvpBootstrapperRunner>() != null)
                return;

            var runner = new GameObject("[MVP] Crafting Bootstrapper").AddComponent<CraftingMvpBootstrapperRunner>();
            Object.DontDestroyOnLoad(runner.gameObject);
        }

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

            if (GameObject.Find(ChestName) == null)
                SpawnResourceChest(inventory.transform);

            if (GameObject.Find(WorkbenchName) == null)
                SpawnWorkbench(inventory.transform);

            if (GameObject.Find(CampfireName) == null)
                SpawnCampfire(inventory.transform);

            if (GameObject.Find(FoodChestName) == null)
                SpawnFoodChest(inventory.transform);

            if (GameObject.Find(CharcoalPitName) == null)
                SpawnCharcoalPit(inventory.transform);

            if (GameObject.Find(CauldronName) == null)
                SpawnCauldron(inventory.transform);

            if (GameObject.Find(FurnaceName) == null)
                SpawnFurnace(inventory.transform);

            if (GameObject.Find(AnvilName) == null)
                SpawnAnvil(inventory.transform);

            if (GameObject.Find(ForgeChestName) == null)
                SpawnForgeChest(inventory.transform);

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

        private static void SpawnResourceChest(Transform player)
        {
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = ChestName;
            chest.transform.position = player.position + player.forward * 1.4f - player.right * 4.1f + Vector3.up * 0.5f;
            chest.transform.localScale = new Vector3(0.9f, 0.7f, 0.9f);

            var renderer = chest.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.35f, 0.22f, 0.12f);

            var resourceChest = chest.AddComponent<TestResourceChest>();
            resourceChest.Configure(
                new[]
                {
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Wood"), 40),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Stone"), 20),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Leather"), 20),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Bone"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Flint"), 20)
                },
                500,
                true,
                ChestName);
        }

        private static void SpawnWorkbench(Transform player)
        {
            var workbench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            workbench.name = WorkbenchName;
            workbench.transform.position = player.position + player.forward * 2f - player.right * 0.9f + Vector3.up * 0.45f;
            workbench.transform.localScale = new Vector3(1.6f, 0.45f, 0.9f);

            var renderer = workbench.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.22f, 0.16f, 0.1f);

            workbench.AddComponent<CraftingWorkbench>();
        }

        private static void SpawnCampfire(Transform player)
        {
            var campfire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            campfire.name = CampfireName;
            campfire.transform.position = player.position + player.forward * 3.4f + Vector3.up * 0.2f;
            campfire.transform.localScale = new Vector3(0.9f, 0.2f, 0.9f);

            var renderer = campfire.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.75f, 0.24f, 0.08f);

            campfire.AddComponent<CampfireStation>().Configure(LoadItem("Crafting/Items/Resources/Wood"));
        }

        private static void SpawnFoodChest(Transform player)
        {
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = FoodChestName;
            chest.transform.position = player.position + player.forward * 2.8f - player.right * 4.1f + Vector3.up * 0.5f;
            chest.transform.localScale = new Vector3(0.8f, 0.65f, 0.8f);

            var renderer = chest.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.18f, 0.33f, 0.18f);

            var resourceChest = chest.AddComponent<TestResourceChest>();
            resourceChest.Configure(
                new[]
                {
                    new ItemGrant(LoadItem("Crafting/Items/Consumables/Berry"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Consumables/GreyMeat"), 20),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Bone"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Leather"), 10),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Wood"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Gromovytsia"), 12),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/PetrifiedBlood"), 5)
                },
                1200,
                true,
                FoodChestName);
        }

        private static void SpawnCharcoalPit(Transform player)
        {
            var pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pit.name = CharcoalPitName;
            pit.transform.position = player.position + player.forward * 4.4f - player.right * 1.15f + Vector3.up * 0.15f;
            pit.transform.localScale = new Vector3(0.95f, 0.15f, 0.95f);

            var renderer = pit.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.08f, 0.08f, 0.075f);

            pit.AddComponent<CharcoalPitStation>();
        }

        private static void SpawnCauldron(Transform player)
        {
            var cauldron = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cauldron.name = CauldronName;
            cauldron.transform.position = player.position + player.forward * 4.4f + player.right * 1.15f + Vector3.up * 0.55f;
            cauldron.transform.localScale = new Vector3(0.9f, 0.65f, 0.9f);

            var renderer = cauldron.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.06f, 0.09f, 0.1f);

            cauldron.AddComponent<CauldronStation>().Configure(LoadItem("Crafting/Items/Resources/Wood"));
        }

        private static void SpawnFurnace(Transform player)
        {
            var furnace = GameObject.CreatePrimitive(PrimitiveType.Cube);
            furnace.name = FurnaceName;
            furnace.transform.position = player.position + player.forward * 5.6f - player.right * 1.15f + Vector3.up * 0.55f;
            furnace.transform.localScale = new Vector3(1.05f, 1.1f, 0.9f);

            var renderer = furnace.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.18f, 0.16f, 0.14f);

            furnace.AddComponent<FurnaceStation>();
        }

        private static void SpawnAnvil(Transform player)
        {
            var anvil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anvil.name = AnvilName;
            anvil.transform.position = player.position + player.forward * 5.6f + player.right * 1.15f + Vector3.up * 0.35f;
            anvil.transform.localScale = new Vector3(1.1f, 0.45f, 0.65f);

            var renderer = anvil.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.16f, 0.17f, 0.18f);

            anvil.AddComponent<AnvilStation>();
        }

        private static void SpawnForgeChest(Transform player)
        {
            var chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = ForgeChestName;
            chest.transform.position = player.position + player.forward * 4.2f - player.right * 4.1f + Vector3.up * 0.5f;
            chest.transform.localScale = new Vector3(0.85f, 0.65f, 0.85f);

            var renderer = chest.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.22f, 0.22f, 0.24f);

            var resourceChest = chest.AddComponent<TestResourceChest>();
            resourceChest.Configure(
                new[]
                {
                    new ItemGrant(LoadItem("Crafting/Items/Resources/IronOre"), 24),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Charcoal"), 24),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Wood"), 12),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Leather"), 8),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Gromovytsia"), 4)
                },
                1000,
                true,
                ForgeChestName);
        }

        private static ItemData LoadItem(string path)
        {
            var item = Resources.Load<ItemData>(path);
            if (item == null)
                Debug.LogWarning($"[Crafting] Missing item resource at Resources/{path}.");

            return item;
        }
    }
}
