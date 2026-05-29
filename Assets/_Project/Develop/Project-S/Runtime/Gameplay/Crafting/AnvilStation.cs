namespace Project_S.Runtime.Gameplay.Crafting
{
    public class AnvilStation : TimedCraftingStation
    {
        protected override void Awake()
        {
            ConfigureStation(
                CraftingContext.Anvil,
                "Ковадло",
                "Викувати",
                false,
                null,
                0f,
                0f);

            base.Awake();
        }
    }
}
