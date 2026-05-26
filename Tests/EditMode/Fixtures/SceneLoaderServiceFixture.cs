using System;
using SceneLoader.Internal;

namespace SceneLoader.Tests.EditMode
{
    /// <summary>
    /// Builds the scene loader test subject and its supporting fake loader.
    /// </summary>
    internal sealed class SceneLoaderServiceFixture : IDisposable
    {
        /// <summary>
        /// Gets the scene loader service under test.
        /// </summary>
        internal SceneLoaderService Service { get; }

        /// <summary>
        /// Gets the fake low-level loader used by the service under test.
        /// </summary>
        internal FakeAddressableSceneLoader Loader { get; }

        /// <summary>
        /// Gets the fake active-scene evaluator used by the service under test.
        /// </summary>
        internal FakeSceneActivationState ActivationState { get; }

        /// <summary>
        /// Creates the fixture with a fresh fake loader and service instance.
        /// </summary>
        internal SceneLoaderServiceFixture()
        {
            Loader = new FakeAddressableSceneLoader();
            ActivationState = new FakeSceneActivationState();
            Service = new SceneLoaderService(Loader, ActivationState);
        }

        /// <summary>
        /// Disposes the service under test.
        /// </summary>
        public void Dispose()
        {
            Service.Dispose();
        }
    }
}
