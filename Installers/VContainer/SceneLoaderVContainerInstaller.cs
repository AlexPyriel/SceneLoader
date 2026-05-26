using SceneLoader.Internal.Loading;
using SceneLoader.Internal;
using VContainer;
using VContainer.Unity;

namespace SceneLoader.Installers.VContainer
{
    /// <summary>
    /// Registers SceneLoader package services for VContainer.
    /// </summary>
    public sealed class SceneLoaderVContainerInstaller : IInstaller
    {
        /// <summary>
        /// Adds SceneLoader package services to the container.
        /// </summary>
        /// <param name="builder">Container builder used for service registrations.</param>
        public void Install(IContainerBuilder builder)
        {
            builder
                .Register<SceneLoadingProgress>(Lifetime.Singleton)
                .AsImplementedInterfaces();

            builder
                .Register<AddressableSceneLoader>(Lifetime.Singleton)
                .As<IAddressableSceneLoader>();

            builder
                .Register<UnitySceneActivationState>(Lifetime.Singleton)
                .As<ISceneActivationState>();

            builder
                .Register<SceneLoaderService>(Lifetime.Singleton)
                .As<ISceneLoaderService>();
        }
    }
}
