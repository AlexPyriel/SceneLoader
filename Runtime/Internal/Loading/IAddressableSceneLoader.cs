using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace SceneLoader.Internal.Loading
{
    /// <summary>
    /// Low-level Addressables scene operations behind <see cref="ISceneLoaderService"/>; replace it to load scenes from another source.
    /// </summary>
    public interface IAddressableSceneLoader
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
