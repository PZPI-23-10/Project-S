using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public enum MvpSceneObjectKind
    {
        ResourceChest = 0,
        Workbench = 1,
        Campfire = 2,
        FoodChest = 3,
        CharcoalPit = 4,
        Cauldron = 5,
        Furnace = 6,
        Anvil = 7,
        ForgeChest = 8,
        BaseStorage = 9,
        GeneralStorage = 10
    }

    public class MvpSceneObjectAuthoring : MonoBehaviour
    {
        [SerializeField] private MvpSceneObjectKind _kind;
        [SerializeField] private Color _runtimeColor = Color.white;

        private void Awake()
        {
            EnsureCollider();
            ApplyRuntimeColor();
            ConfigureGameplayComponent();
        }

        private void ConfigureGameplayComponent()
        {
            switch (_kind)
            {
                case MvpSceneObjectKind.ResourceChest:
                    ConfigureResourceChest();
                    break;
                case MvpSceneObjectKind.Workbench:
                    EnsureComponent<CraftingWorkbench>();
                    break;
                case MvpSceneObjectKind.Campfire:
                    EnsureComponent<CampfireStation>();
                    break;
                case MvpSceneObjectKind.FoodChest:
                    ConfigureFoodChest();
                    break;
                case MvpSceneObjectKind.CharcoalPit:
                    EnsureComponent<CharcoalPitStation>();
                    break;
                case MvpSceneObjectKind.Cauldron:
                    EnsureComponent<CauldronStation>();
                    break;
                case MvpSceneObjectKind.Furnace:
                    EnsureComponent<FurnaceStation>();
                    break;
                case MvpSceneObjectKind.Anvil:
                    EnsureComponent<AnvilStation>();
                    break;
                case MvpSceneObjectKind.ForgeChest:
                    ConfigureForgeChest();
                    break;
                case MvpSceneObjectKind.BaseStorage:
                    EnsureComponent<BaseResourceStorage>();
                    break;
                case MvpSceneObjectKind.GeneralStorage:
                    EnsureComponent<GeneralItemStorage>();
                    break;
            }
        }

        private void ConfigureResourceChest()
        {
            EnsureComponent<TestResourceChest>().Configure(
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
                "[DEBUG] Resource Crate");
        }

        private void ConfigureFoodChest()
        {
            EnsureComponent<TestResourceChest>().Configure(
                new[]
                {
                    new ItemGrant(LoadItem("Crafting/Items/Consumables/Berry"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Consumables/GreyMeat"), 70),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Bone"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Leather"), 10),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Wood"), 30),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/Gromovytsia"), 12),
                    new ItemGrant(LoadItem("Crafting/Items/Resources/PetrifiedBlood"), 5)
                },
                1200,
                true,
                "[DEBUG] Food Crate");
        }

        private void ConfigureForgeChest()
        {
            EnsureComponent<TestResourceChest>().Configure(
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
                "[DEBUG] Forge Crate");
        }

        private void EnsureCollider()
        {
            if (GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();
        }

        private void ApplyRuntimeColor()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.material.color = _runtimeColor;
        }

        private T EnsureComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static ItemData LoadItem(string path)
        {
            return Resources.Load<ItemData>(path);
        }
    }
}
