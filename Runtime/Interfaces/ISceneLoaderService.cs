using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace SceneLoader
{
    /// <summary>
    /// Defines the contract for loading and transitioning Unity Addressables scenes by reference.
    /// </summary>
    public interface ISceneLoaderService : IDisposable
    {
        /// <summary>
        /// Gets the current externally visible load state for the target Addressables scene reference.
        /// </summary>
        /// <param name="sceneReference">Unity Addressables reference to inspect.</param>
        /// <returns>The current load state for the scene reference.</returns>
        public ESceneLoadState GetState(AssetReference sceneReference);

        /// <summary>
        /// Loads the target scene by its Unity Addressables reference.
        /// </summary>
        /// <param name="sceneReference">Unity Addressables reference to the target scene.</param>
        /// <param name="loadSceneMode">How the scene should be loaded. Defaults to <see cref="LoadSceneMode.Single"/>.</param>
        /// <param name="activateOnLoad">Whether the loaded scene should be activated immediately. Defaults to <see langword="true"/>.</param>
        /// <param name="reportProgress">Whether the load should publish progress into the shared <see cref="ISceneLoadingProgress"/> channel.</param>
        /// <returns>The scene load result.</returns>
        public UniTask<LoadResult> LoadScene(
            AssetReference sceneReference,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            bool reportProgress = false);

        /// <summary>
        /// Activates a scene that was previously preloaded through Addressables.
        /// </summary>
        /// <param name="sceneReference">Unity Addressables reference to the loaded scene.</param>
        /// <returns>The activation result.</returns>
        public UniTask<LoadResult> ActivateScene(AssetReference sceneReference);

        /// <summary>
        /// Unloads a loaded scene by its Unity Addressables reference.
        /// </summary>
        /// <param name="sceneReference">Unity Addressables reference to the loaded scene.</param>
        /// <returns>The unload result.</returns>
        public UniTask<LoadResult> UnloadScene(AssetReference sceneReference);
    }
}
