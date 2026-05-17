namespace Project_S.Runtime.Gameplay.Crafting
{
    public class AnvilStation : TimedCraftingStation
    {
        protected override void Awake()
        {
            ConfigureStation(
                CraftingContext.Anvil,
                "Anvil",
                "Forge",
                false,
                null,
                0f,
                0f);

            base.Awake();
        }
    }
}
