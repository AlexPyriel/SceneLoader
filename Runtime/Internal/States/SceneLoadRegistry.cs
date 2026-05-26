using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace SceneLoader.Internal.States
{
    /// <summary>
    /// Owns loaded-scene tracking and in-flight request bookkeeping.
    /// </summary>
    internal sealed class SceneLoadRegistry
    {
        private readonly object _stateLock = new();
        private readonly Dictionary<string, LoadedSceneState> _loadedScenes = new();
        private readonly Dictionary<string, LoadRequestState> _inFlightAdditiveLoads = new();
        private LoadRequestState _inFlightSingleLoad;

        /// <summary>
        /// Returns the externally visible load state for the specified scene GUID.
        /// </summary>
        internal ESceneLoadState GetState(string sceneGuid)
        {
            lock (_stateLock)
            {
                if (_loadedScenes.ContainsKey(sceneGuid))
                {
                    return ESceneLoadState.Loaded;
                }

                if ((_inFlightSingleLoad != null && _inFlightSingleLoad.SceneGuid == sceneGuid) ||
                    _inFlightAdditiveLoads.ContainsKey(sceneGuid))
                {
                    return ESceneLoadState.Loading;
                }

                return ESceneLoadState.None;
            }
        }

        /// <summary>
        /// Reports whether an exclusive single-scene load is currently in progress.
        /// </summary>
        internal bool HasInFlightSingleLoad()
        {
            lock (_stateLock)
            {
                return _inFlightSingleLoad != null;
            }
        }

        /// <summary>
        /// Tries to retrieve a tracked loaded scene by GUID.
        /// </summary>
        internal bool TryGetLoadedScene(string sceneGuid, out LoadedSceneState loadedScene)
        {
            lock (_stateLock)
            {
                return _loadedScenes.TryGetValue(sceneGuid, out loadedScene);
            }
        }

        /// <summary>
        /// Returns whether the specified scene is the only tracked loaded scene.
        /// </summary>
        internal bool IsOnlyTrackedLoadedScene(string sceneGuid)
        {
            lock (_stateLock)
            {
                return _loadedScenes.Count == 1 && _loadedScenes.ContainsKey(sceneGuid);
            }
        }

        /// <summary>
        /// Creates a snapshot of all currently tracked loaded scenes.
        /// </summary>
        internal List<LoadedSceneState> SnapshotLoadedScenes()
        {
            lock (_stateLock)
            {
                return new List<LoadedSceneState>(_loadedScenes.Values);
            }
        }

        /// <summary>
        /// Creates a snapshot of all tracked loaded scenes and clears the loaded registry.
        /// </summary>
        internal List<LoadedSceneState> SnapshotLoadedScenesAndClear()
        {
            lock (_stateLock)
            {
                List<LoadedSceneState> loadedScenes = new(_loadedScenes.Values);
                _loadedScenes.Clear();
                return loadedScenes;
            }
        }

        /// <summary>
        /// Stores or replaces the tracked state for a loaded scene.
        /// </summary>
        internal void SetLoadedScene(LoadedSceneState loadedScene)
        {
            lock (_stateLock)
            {
                _loadedScenes[loadedScene.SceneGuid] = loadedScene;
            }
        }

        /// <summary>
        /// Removes a tracked loaded scene only when the stored handle matches the expected one.
        /// </summary>
        internal bool TryRemoveLoadedSceneIfTracked(string sceneGuid, AsyncOperationHandle<SceneInstance> expectedHandle)
        {
            lock (_stateLock)
            {
                if (_loadedScenes.TryGetValue(sceneGuid, out LoadedSceneState loadedScene) &&
                    loadedScene.Handle.Equals(expectedHandle))
                {
                    _loadedScenes.Remove(sceneGuid);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Marks a request as the current exclusive single-scene load.
        /// </summary>
        internal void SetInFlightSingleLoad(LoadRequestState loadRequest)
        {
            lock (_stateLock)
            {
                _inFlightSingleLoad = loadRequest;
            }
        }

        /// <summary>
        /// Tries to retrieve the current exclusive single-scene load request.
        /// </summary>
        internal bool TryGetInFlightSingleLoad(out LoadRequestState loadRequest)
        {
            lock (_stateLock)
            {
                loadRequest = _inFlightSingleLoad;
                return loadRequest != null;
            }
        }

        /// <summary>
        /// Clears the tracked single-scene request when it matches the supplied request instance.
        /// </summary>
        internal void ClearInFlightSingleLoadIfMatches(LoadRequestState loadRequest)
        {
            lock (_stateLock)
            {
                if (ReferenceEquals(_inFlightSingleLoad, loadRequest))
                {
                    _inFlightSingleLoad = null;
                }
            }
        }

        /// <summary>
        /// Adds or replaces a tracked additive request by scene GUID.
        /// </summary>
        internal void AddInFlightAdditiveLoad(LoadRequestState loadRequest)
        {
            lock (_stateLock)
            {
                _inFlightAdditiveLoads[loadRequest.SceneGuid] = loadRequest;
            }
        }

        /// <summary>
        /// Tries to retrieve a tracked additive request by scene GUID.
        /// </summary>
        internal bool TryGetInFlightAdditiveLoad(string sceneGuid, out LoadRequestState loadRequest)
        {
            lock (_stateLock)
            {
                return _inFlightAdditiveLoads.TryGetValue(sceneGuid, out loadRequest);
            }
        }

        /// <summary>
        /// Creates a snapshot of all additive requests and clears the additive in-flight registry.
        /// </summary>
        internal List<LoadRequestState> SnapshotAndClearInFlightAdditiveLoads()
        {
            lock (_stateLock)
            {
                List<LoadRequestState> inFlightAdditiveLoads = new(_inFlightAdditiveLoads.Values);
                _inFlightAdditiveLoads.Clear();
                return inFlightAdditiveLoads;
            }
        }

        /// <summary>
        /// Clears a tracked additive request only when the stored instance matches the supplied request.
        /// </summary>
        internal void ClearInFlightAdditiveLoadIfMatches(LoadRequestState loadRequest)
        {
            lock (_stateLock)
            {
                if (_inFlightAdditiveLoads.TryGetValue(loadRequest.SceneGuid, out LoadRequestState currentRequest) &&
                    ReferenceEquals(currentRequest, loadRequest))
                {
                    _inFlightAdditiveLoads.Remove(loadRequest.SceneGuid);
                }
            }
        }
    }
}
