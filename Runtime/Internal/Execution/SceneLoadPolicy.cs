using SceneLoader.Internal.States;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.Execution
{
    /// <summary>
    /// Encapsulates scene load acceptance rules and duplicate-request policy.
    /// </summary>
    internal static class SceneLoadPolicy
    {
        /// <summary>
        /// Determines whether a new single-scene request can join an existing in-flight single request.
        /// </summary>
        internal static bool CanJoinSingleRequest(
            LoadRequestState inFlightSingleLoad,
            string sceneGuid,
            bool activateOnLoad,
            bool reportProgress,
            out string rejectionReason)
        {
            if (inFlightSingleLoad.SceneGuid != sceneGuid)
            {
                rejectionReason = SceneLoaderMessages.DIFFERENT_SINGLE_TRANSITION_IN_PROGRESS;
                return false;
            }

            if (inFlightSingleLoad.ActivateOnLoad != activateOnLoad)
            {
                rejectionReason = SceneLoaderMessages.AlreadyLoadingWithDifferentActivationPolicy(sceneGuid);
                return false;
            }

            if (inFlightSingleLoad.ReportProgress != reportProgress)
            {
                rejectionReason = SceneLoaderMessages.AlreadyLoadingWithDifferentProgressPolicy(sceneGuid);
                return false;
            }

            rejectionReason = null;
            return true;
        }

        /// <summary>
        /// Determines whether a new additive request can join an existing in-flight additive request.
        /// </summary>
        internal static bool CanJoinAdditiveRequest(
            LoadRequestState inFlightAdditiveLoad,
            bool activateOnLoad,
            bool reportProgress,
            out string rejectionReason)
        {
            if (inFlightAdditiveLoad.ActivateOnLoad != activateOnLoad)
            {
                rejectionReason = SceneLoaderMessages.AlreadyLoadingWithDifferentActivationPolicy(inFlightAdditiveLoad.SceneGuid);
                return false;
            }

            if (inFlightAdditiveLoad.ReportProgress != reportProgress)
            {
                rejectionReason = SceneLoaderMessages.AlreadyLoadingWithDifferentProgressPolicy(inFlightAdditiveLoad.SceneGuid);
                return false;
            }

            rejectionReason = null;
            return true;
        }

        /// <summary>
        /// Builds the rejection reason for duplicate requests targeting an already loaded scene.
        /// </summary>
        internal static string GetAlreadyLoadedRejectionReason(
            LoadedSceneState loadedScene,
            LoadSceneMode requestedMode)
        {
            if (loadedScene.LoadSceneMode == requestedMode)
            {
                return requestedMode == LoadSceneMode.Single
                    ? SceneLoaderMessages.AlreadyLoadedAsActiveSingleScene(loadedScene.SceneGuid)
                    : SceneLoaderMessages.AlreadyLoadedAdditively(loadedScene.SceneGuid);
            }

            return SceneLoaderMessages.AlreadyLoaded(loadedScene.SceneGuid);
        }
    }
}
