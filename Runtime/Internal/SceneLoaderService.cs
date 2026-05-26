using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using SceneLoader.Internal.Execution;
using SceneLoader.Internal.Loading;
using SceneLoader.Internal.States;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal
{
    /// <summary>
    /// Implements <see cref="ISceneLoaderService"/> and performs sequential scene loading.
    /// </summary>
    internal sealed class SceneLoaderService : ISceneLoaderService
    {
        private readonly SceneLoadRegistry _sceneRegistry = new();
        private readonly SceneLoadExecutor _sceneLoadExecutor;
        private readonly ISceneActivationState _sceneActivationState;
        private readonly CancellationTokenSource _disposeCancellationTokenSource = new();
        private readonly CancellationToken _disposeToken;
        private int _isDisposed;

        internal SceneLoaderService(IAddressableSceneLoader sceneLoadService, ISceneActivationState sceneActivationState)
        {
            _disposeToken = _disposeCancellationTokenSource.Token;
            _sceneActivationState = sceneActivationState ?? throw new ArgumentNullException(nameof(sceneActivationState));
            _sceneLoadExecutor = new SceneLoadExecutor(
                sceneLoadService ?? throw new ArgumentNullException(nameof(sceneLoadService)),
                _sceneRegistry,
                _disposeToken);
        }

        /// <inheritdoc />
        public ESceneLoadState GetState(AssetReference sceneReference)
        {
            if (!TryGetSceneGuid(sceneReference, out string sceneGuid))
            {
                return ESceneLoadState.None;
            }

            return _sceneRegistry.GetState(sceneGuid);
        }

        /// <inheritdoc />
        public async UniTask<LoadResult> LoadScene(
            AssetReference sceneReference,
            LoadSceneMode loadSceneMode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            bool reportProgress = false)
        {
            if (!TryGetSceneGuid(sceneReference, out string sceneGuid))
            {
                return LoadResult.CreateError(SceneLoaderMessages.INVALID_SCENE_REFERENCE);
            }

            if (!TryAcquireLoadRequest(
                    sceneReference,
                    sceneGuid,
                    loadSceneMode,
                    activateOnLoad,
                    reportProgress,
                    out LoadRequestState loadRequest,
                    out bool isNewRequest,
                    out List<LoadRequestState> canceledAdditiveRequests,
                    out string rejectionReason))
            {
                return LoadResult.CreateError(rejectionReason);
            }

            if (isNewRequest)
            {
                CancelLoadRequests(canceledAdditiveRequests, SceneLoaderMessages.ADDITIVE_LOAD_CANCELED_BY_SINGLE_TRANSITION);
                _sceneLoadExecutor.RunLoadRequest(loadRequest).Forget();
            }

            try
            {
                if (_disposeToken.IsCancellationRequested)
                {
                    return LoadResult.CreateError(SceneLoaderMessages.SERVICE_DISPOSED);
                }

                return await loadRequest.CompletionSource.Task;
            }
            catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.LOAD_CANCELED_BY_DISPOSE);
            }
            catch (Exception exception)
            {
                return LoadResult.CreateError(exception.Message);
            }
        }

        /// <inheritdoc />
        public async UniTask<LoadResult> ActivateScene(AssetReference sceneReference)
        {
            if (!TryGetSceneGuid(sceneReference, out string sceneGuid))
            {
                return LoadResult.CreateError(SceneLoaderMessages.INVALID_SCENE_REFERENCE);
            }

            if (_disposeToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.SERVICE_DISPOSED);
            }

            if (_sceneRegistry.HasInFlightSingleLoad())
            {
                return LoadResult.CreateError(SceneLoaderMessages.SINGLE_TRANSITION_IN_PROGRESS);
            }

            if (!_sceneRegistry.TryGetLoadedScene(sceneGuid, out LoadedSceneState loadedScene))
            {
                return LoadResult.CreateError(SceneLoaderMessages.NotLoaded(sceneGuid));
            }

            if (IsActiveScene(loadedScene))
            {
                return LoadResult.CreateError(SceneLoaderMessages.AlreadyActive(sceneGuid));
            }

            try
            {
                _disposeToken.ThrowIfCancellationRequested();
                await _sceneLoadExecutor.ActivateLoadedScene(loadedScene, _disposeToken);
                return LoadResult.CreateSuccess();
            }
            catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.ACTIVATION_CANCELED_BY_DISPOSE);
            }
            catch (Exception exception)
            {
                return LoadResult.CreateError(exception.Message);
            }
        }

        /// <inheritdoc />
        public async UniTask<LoadResult> UnloadScene(AssetReference sceneReference)
        {
            if (!TryGetSceneGuid(sceneReference, out string sceneGuid))
            {
                return LoadResult.CreateError(SceneLoaderMessages.INVALID_SCENE_REFERENCE);
            }

            if (_disposeToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.SERVICE_DISPOSED);
            }

            if (!_sceneRegistry.TryGetLoadedScene(sceneGuid, out LoadedSceneState loadedScene))
            {
                return LoadResult.CreateError(SceneLoaderMessages.NotLoaded(sceneGuid));
            }

            if (loadedScene.LoadSceneMode == LoadSceneMode.Single &&
                _sceneRegistry.IsOnlyTrackedLoadedScene(sceneGuid))
            {
                return LoadResult.CreateError(SceneLoaderMessages.LAST_LOADED_SINGLE_SCENE_UNLOAD_NOT_SUPPORTED);
            }

            try
            {
                _disposeToken.ThrowIfCancellationRequested();
                return await _sceneLoadExecutor.UnloadLoadedScene(loadedScene, _disposeToken);
            }
            catch (OperationCanceledException) when (_disposeToken.IsCancellationRequested)
            {
                return LoadResult.CreateError(SceneLoaderMessages.UNLOAD_CANCELED_BY_DISPOSE);
            }
            catch (Exception exception)
            {
                return LoadResult.CreateError(exception.Message);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            {
                return;
            }

            List<LoadedSceneState> loadedScenesToCleanup = _sceneRegistry.SnapshotLoadedScenesAndClear();

            _disposeCancellationTokenSource.Cancel();

            _sceneLoadExecutor.CleanupLoadedScenesOnDispose(loadedScenesToCleanup).Forget();
            _disposeCancellationTokenSource.Dispose();
        }

        private bool TryAcquireLoadRequest(
            AssetReference sceneReference,
            string sceneGuid,
            LoadSceneMode loadSceneMode,
            bool activateOnLoad,
            bool reportProgress,
            out LoadRequestState loadRequest,
            out bool isNewRequest,
            out List<LoadRequestState> canceledAdditiveRequests,
            out string rejectionReason)
        {
            canceledAdditiveRequests = null;
            rejectionReason = null;

            if (_disposeToken.IsCancellationRequested)
            {
                loadRequest = null;
                isNewRequest = false;
                rejectionReason = SceneLoaderMessages.SERVICE_DISPOSED;
                return false;
            }

            if (loadSceneMode == LoadSceneMode.Single)
            {
                if (_sceneRegistry.TryGetInFlightSingleLoad(out LoadRequestState inFlightSingleLoad))
                {
                    if (SceneLoadPolicy.CanJoinSingleRequest(
                            inFlightSingleLoad,
                            sceneGuid,
                            activateOnLoad,
                            reportProgress,
                            out rejectionReason))
                    {
                        loadRequest = inFlightSingleLoad;
                        isNewRequest = false;
                        return true;
                    }

                    loadRequest = null;
                    isNewRequest = false;
                    return false;
                }

                if (_sceneRegistry.TryGetLoadedScene(sceneGuid, out LoadedSceneState loadedSingleScene))
                {
                    rejectionReason = SceneLoadPolicy.GetAlreadyLoadedRejectionReason(loadedSingleScene, loadSceneMode);
                    loadRequest = null;
                    isNewRequest = false;
                    return false;
                }

                canceledAdditiveRequests = _sceneRegistry.SnapshotAndClearInFlightAdditiveLoads();

                loadRequest = new LoadRequestState(sceneReference, sceneGuid, loadSceneMode, activateOnLoad, reportProgress);
                _sceneRegistry.SetInFlightSingleLoad(loadRequest);
                isNewRequest = true;
                return true;
            }

            if (_sceneRegistry.HasInFlightSingleLoad())
            {
                loadRequest = null;
                isNewRequest = false;
                rejectionReason = SceneLoaderMessages.SINGLE_TRANSITION_IN_PROGRESS;
                return false;
            }

            if (_sceneRegistry.TryGetLoadedScene(sceneGuid, out LoadedSceneState loadedAdditiveScene))
            {
                rejectionReason = SceneLoadPolicy.GetAlreadyLoadedRejectionReason(loadedAdditiveScene, loadSceneMode);
                loadRequest = null;
                isNewRequest = false;
                return false;
            }

            if (_sceneRegistry.TryGetInFlightAdditiveLoad(sceneGuid, out LoadRequestState inFlightAdditiveLoad))
            {
                if (SceneLoadPolicy.CanJoinAdditiveRequest(
                        inFlightAdditiveLoad,
                        activateOnLoad,
                        reportProgress,
                        out rejectionReason))
                {
                    loadRequest = inFlightAdditiveLoad;
                    isNewRequest = false;
                    return true;
                }

                loadRequest = null;
                isNewRequest = false;
                return false;
            }

            loadRequest = new LoadRequestState(sceneReference, sceneGuid, loadSceneMode, activateOnLoad, reportProgress);
            _sceneRegistry.AddInFlightAdditiveLoad(loadRequest);
            isNewRequest = true;
            return true;
        }

        private void CancelLoadRequests(List<LoadRequestState> loadRequests, string reason)
        {
            if (loadRequests == null)
            {
                return;
            }

            foreach (LoadRequestState request in loadRequests)
            {
                request.Cancel(reason);
            }
        }

        private bool IsActiveScene(LoadedSceneState loadedScene)
        {
            return _sceneActivationState.IsActive(loadedScene.Handle.Result);
        }

        private static bool TryGetSceneGuid(AssetReference sceneReference, out string sceneGuid)
        {
            sceneGuid = sceneReference?.AssetGUID;
            return !string.IsNullOrWhiteSpace(sceneGuid);
        }
    }

}
