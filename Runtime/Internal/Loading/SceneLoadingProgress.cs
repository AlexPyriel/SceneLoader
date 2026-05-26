using System;

namespace SceneLoader.Internal.Loading
{
    /// <summary>
    /// Default scene loading progress implementation for the package.
    /// </summary>
    internal sealed class SceneLoadingProgress : ISceneLoadingProgress, ISceneLoadingProgressReporter
    {
        /// <inheritdoc />
        public event Action<float> ProgressChanged;

        /// <inheritdoc />
        public float Current { get; private set; }

        /// <inheritdoc />
        public void Report(float progress)
        {
            Current = progress;
            ProgressChanged?.Invoke(progress);
        }
    }
}
