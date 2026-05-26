using UnityEngine.ResourceManagement.ResourceProviders;

namespace SceneLoader.Internal
{
    /// <summary>
    /// Evaluates whether a loaded scene instance is currently the active Unity scene.
    /// </summary>
    internal interface ISceneActivationState
    {
        /// <summary>
        /// Returns whether the supplied scene instance is currently active.
        /// </summary>
        /// <param name="sceneInstance">Loaded scene instance to inspect.</param>
        bool IsActive(SceneInstance sceneInstance);
    }
}
