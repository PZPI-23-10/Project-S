namespace Project_S.Runtime.Gameplay.Crafting
{
    public class FurnaceStation : TimedCraftingStation
    {
        protected override void Awake()
        {
            ConfigureStation(
                CraftingContext.Furnace,
                "Піч",
                "Переплавити",
                false,
                null,
                0f,
                0f);

            base.Awake();
        }
    }
}
