namespace SceneLoader.Internal
{
    /// <summary>
    /// Centralizes runtime-facing scene loader error and warning messages.
    /// </summary>
    internal static class SceneLoaderMessages
    {
        /// <summary>
        /// Describes an invalid or empty scene reference input.
        /// </summary>
        internal const string INVALID_SCENE_REFERENCE = "Scene reference is null or has an empty asset GUID.";

        /// <summary>
        /// Describes attempts to use the service after disposal.
        /// </summary>
        internal const string SERVICE_DISPOSED = "Scene loader service is disposed.";

        /// <summary>
        /// Describes an active exclusive single-scene transition.
        /// </summary>
        internal const string SINGLE_TRANSITION_IN_PROGRESS = "A single-scene transition is in progress.";

        /// <summary>
        /// Describes a conflicting single-scene transition targeting a different scene.
        /// </summary>
        internal const string DIFFERENT_SINGLE_TRANSITION_IN_PROGRESS = "A different single-scene transition is already in progress.";

        /// <summary>
        /// Describes a load canceled because the service was disposed.
        /// </summary>
        internal const string LOAD_CANCELED_BY_DISPOSE = "Scene load was canceled because the service was disposed.";

        /// <summary>
        /// Describes a load canceled because a newer single-scene transition superseded it.
        /// </summary>
        internal const string LOAD_CANCELED_BY_SINGLE_TRANSITION = "Scene load was canceled because a single-scene transition started.";

        /// <summary>
        /// Describes an additive load canceled by a newer single-scene transition.
        /// </summary>
        internal const string ADDITIVE_LOAD_CANCELED_BY_SINGLE_TRANSITION = "Additive scene load was canceled because a single-scene transition started.";

        /// <summary>
        /// Describes an activation canceled because the service was disposed.
        /// </summary>
        internal const string ACTIVATION_CANCELED_BY_DISPOSE = "Scene activation was canceled because the service was disposed.";

        /// <summary>
        /// Describes an unload canceled because the service was disposed.
        /// </summary>
        internal const string UNLOAD_CANCELED_BY_DISPOSE = "Scene unload was canceled because the service was disposed.";

        /// <summary>
        /// Describes an unload canceled by an external cancellation request.
        /// </summary>
        internal const string UNLOAD_CANCELED = "Scene unload was canceled.";

        /// <summary>
        /// Describes an unsupported attempt to unload the last tracked single scene.
        /// </summary>
        internal const string LAST_LOADED_SINGLE_SCENE_UNLOAD_NOT_SUPPORTED = "Unloading the only remaining tracked loaded scene is not supported.";

        /// <summary>
        /// Builds a message for a conflicting in-flight activation policy on the same scene.
        /// </summary>
        internal static string AlreadyLoadingWithDifferentActivationPolicy(string sceneGuid) =>
            $"Scene '{sceneGuid}' is already loading with a different activation policy.";

        /// <summary>
        /// Builds a message for a conflicting in-flight progress reporting policy on the same scene.
        /// </summary>
        internal static string AlreadyLoadingWithDifferentProgressPolicy(string sceneGuid) =>
            $"Scene '{sceneGuid}' is already loading with a different progress reporting policy.";

        /// <summary>
        /// Builds a message for a duplicate request against the current active single scene.
        /// </summary>
        internal static string AlreadyLoadedAsActiveSingleScene(string sceneGuid) =>
            $"Scene '{sceneGuid}' is already loaded as the active single scene.";

        /// <summary>
        /// Builds a message for a duplicate additive load request.
        /// </summary>
        internal static string AlreadyLoadedAdditively(string sceneGuid) =>
            $"Scene '{sceneGuid}' is already loaded additively.";

        /// <summary>
        /// Builds a message for a duplicate request against any already loaded scene.
        /// </summary>
        internal static string AlreadyLoaded(string sceneGuid) =>
            $"Scene '{sceneGuid}' is already loaded.";

        /// <summary>
        /// Builds a message for operations targeting a scene that is not currently tracked as loaded.
        /// </summary>
        internal static string NotLoaded(string sceneGuid) =>
            $"Scene '{sceneGuid}' is not loaded.";

        /// <summary>
        /// Builds a message for attempts to activate the already active scene.
        /// </summary>
        internal static string AlreadyActive(string sceneGuid) =>
            $"Scene '{sceneGuid}' is already active.";

        /// <summary>
        /// Builds a warning for best-effort cleanup failures after a successful single transition.
        /// </summary>
        internal static string CleanupFailedDuringSingleTransition(string sceneGuid, string reason) =>
            $"[SceneLoadExecutor] Failed to cleanup scene '{sceneGuid}' during single transition: {reason}";
    }
}
