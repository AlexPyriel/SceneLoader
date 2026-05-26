using SceneLoader.Internal;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace SceneLoader.Tests.EditMode
{
    /// <summary>
    /// Controls active-scene evaluation in edit mode tests.
    /// </summary>
    internal sealed class FakeSceneActivationState : ISceneActivationState
    {
        /// <summary>
        /// Gets or sets the value returned for active-scene checks.
        /// </summary>
        internal bool IsActiveResult { get; set; }

        /// <inheritdoc />
        public bool IsActive(SceneInstance sceneInstance)
        {
            return IsActiveResult;
        }
    }
}
