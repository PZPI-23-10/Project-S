using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Services.Save;
using Project_S.Runtime.Services.SceneManagement;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Core
{
    public class CoreEntryPoint : MonoBehaviour, IInitializable
    {
        [SerializeField] private PlayerFacade _playerFacade;

        [Inject] private PlayerProvider _playerProvider;
        [Inject] private SceneTransitionService _sceneTransitionService;
        [Inject] private GameSaveService _gameSaveService;

        public void Initialize()
        {
            //TODO: maybe spawn player from code
            _playerProvider.SetPlayer(_playerFacade);
            _sceneTransitionService.LoadInitialLevel(_gameSaveService.BeginLoadOrStartNew(SceneNames.YavWorld));
        }
    }
}
