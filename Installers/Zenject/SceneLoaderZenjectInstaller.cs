using SceneLoader.Internal.Loading;
using SceneLoader.Internal;
using Zenject;

namespace SceneLoader.Installers.Zenject
{
    /// <summary>
    /// Registers SceneLoader package services for Zenject.
    /// </summary>
    public sealed class SceneLoaderZenjectInstaller : Installer<SceneLoaderZenjectInstaller>
    {
        /// <summary>
        /// Adds SceneLoader package services to the current container.
        /// </summary>
        public override void InstallBindings()
        {
            Container
                .BindInterfacesTo<SceneLoadingProgress>()
                .AsSingle();

            Container
                .Bind<IAddressableSceneLoader>()
                .To<AddressableSceneLoader>()
                .AsSingle();

            Container
                .Bind<ISceneActivationState>()
                .To<UnitySceneActivationState>()
                .AsSingle();

            Container
                .Bind<ISceneLoaderService>()
                .To<SceneLoaderService>()
                .AsSingle();
        }
    }
}
