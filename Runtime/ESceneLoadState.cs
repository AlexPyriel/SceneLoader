namespace SceneLoader
{
    /// <summary>
    /// Describes the externally visible load state of an Addressables scene reference.
    /// </summary>
    public enum ESceneLoadState
    {
        /// <summary>
        /// Indicates that the scene is neither loaded nor currently loading.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the scene load request is currently in progress.
        /// </summary>
        Loading = 1,

        /// <summary>
        /// Indicates that the scene has already been loaded.
        /// </summary>
        Loaded = 2
    }
}
