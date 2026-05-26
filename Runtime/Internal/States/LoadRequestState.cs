using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.States
{
    /// <summary>
    /// Represents a single in-flight scene load request and its completion state.
    /// </summary>
    internal sealed class LoadRequestState
    {
        /// <summary>
        /// Gets the target scene reference.
        /// </summary>
        internal AssetReference SceneReference { get; }

        /// <summary>
        /// Gets the stable scene GUID derived from the scene reference.
        /// </summary>
        internal string SceneGuid { get; }

        /// <summary>
        /// Gets the requested load mode.
        /// </summary>
        internal LoadSceneMode LoadSceneMode { get; }

        /// <summary>
        /// Gets whether the scene should be activated immediately after loading.
        /// </summary>
        internal bool ActivateOnLoad { get; }

        /// <summary>
        /// Gets whether this request should publish progress into the shared progress channel.
        /// </summary>
        internal bool ReportProgress { get; }

        /// <summary>
        /// Completes when the request finishes, fails, or is superseded.
        /// </summary>
        internal UniTaskCompletionSource<LoadResult> CompletionSource { get; } = new();

        /// <summary>
        /// Gets the cancellation token that aborts request orchestration.
        /// </summary>
        internal CancellationToken CancellationToken => _cancellationTokenSource.Token;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// Creates a tracked in-flight load request snapshot.
        /// </summary>
        /// <param name="sceneReference">Target scene reference.</param>
        /// <param name="sceneGuid">Stable GUID derived from the scene reference.</param>
        /// <param name="loadSceneMode">Requested scene load mode.</param>
        /// <param name="activateOnLoad">Whether the scene should activate immediately after loading.</param>
        /// <param name="reportProgress">Whether the request should publish progress updates.</param>
        internal LoadRequestState(
            AssetReference sceneReference,
            string sceneGuid,
            LoadSceneMode loadSceneMode,
            bool activateOnLoad,
            bool reportProgress)
        {
            SceneReference = sceneReference;
            SceneGuid = sceneGuid;
            LoadSceneMode = loadSceneMode;
            ActivateOnLoad = activateOnLoad;
            ReportProgress = reportProgress;
        }

        /// <summary>
        /// Tries to complete the request with the supplied result.
        /// </summary>
        /// <param name="result">Final result visible to callers.</param>
        internal void TrySetResult(LoadResult result)
        {
            CompletionSource.TrySetResult(result);
        }

        /// <summary>
        /// Marks the request as canceled and completes waiters with the supplied reason.
        /// </summary>
        /// <param name="error">Cancellation reason visible to callers.</param>
        internal void Cancel(string error)
        {
            CompletionSource.TrySetResult(LoadResult.CreateError(error));

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}
