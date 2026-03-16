using Project_S.Runtime.Core.Services;
using UnityEngine;
using Zenject;

namespace Project_S.Runtime.Core
{
    public class CoreInstaller : MonoInstaller<CoreInstaller>
    {
        [SerializeField] private CameraProvider _cameraProvider;

        public override void InstallBindings()
        {
            Container.BindInstance(_cameraProvider).AsSingle().NonLazy();
        }
    }
}