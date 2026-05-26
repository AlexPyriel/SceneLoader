using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SceneLoader.Internal.Loading;
using SceneLoader.Internal.States;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.Execution
{
    /// <summary>
    /// Executes scene load, activation, and cleanup operations.
    /// </summary>
    internal sealed class SceneLoadExecutor
    {
        private readonly IAddressableSceneLoader _sceneLoadService;
        private readonly SceneLoadRegistry _sceneRegistry;
        private readonly CancellationToken _disposeToken;

        internal SceneLoadExecutor(
            IAddressableSceneLoader sceneLoadService,
            SceneLoadRegistry sceneRegistry,
            CancellationToken disposeToken)
        {
            _sceneLoadService = sceneLoadService ?? throw new ArgumentNullException(nameof(sceneLoadService));
            _sceneRegistry = sceneRegistry ?? throw new ArgumentNullException(nameof(sceneRegistry));
            _disposeToken = disposeToken;
        }

        /// <summary>
        /// Executes the full lifecycle of a tracked load request and completes its result source.
        /// </summary>
        internal async UniTaskVoid RunLoadRequest(LoadRequestState loadRequest)
        {
            LoadResult result;

            try
            {
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeToken, loadRequest.CancellationToken);

                result = await ExecuteLoadRequest(loadRequest, linkedCts.Token);
            }
            catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
            {
                result = LoadResult.CreateError(SceneLoaderMessages.LOAD_CANCELED_BY_DISPOSE);
            }
            catch (OperationCanceledException) when (loadRequest.CancellationToken.IsCancellationRequested)
            {
                result = LoadResult.CreateError(SceneLoaderMessages.LOAD_CANCELED_BY_SINGLE_TRANSITION);
            }
            catch (Exception exception)
            {
                result = LoadResult.CreateError(exception.Message);
            }

            CompleteLoadRequest(loadRequest);
            loadRequest.TrySetResult(result);
        }

        /// <summary>
        /// Performs best-effort cleanup of loaded scenes during service disposal.
        /// </summary>
        internal async UniTaskVoid CleanupLoadedScenesOnDispose(List<LoadedSceneState> loadedScenes)
        {
            await CleanupLoadedScenes(loadedScenes, removeFromRegistryOnSuccess: false);
        }

        /// <summary>
        /// Activates a previously loaded scene without mutating registry tracking.
        /// </summary>
        internal async UniTask ActivateLoadedScene(LoadedSceneState loadedScene, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _sceneLoadService.Activate(loadedScene.Handle.Result);
        }

        /// <summary>
        /// Unloads a tracked scene and removes it from the registry when unloading succeeds.
        /// </summary>
        internal async UniTask<LoadResult> UnloadLoadedScene(LoadedSceneState loadedScene, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _sceneLoadService.UnloadAndReleaseScene(loadedScene.Handle);
                _sceneRegistry.TryRemoveLoadedSceneIfTracked(loadedScene.SceneGuid, loadedScene.Handle);
                return LoadResult.CreateSuccess();
            }
            catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.UNLOAD_CANCELED_BY_DISPOSE);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.UNLOAD_CANCELED);
            }
            catch (Exception exception)
            {
                return LoadResult.CreateError(exception.Message);
            }
        }

        /// <summary>
        /// Performs the low-level load and post-load cleanup flow for a single tracked request.
        /// </summary>
        private async UniTask<LoadResult> ExecuteLoadRequest(LoadRequestState loadRequest, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AsyncOperationHandle<SceneInstance> sceneHandle = await _sceneLoadService.LoadScene(
                loadRequest.SceneReference,
                loadRequest.LoadSceneMode,
                loadRequest.ActivateOnLoad,
                reportProgress: loadRequest.ReportProgress);

            if (cancellationToken.IsCancellationRequested)
            {
                await _sceneLoadService.UnloadAndReleaseScene(sceneHandle);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (loadRequest.CancellationToken.IsCancellationRequested)
            {
                await _sceneLoadService.UnloadAndReleaseScene(sceneHandle);
                return LoadResult.CreateError(SceneLoaderMessages.LOAD_CANCELED_BY_SINGLE_TRANSITION);
            }

            if (loadRequest.LoadSceneMode == LoadSceneMode.Single)
            {
                List<LoadedSceneState> replacedLoadedScenes = _sceneRegistry.SnapshotLoadedScenes();
                _sceneRegistry.SetLoadedScene(new LoadedSceneState(loadRequest.SceneGuid, LoadSceneMode.Single, sceneHandle));
                await CleanupLoadedScenes(replacedLoadedScenes, removeFromRegistryOnSuccess: true);
            }
            else
            {
                _sceneRegistry.SetLoadedScene(new LoadedSceneState(loadRequest.SceneGuid, loadRequest.LoadSceneMode, sceneHandle));
            }

            return LoadResult.CreateSuccess();
        }

        /// <summary>
        /// Unloads a snapshot of tracked scenes and optionally removes successfully cleaned scenes from the registry.
        /// </summary>
        private async UniTask CleanupLoadedScenes(List<LoadedSceneState> loadedScenes, bool removeFromRegistryOnSuccess)
        {
            if (loadedScenes == null || loadedScenes.Count == 0)
            {
                return;
            }

            foreach (LoadedSceneState loadedScene in loadedScenes)
            {
                try
                {
                    await _sceneLoadService.UnloadAndReleaseScene(loadedScene.Handle);
                    if (removeFromRegistryOnSuccess)
                    {
                        _sceneRegistry.TryRemoveLoadedSceneIfTracked(loadedScene.SceneGuid, loadedScene.Handle);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(SceneLoaderMessages.CleanupFailedDuringSingleTransition(loadedScene.SceneGuid, exception.Message));
                }
            }
        }

        /// <summary>
        /// Clears the matching in-flight request slot after request completion.
        /// </summary>
        private void CompleteLoadRequest(LoadRequestState loadRequest)
        {
            if (loadRequest.LoadSceneMode == LoadSceneMode.Single)
            {
                _sceneRegistry.ClearInFlightSingleLoadIfMatches(loadRequest);
                return;
            }

            _sceneRegistry.ClearInFlightAdditiveLoadIfMatches(loadRequest);
        }
    }
}
