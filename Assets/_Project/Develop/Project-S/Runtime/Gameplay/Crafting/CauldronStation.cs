using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CauldronStation : TimedCraftingStation
    {
        public const float DefaultSecondsPerWood = 300f;
        public const float DefaultMaxFuelSeconds = 1200f;

        protected override void Awake()
        {
            Configure(Resources.Load<ItemData>("Crafting/Items/Resources/Wood"));
            base.Awake();
        }

        public void Configure(ItemData woodItem)
        {
            ConfigureStation(
                CraftingContext.Cauldron,
                "Cauldron",
                "Brew",
                true,
                woodItem,
                DefaultSecondsPerWood,
                DefaultMaxFuelSeconds);
        }
    }
}
