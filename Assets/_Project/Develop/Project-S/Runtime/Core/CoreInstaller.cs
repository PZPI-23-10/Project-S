using Project_S.Runtime.Core.Services;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Core
{
    public class CoreInstaller : MonoInstaller<CoreInstaller>
    {
        [SerializeField] private CameraProvider _cameraProvider;
        [SerializeField] private PlayerProvider _playerProvider;
        
        [SerializeField] private CoreEntryPoint _entryPoint;

        public override void InstallBindings()
        {
            Container
                .BindInterfacesTo<CoreEntryPoint>()
                .FromInstance(_entryPoint)
                .AsSingle();
            
            Container.BindInstance(_cameraProvider).AsSingle().NonLazy();
            Container.BindInstance(_playerProvider).AsSingle().NonLazy();
        }
    }
}