using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SceneLoader.Internal.Loading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Tests.EditMode
{
    /// <summary>
    /// Captures low-level loader interactions for edit mode tests.
    /// </summary>
    internal sealed class FakeAddressableSceneLoader : IAddressableSceneLoader
    {
        /// <summary>
        /// Gets the recorded low-level load calls in execution order.
        /// </summary>
        internal List<LoadCall> LoadCalls { get; } = new();

        /// <summary>
        /// Gets the GUIDs of scenes that were unloaded by the fake loader.
        /// </summary>
        internal List<string> UnloadedSceneGuids { get; } = new();

        /// <summary>
        /// Gets the number of activation attempts observed by the fake loader.
        /// </summary>
        internal int ActivateCallCount { get; private set; }

        private readonly Queue<Exception> _pendingUnloadFailures = new();
        private readonly ResourceManager _resourceManager = new();
        private readonly Dictionary<AsyncOperationHandle<SceneInstance>, string> _handleToSceneGuids = new();

        /// <inheritdoc />
        public UniTask Activate(SceneInstance scene)
        {
            ActivateCallCount++;
            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public UniTask<AsyncOperationHandle<SceneInstance>> LoadScene(
            AssetReference sceneReference,
            LoadSceneMode loadSceneMode,
            bool activateOnLoad,
            bool reportProgress)
        {
            LoadCall call = new(sceneReference, loadSceneMode, activateOnLoad, reportProgress);
            LoadCalls.Add(call);
            return call.CompletionSource.Task;
        }

        /// <inheritdoc />
        public UniTask UnloadAndReleaseScene(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            if (_pendingUnloadFailures.Count > 0)
            {
                Exception exception = _pendingUnloadFailures.Dequeue();
                return UniTask.FromException(exception);
            }

            if (_handleToSceneGuids.TryGetValue(sceneHandle, out string sceneGuid))
            {
                UnloadedSceneGuids.Add(sceneGuid);
                _handleToSceneGuids.Remove(sceneHandle);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Completes a pending load request with a default scene instance.
        /// </summary>
        internal void CompleteLoad(int index)
        {
            LoadCall call = LoadCalls[index];
            AsyncOperationHandle<SceneInstance> handle = CreateHandle();
            _handleToSceneGuids[handle] = call.SceneReference.AssetGUID;
            call.CompletionSource.TrySetResult(handle);
        }

        /// <summary>
        /// Queues an unload failure to be thrown by the next unload attempt.
        /// </summary>
        internal void FailNextUnload(Exception exception)
        {
            _pendingUnloadFailures.Enqueue(exception);
        }

        private AsyncOperationHandle<SceneInstance> CreateHandle()
        {
            return _resourceManager.CreateCompletedOperation(default(SceneInstance), null);
        }

        /// <summary>
        /// Represents a single recorded low-level load invocation.
        /// </summary>
        internal sealed class LoadCall
        {
            /// <summary>
            /// Gets the scene reference passed to the load call.
            /// </summary>
            internal AssetReference SceneReference { get; }

            /// <summary>
            /// Gets the requested scene load mode.
            /// </summary>
            internal LoadSceneMode LoadSceneMode { get; }

            /// <summary>
            /// Gets whether the load requested immediate activation.
            /// </summary>
            internal bool ActivateOnLoad { get; }

            /// <summary>
            /// Gets whether the load requested shared progress reporting.
            /// </summary>
            internal bool ReportProgress { get; }

            /// <summary>
            /// Gets the completion source used to finish the synthetic load.
            /// </summary>
            internal UniTaskCompletionSource<AsyncOperationHandle<SceneInstance>> CompletionSource { get; } = new();

            /// <summary>
            /// Creates a recorded low-level load call snapshot.
            /// </summary>
            internal LoadCall(
                AssetReference sceneReference,
                LoadSceneMode loadSceneMode,
                bool activateOnLoad,
                bool reportProgress)
            {
                SceneReference = sceneReference;
                LoadSceneMode = loadSceneMode;
                ActivateOnLoad = activateOnLoad;
                ReportProgress = reportProgress;
            }
        }
    }
}
