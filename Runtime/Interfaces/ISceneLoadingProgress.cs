using System;

namespace SceneLoader
{
    /// <summary>
    /// Exposes scene loading progress for UI and orchestration layers.
    /// </summary>
    public interface ISceneLoadingProgress
    {
        /// <summary>
        /// Raised when scene loading progress changes.
        /// </summary>
        public event Action<float> ProgressChanged;

        /// <summary>
        /// Gets the latest normalized scene loading progress in the range from 0 to 1.
        /// </summary>
        public float Current { get; }
    }
}
