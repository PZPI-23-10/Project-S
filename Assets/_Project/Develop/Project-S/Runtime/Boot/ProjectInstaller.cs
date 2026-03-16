using Project_S.Runtime.Common.EditorTools;
using Project_S.Runtime.Services.AssetManagement;
using Project_S.Runtime.Services.SceneManagement;
using Project_S.Runtime.Services.Storage;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Boot
{
    public class ProjectInstaller : MonoInstaller<ProjectInstaller>
    {
        [SerializeField] private PlayerStorage _playerStorage;

        public override void InstallBindings()
        {
#if UNITY_EDITOR
            SwitchToEntrySceneEditor.Init();
#endif
            
            Container.Bind<AssetLoader>().AsSingle();
            Container.Bind<SceneLoader>().AsSingle();

            Container.BindInstance(_playerStorage).AsSingle().NonLazy();
        }
    }
}