using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CampfireStation : TimedCraftingStation
    {
        public const float DefaultSecondsPerWood = 300f;
        public const float DefaultMaxFuelSeconds = 900f;

        public static new CampfireStation Active => TimedCraftingStation.Active as CampfireStation;

        public void Configure(ItemData woodItem)
        {
            ConfigureStation(
                CraftingContext.Campfire,
                "Багаття",
                "Приготувати",
                true,
                woodItem,
                DefaultSecondsPerWood,
                DefaultMaxFuelSeconds);
        }

        protected override void Awake()
        {
            Configure(Resources.Load<ItemData>("Crafting/Items/Resources/Wood"));
            base.Awake();
        }
    }
}
