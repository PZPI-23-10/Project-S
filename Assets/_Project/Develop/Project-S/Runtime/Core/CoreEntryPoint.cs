using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Player;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Core
{
    public class CoreEntryPoint : MonoBehaviour, IInitializable
    {
        [SerializeField] private PlayerFacade _playerFacade;
        
        [Inject] private PlayerProvider _playerProvider;

        public void Initialize()
        {
            //TODO: maybe spawn player from code
            _playerProvider.SetPlayer(_playerFacade);
        }
    }
}