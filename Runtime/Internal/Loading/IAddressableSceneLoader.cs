using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.Loading
{
    /// <summary>
    /// Defines the internal low-level Addressables scene loading operations.
    /// </summary>
    internal interface IAddressableSceneLoader
    {
        /// <summary>
        /// Loads the target scene and returns its Addressables handle.
        /// </summary>
        public UniTask<AsyncOperationHandle<SceneInstance>> LoadScene(
            AssetReference sceneReference,
            LoadSceneMode loadSceneMode,
            bool activateOnLoad,
            bool reportProgress);

        /// <summary>
        /// Activates the loaded scene instance.
        /// </summary>
        public UniTask Activate(SceneInstance scene);

        /// <summary>
        /// Unloads the target scene and releases its Addressables handle.
        /// </summary>
        public UniTask UnloadAndReleaseScene(AsyncOperationHandle<SceneInstance> sceneHandle);
    }
}
