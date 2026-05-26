using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.States
{
    /// <summary>
    /// Stores the tracked handle and load mode for a loaded scene.
    /// </summary>
    internal sealed class LoadedSceneState
    {
        /// <summary>
        /// Gets the stable scene GUID derived from the scene reference.
        /// </summary>
        internal string SceneGuid { get; }

        /// <summary>
        /// Gets the mode that was used to load the scene.
        /// </summary>
        internal LoadSceneMode LoadSceneMode { get; }

        /// <summary>
        /// Gets the Addressables handle that owns the loaded scene.
        /// </summary>
        internal AsyncOperationHandle<SceneInstance> Handle { get; }

        /// <summary>
        /// Creates the tracked state snapshot for a loaded scene.
        /// </summary>
        /// <param name="sceneGuid">Stable GUID derived from the scene reference.</param>
        /// <param name="loadSceneMode">Mode that was used to load the scene.</param>
        /// <param name="handle">Addressables handle that owns the loaded scene.</param>
        internal LoadedSceneState(string sceneGuid, LoadSceneMode loadSceneMode, AsyncOperationHandle<SceneInstance> handle)
        {
            SceneGuid = sceneGuid;
            LoadSceneMode = loadSceneMode;
            Handle = handle;
        }
    }
}
