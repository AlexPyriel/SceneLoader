namespace SceneLoader
{
    /// <summary>
    /// Represents the result of an Addressables-backed scene operation with an optional failure description.
    /// </summary>
    public readonly struct LoadResult
    {
        /// <summary>
        /// Gets whether the scene operation completed successfully.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the failure description when <see cref="Success"/> is <see langword="false"/>.
        /// </summary>
        public string Error { get; }

        private LoadResult(bool success, string error = null)
        {
            Success = success;
            Error = error;
        }

        internal static LoadResult CreateSuccess() => new(true);

        internal static LoadResult CreateError(string error) => new(false, error);
    }
}
