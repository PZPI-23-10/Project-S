using UnityEngine;

namespace Project_S.Runtime.Gameplay.Crafting
{
    public class CharcoalPitStation : TimedCraftingStation
    {
        protected override void Awake()
        {
            ConfigureStation(
                CraftingContext.CharcoalPit,
                "Вуглярня",
                "Випалити",
                false,
                null,
                0f,
                0f);

            base.Awake();
        }
    }
}
