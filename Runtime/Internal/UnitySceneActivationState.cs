using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal
{
    /// <summary>
    /// Default Unity-backed implementation for active scene inspection.
    /// </summary>
    internal sealed class UnitySceneActivationState : ISceneActivationState
    {
        /// <inheritdoc />
        public bool IsActive(SceneInstance sceneInstance)
        {
            return sceneInstance.Scene == SceneManager.GetActiveScene();
        }
    }
}
