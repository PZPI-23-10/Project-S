using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;

namespace Project_S.Runtime.Core.Services
{
    public class PlayerProvider : MonoBehaviour
    {
        public PlayerFacade Player { get; private set; }

        public void SetPlayer(PlayerFacade playerFacade)
        {
            Player = playerFacade;
        }
    }
}