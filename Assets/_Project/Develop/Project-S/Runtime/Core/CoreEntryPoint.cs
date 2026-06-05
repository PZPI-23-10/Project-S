using Project_S.Runtime.Common.Constants;
using Project_S.Runtime.Core.Services;
using Project_S.Runtime.Gameplay.Character.Player;
using Project_S.Runtime.Gameplay.Respawn;
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
            string initialLevel = _gameSaveService.BeginLoadOrStartNew(SceneNames.YavWorld);
            bool startNewGame = !_gameSaveService.HasSave;
            if (startNewGame)
                PlayerRespawnUtility.RestoreHealthToMax(_playerFacade != null ? _playerFacade.Stats : null);

            _sceneTransitionService.LoadInitialLevel(initialLevel, useNewGameSpawn: startNewGame);
        }
    }
}
