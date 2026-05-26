using SceneLoader.Internal;
using SceneLoader.Internal.Loading;
using Reflex.Core;
using UnityEngine;

namespace SceneLoader.Installers.Reflex
{
    /// <summary>
    /// Registers SceneLoader package services for Reflex.
    /// </summary>
    public sealed class SceneLoaderReflexInstaller : MonoBehaviour, IInstaller
    {
        /// <summary>
        /// Adds SceneLoader package services to the current Reflex container.
        /// </summary>
        /// <param name="builder">Container builder used for service registrations.</param>
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddSingleton(
                typeof(SceneLoadingProgress),
                typeof(ISceneLoadingProgress),
                typeof(ISceneLoadingProgressReporter));

            builder.AddSingleton(
                typeof(AddressableSceneLoader),
                typeof(IAddressableSceneLoader));

            builder.AddSingleton(
                typeof(UnitySceneActivationState),
                typeof(ISceneActivationState));

            builder.AddSingleton(
                typeof(SceneLoaderService),
                typeof(ISceneLoaderService));
        }
    }
}
