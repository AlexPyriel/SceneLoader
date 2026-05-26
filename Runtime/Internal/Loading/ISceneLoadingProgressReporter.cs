using System;

namespace SceneLoader.Internal.Loading
{
    /// <summary>
    /// Reports normalized scene loading progress in the range from 0 to 1.
    /// </summary>
    internal interface ISceneLoadingProgressReporter : IProgress<float> { }
}
