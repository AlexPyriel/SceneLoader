using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.Loading
{
    /// <summary>
    /// Performs low-level scene loading, activation, and unloading operations via Addressable.
    /// </summary>
    internal sealed class AddressableSceneLoader : IAddressableSceneLoader
    {
        private readonly ISceneLoadingProgressReporter _sceneLoadingProgressReporter;

        /// <summary>
        /// Creates the internal scene loader with a progress receiver.
        /// </summary>
        /// <param name="sceneLoadingProgressReporter">Package-owned writable progress sink.</param>
        internal AddressableSceneLoader(ISceneLoadingProgressReporter sceneLoadingProgressReporter)
        {
            _sceneLoadingProgressReporter = sceneLoadingProgressReporter;
        }

        /// <summary>
        /// Loads a scene by Addressable reference and optionally activates it immediately.
        /// </summary>
        /// <param name="sceneReference">Addressable scene reference.</param>
        /// <param name="loadSceneMode">Scene load mode.</param>
        /// <param name="activateOnLoad">Whether the scene should be activated when loaded.</param>
        /// <param name="reportProgress">Whether loading progress should be published to the shared reporter.</param>
        /// <returns>Handle of the loaded scene.</returns>
        public async UniTask<AsyncOperationHandle<SceneInstance>> LoadScene(
            AssetReference sceneReference,
            LoadSceneMode loadSceneMode,
            bool activateOnLoad,
            bool reportProgress)
        {
            if (sceneReference == null)
            {
                throw new ArgumentNullException(nameof(sceneReference));
            }

            AsyncOperationHandle<SceneInstance> targetSceneHandle = sceneReference.LoadSceneAsync(loadSceneMode, activateOnLoad);

            try
            {
                if (reportProgress)
                {
                    _sceneLoadingProgressReporter.Report(0f);
                    await WaitWithProgress(targetSceneHandle);
                }
                else
                {
                    await targetSceneHandle.Task;
                }

                EnsureLoadSucceeded(targetSceneHandle, sceneReference);
                if (reportProgress)
                {
                    _sceneLoadingProgressReporter.Report(1f);
                }

                return targetSceneHandle;
            }
            catch
            {
                ReleaseHandleIfValid(targetSceneHandle);
                throw;
            }
        }

        /// <summary>
        /// Activates the scene and sets it as the active scene in <see cref="SceneManager"/>.
        /// </summary>
        /// <param name="scene">Scene instance to activate.</param>
        public async UniTask Activate(SceneInstance scene)
        {
            if (!scene.Scene.isLoaded)
            {
                await scene.ActivateAsync();
            }

            if (!scene.Scene.IsValid())
            {
                throw new InvalidOperationException("Target scene is invalid and cannot be activated.");
            }

            SceneManager.SetActiveScene(scene.Scene);
        }

        /// <summary>
        /// Unloads the scene and releases its resources when the handle is valid and the scene is loaded.
        /// </summary>
        /// <param name="sceneHandle">Scene handle to unload and release.</param>
        public async UniTask UnloadAndReleaseScene(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            if (!sceneHandle.IsValid())
            {
                return;
            }

            if (sceneHandle.Status != AsyncOperationStatus.Succeeded || !sceneHandle.Result.Scene.IsValid())
            {
                ReleaseHandleIfValid(sceneHandle);
                return;
            }

            if (!sceneHandle.Result.Scene.isLoaded)
            {
                ReleaseHandleIfValid(sceneHandle);
                return;
            }

            AsyncOperationHandle<SceneInstance> unloadSceneHandle = Addressables.UnloadSceneAsync(sceneHandle, autoReleaseHandle: true);
            await unloadSceneHandle.Task;
        }

        private void ReleaseHandleIfValid(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            if (sceneHandle.IsValid())
            {
                Addressables.Release(sceneHandle);
            }
        }

        private async UniTask WaitWithProgress(AsyncOperationHandle<SceneInstance> handle)
        {
            while (!handle.IsDone)
            {
                _sceneLoadingProgressReporter.Report(Mathf.Min(handle.PercentComplete, 0.95f));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
        private static void EnsureLoadSucceeded(AsyncOperationHandle<SceneInstance> handle, AssetReference sceneReference)
        {
            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                string sceneDescription = sceneReference?.RuntimeKey?.ToString() ?? "<unknown>";
                throw new InvalidOperationException($"Failed to load scene: {sceneDescription}");
            }
        }
    }
}
