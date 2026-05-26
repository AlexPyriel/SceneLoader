using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using SceneLoader.Internal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SceneLoader.Tests.EditMode
{
    /// <summary>
    /// Covers the public scene loader contract and the main orchestration policy branches.
    /// </summary>
    public sealed class SceneLoaderServiceTests
    {
        private const string SCENE_A_GUID = "scene-a-guid";
        private const string SCENE_B_GUID = "scene-b-guid";
        private const string LOADED_SCENE_A_GUID = "loaded-scene-a-guid";
        private const string ADDITIVE_SCENE_B_GUID = "additive-scene-b-guid";
        private const string ADDITIVE_SCENE_C_GUID = "additive-scene-c-guid";
        private const string SINGLE_SCENE_D_GUID = "single-scene-d-guid";

        /// <summary>
        /// Verifies that invalid scene references report the neutral externally visible state.
        /// </summary>
        [Test]
        public void GetState_ReturnsNone_ForInvalidReference()
        {
            using SceneLoaderServiceFixture fixture = new();

            ESceneLoadState state = fixture.Service.GetState(null);

            Assert.That(state, Is.EqualTo(ESceneLoadState.None));
        }

        /// <summary>
        /// Verifies that loading with an invalid scene reference returns the expected error result.
        /// </summary>
        [UnityTest]
        public IEnumerator LoadScene_ReturnsError_ForInvalidReference()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();

                LoadResult result = await fixture.Service.LoadScene(null);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.INVALID_SCENE_REFERENCE));
            });
        }

        /// <summary>
        /// Verifies that identical in-flight single-scene requests share the same load operation.
        /// </summary>
        [UnityTest]
        public IEnumerator SameSingleRequest_JoinsInFlightRequest()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA);
                UniTask<LoadResult> secondLoad = fixture.Service.LoadScene(sceneA);

                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);

                fixture.Loader.CompleteLoad(0);

                LoadResult firstResult = await firstLoad;
                LoadResult secondResult = await secondLoad;

                Assert.That(firstResult.Success, Is.True);
                Assert.That(secondResult.Success, Is.True);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that a second single-scene request is rejected while a different single transition is already running.
        /// </summary>
        [UnityTest]
        public IEnumerator DifferentSingleRequest_IsRejectedWhileAnotherSingleIsInFlight()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);
                AssetReference sceneB = CreateSceneReference(SCENE_B_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA);
                LoadResult secondLoad = await fixture.Service.LoadScene(sceneB);

                Assert.That(secondLoad.Success, Is.False);
                Assert.That(secondLoad.Error, Is.EqualTo(SceneLoaderMessages.DIFFERENT_SINGLE_TRANSITION_IN_PROGRESS));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);

                fixture.Loader.CompleteLoad(0);
                await firstLoad;
            });
        }

        /// <summary>
        /// Verifies that additive loads are rejected while an exclusive single-scene transition is in progress.
        /// </summary>
        [UnityTest]
        public IEnumerator AdditiveRequest_IsRejectedWhileSingleTransitionIsInFlight()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);
                AssetReference sceneB = CreateSceneReference(SCENE_B_GUID);

                UniTask<LoadResult> singleLoad = fixture.Service.LoadScene(sceneA);
                LoadResult additiveLoad = await fixture.Service.LoadScene(sceneB, LoadSceneMode.Additive, activateOnLoad: false);

                Assert.That(additiveLoad.Success, Is.False);
                Assert.That(additiveLoad.Error, Is.EqualTo(SceneLoaderMessages.SINGLE_TRANSITION_IN_PROGRESS));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);

                fixture.Loader.CompleteLoad(0);
                await singleLoad;
            });
        }

        /// <summary>
        /// Verifies that duplicate additive requests for the same scene join the same in-flight operation.
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateAdditiveRequest_JoinsInFlightRequest()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                UniTask<LoadResult> secondLoad = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);

                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);

                fixture.Loader.CompleteLoad(0);

                LoadResult firstResult = await firstLoad;
                LoadResult secondResult = await secondLoad;

                Assert.That(firstResult.Success, Is.True);
                Assert.That(secondResult.Success, Is.True);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that additive requests for different scenes can proceed in parallel.
        /// </summary>
        [UnityTest]
        public IEnumerator DifferentAdditiveRequests_CanRunInParallel()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);
                AssetReference sceneB = CreateSceneReference(SCENE_B_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                UniTask<LoadResult> secondLoad = fixture.Service.LoadScene(sceneB, LoadSceneMode.Additive, activateOnLoad: false);

                Assert.AreEqual(2, fixture.Loader.LoadCalls.Count);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loading));
                Assert.That(fixture.Service.GetState(sceneB), Is.EqualTo(ESceneLoadState.Loading));

                fixture.Loader.CompleteLoad(0);
                fixture.Loader.CompleteLoad(1);

                LoadResult firstResult = await firstLoad;
                LoadResult secondResult = await secondLoad;

                Assert.That(firstResult.Success, Is.True);
                Assert.That(secondResult.Success, Is.True);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
                Assert.That(fixture.Service.GetState(sceneB), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that conflicting activation policies reject duplicate in-flight single-scene requests.
        /// </summary>
        [UnityTest]
        public IEnumerator SameSingleRequest_WithConflictingActivationPolicy_IsRejected()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA, activateOnLoad: false);
                LoadResult secondLoad = await fixture.Service.LoadScene(sceneA);

                Assert.That(secondLoad.Success, Is.False);
                Assert.That(secondLoad.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadingWithDifferentActivationPolicy(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);

                fixture.Loader.CompleteLoad(0);
                await firstLoad;
            });
        }

        /// <summary>
        /// Verifies that conflicting activation policies reject duplicate in-flight additive requests.
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateAdditiveRequest_WithConflictingActivationPolicy_IsRejected()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                LoadResult secondLoad = await fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: true);

                Assert.That(secondLoad.Success, Is.False);
                Assert.That(secondLoad.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadingWithDifferentActivationPolicy(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);

                fixture.Loader.CompleteLoad(0);
                await firstLoad;
            });
        }

        /// <summary>
        /// Verifies that conflicting progress policies reject duplicate in-flight single-scene requests.
        /// </summary>
        [UnityTest]
        public IEnumerator SameSingleRequest_WithConflictingProgressPolicy_IsRejected()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA, reportProgress: false);
                LoadResult secondLoad = await fixture.Service.LoadScene(sceneA, reportProgress: true);

                Assert.That(secondLoad.Success, Is.False);
                Assert.That(secondLoad.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadingWithDifferentProgressPolicy(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);
                Assert.That(fixture.Loader.LoadCalls[0].ReportProgress, Is.False);

                fixture.Loader.CompleteLoad(0);
                await firstLoad;
            });
        }

        /// <summary>
        /// Verifies that conflicting progress policies reject duplicate in-flight additive requests.
        /// </summary>
        [UnityTest]
        public IEnumerator DuplicateAdditiveRequest_WithConflictingProgressPolicy_IsRejected()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(
                    sceneA,
                    LoadSceneMode.Additive,
                    activateOnLoad: false,
                    reportProgress: false);
                LoadResult secondLoad = await fixture.Service.LoadScene(
                    sceneA,
                    LoadSceneMode.Additive,
                    activateOnLoad: false,
                    reportProgress: true);

                Assert.That(secondLoad.Success, Is.False);
                Assert.That(secondLoad.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadingWithDifferentProgressPolicy(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);
                Assert.That(fixture.Loader.LoadCalls[0].ReportProgress, Is.False);

                fixture.Loader.CompleteLoad(0);
                await firstLoad;
            });
        }

        /// <summary>
        /// Verifies that additive loads can explicitly opt into progress reporting.
        /// </summary>
        [UnityTest]
        public IEnumerator AdditiveRequest_CanReportProgress_WhenExplicitlyEnabled()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(
                    sceneA,
                    LoadSceneMode.Additive,
                    activateOnLoad: false,
                    reportProgress: true);

                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);
                Assert.That(fixture.Loader.LoadCalls[0].ReportProgress, Is.True);

                fixture.Loader.CompleteLoad(0);
                LoadResult result = await load;

                Assert.That(result.Success, Is.True);
            });
        }

        /// <summary>
        /// Verifies that already loaded single scenes reject both duplicate single and additive follow-up requests.
        /// </summary>
        [UnityTest]
        public IEnumerator LoadedSingleScene_IsRejectedForDuplicateRequests()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(sceneA);
                fixture.Loader.CompleteLoad(0);
                await load;

                LoadResult duplicateSingle = await fixture.Service.LoadScene(sceneA);
                LoadResult duplicateAdditive = await fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);

                Assert.That(duplicateSingle.Success, Is.False);
                Assert.That(duplicateSingle.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadedAsActiveSingleScene(SCENE_A_GUID)));
                Assert.That(duplicateAdditive.Success, Is.False);
                Assert.That(duplicateAdditive.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoaded(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that a completed single-scene load is still rejected on a later duplicate request.
        /// </summary>
        [UnityTest]
        public IEnumerator CompletedSingleRequest_IsRejectedForFollowupRequest_AfterCompletion()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> firstLoad = fixture.Service.LoadScene(sceneA);
                fixture.Loader.CompleteLoad(0);

                LoadResult firstResult = await firstLoad;
                LoadResult secondResult = await fixture.Service.LoadScene(sceneA);

                Assert.That(firstResult.Success, Is.True);
                Assert.That(secondResult.Success, Is.False);
                Assert.That(secondResult.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadedAsActiveSingleScene(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);
            });
        }

        /// <summary>
        /// Verifies that already loaded additive scenes reject both duplicate additive and single follow-up requests.
        /// </summary>
        [UnityTest]
        public IEnumerator LoadedAdditiveScene_IsRejectedForDuplicateRequests()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                fixture.Loader.CompleteLoad(0);
                await load;

                LoadResult duplicateAdditive = await fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                LoadResult duplicateSingle = await fixture.Service.LoadScene(sceneA);

                Assert.That(duplicateAdditive.Success, Is.False);
                Assert.That(duplicateAdditive.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoadedAdditively(SCENE_A_GUID)));
                Assert.That(duplicateSingle.Success, Is.False);
                Assert.That(duplicateSingle.Error, Is.EqualTo(SceneLoaderMessages.AlreadyLoaded(SCENE_A_GUID)));
                Assert.AreEqual(1, fixture.Loader.LoadCalls.Count);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that a new single-scene transition cancels in-flight additive loads and cleans up their handles.
        /// </summary>
        [UnityTest]
        public IEnumerator SingleRequest_CancelsInFlightAdditiveRequests_AndCleansTheirHandles()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference loadedSceneA = CreateSceneReference(LOADED_SCENE_A_GUID);
                AssetReference additiveSceneB = CreateSceneReference(ADDITIVE_SCENE_B_GUID);
                AssetReference additiveSceneC = CreateSceneReference(ADDITIVE_SCENE_C_GUID);
                AssetReference singleSceneD = CreateSceneReference(SINGLE_SCENE_D_GUID);

                UniTask<LoadResult> loadSceneA = fixture.Service.LoadScene(loadedSceneA);
                fixture.Loader.CompleteLoad(0);
                await loadSceneA;

                UniTask<LoadResult> additiveLoadB = fixture.Service.LoadScene(additiveSceneB, LoadSceneMode.Additive, activateOnLoad: false);
                UniTask<LoadResult> additiveLoadC = fixture.Service.LoadScene(additiveSceneC, LoadSceneMode.Additive, activateOnLoad: false);

                Assert.That(fixture.Service.GetState(additiveSceneB), Is.EqualTo(ESceneLoadState.Loading));
                Assert.That(fixture.Service.GetState(additiveSceneC), Is.EqualTo(ESceneLoadState.Loading));

                UniTask<LoadResult> singleLoadD = fixture.Service.LoadScene(singleSceneD);

                LoadResult additiveResultB = await additiveLoadB;
                LoadResult additiveResultC = await additiveLoadC;

                Assert.That(additiveResultB.Success, Is.False);
                Assert.That(additiveResultB.Error, Is.EqualTo(SceneLoaderMessages.ADDITIVE_LOAD_CANCELED_BY_SINGLE_TRANSITION));
                Assert.That(additiveResultC.Success, Is.False);
                Assert.That(additiveResultC.Error, Is.EqualTo(SceneLoaderMessages.ADDITIVE_LOAD_CANCELED_BY_SINGLE_TRANSITION));

                fixture.Loader.CompleteLoad(1);
                fixture.Loader.CompleteLoad(2);
                await UniTask.Yield();

                CollectionAssert.AreEquivalent(new[] { ADDITIVE_SCENE_B_GUID, ADDITIVE_SCENE_C_GUID }, fixture.Loader.UnloadedSceneGuids);

                fixture.Loader.CompleteLoad(3);
                LoadResult singleResultD = await singleLoadD;

                Assert.That(singleResultD.Success, Is.True);
                CollectionAssert.AreEquivalent(new[] { ADDITIVE_SCENE_B_GUID, ADDITIVE_SCENE_C_GUID, LOADED_SCENE_A_GUID }, fixture.Loader.UnloadedSceneGuids);
                Assert.That(fixture.Service.GetState(loadedSceneA), Is.EqualTo(ESceneLoadState.None));
                Assert.That(fixture.Service.GetState(additiveSceneB), Is.EqualTo(ESceneLoadState.None));
                Assert.That(fixture.Service.GetState(additiveSceneC), Is.EqualTo(ESceneLoadState.None));
                Assert.That(fixture.Service.GetState(singleSceneD), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that single-scene transitions still succeed when cleanup of replaced scenes fails.
        /// </summary>
        [UnityTest]
        public IEnumerator SingleRequest_StillSucceeds_WhenCleanupOfPreviousSceneFails()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);
                AssetReference sceneB = CreateSceneReference(SCENE_B_GUID);

                UniTask<LoadResult> loadA = fixture.Service.LoadScene(sceneA);
                fixture.Loader.CompleteLoad(0);
                await loadA;

                fixture.Loader.FailNextUnload(new InvalidOperationException("Cleanup failed."));
                UniTask<LoadResult> loadB = fixture.Service.LoadScene(sceneB);
                LogAssert.Expect(LogType.Warning, SceneLoaderMessages.CleanupFailedDuringSingleTransition(SCENE_A_GUID, "Cleanup failed."));
                fixture.Loader.CompleteLoad(1);

                LoadResult resultB = await loadB;

                Assert.That(resultB.Success, Is.True);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
                Assert.That(fixture.Service.GetState(sceneB), Is.EqualTo(ESceneLoadState.Loaded));
                CollectionAssert.DoesNotContain(fixture.Loader.UnloadedSceneGuids, SCENE_A_GUID);
            });
        }

        /// <summary>
        /// Verifies that failed unload attempts keep the scene tracked until a later unload succeeds.
        /// </summary>
        [UnityTest]
        public IEnumerator UnloadScene_KeepsTrackingUntilUnloadSucceeds()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                fixture.Loader.CompleteLoad(0);
                await load;

                fixture.Loader.FailNextUnload(new InvalidOperationException("Unload failed."));
                LoadResult failedUnload = await fixture.Service.UnloadScene(sceneA);

                Assert.That(failedUnload.Success, Is.False);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));

                LoadResult successfulUnload = await fixture.Service.UnloadScene(sceneA);

                Assert.That(successfulUnload.Success, Is.True);
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.None));
            });
        }

        /// <summary>
        /// Verifies that activation with an invalid scene reference returns the expected error result.
        /// </summary>
        [UnityTest]
        public IEnumerator ActivateScene_ReturnsError_ForInvalidReference()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();

                LoadResult result = await fixture.Service.ActivateScene(null);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.INVALID_SCENE_REFERENCE));
            });
        }

        /// <summary>
        /// Verifies that activation rejects scenes that are not currently loaded.
        /// </summary>
        [UnityTest]
        public IEnumerator ActivateScene_ReturnsError_WhenSceneIsNotLoaded()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                LoadResult result = await fixture.Service.ActivateScene(sceneA);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.NotLoaded(SCENE_A_GUID)));
            });
        }

        /// <summary>
        /// Verifies that successful activation does not remove the loaded scene from tracking.
        /// </summary>
        [UnityTest]
        public IEnumerator ActivateScene_Succeeds_AndDoesNotMutateLoadedTracking()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                fixture.Loader.CompleteLoad(0);
                await load;

                LoadResult activationResult = await fixture.Service.ActivateScene(sceneA);

                Assert.That(activationResult.Success, Is.True);
                Assert.That(fixture.Loader.ActivateCallCount, Is.EqualTo(1));
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that activation rejects scenes that are already active.
        /// </summary>
        [UnityTest]
        public IEnumerator ActivateScene_RejectsAlreadyActiveScene()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(sceneA, LoadSceneMode.Additive, activateOnLoad: false);
                fixture.Loader.CompleteLoad(0);
                await load;
                fixture.ActivationState.IsActiveResult = true;

                LoadResult activationResult = await fixture.Service.ActivateScene(sceneA);

                Assert.That(activationResult.Success, Is.False);
                Assert.That(activationResult.Error, Is.EqualTo(SceneLoaderMessages.AlreadyActive(SCENE_A_GUID)));
                Assert.That(fixture.Loader.ActivateCallCount, Is.EqualTo(0));
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
            });
        }

        /// <summary>
        /// Verifies that activation requests are rejected while an exclusive single-scene transition is running.
        /// </summary>
        [UnityTest]
        public IEnumerator ActivateScene_IsRejectedWhileSingleTransitionIsInFlight()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);
                
                AssetReference sceneB = CreateSceneReference(SCENE_B_GUID);

                UniTask<LoadResult> singleLoad = fixture.Service.LoadScene(sceneA);
                LoadResult activationResult = await fixture.Service.ActivateScene(sceneB);

                Assert.That(activationResult.Success, Is.False);
                Assert.That(activationResult.Error, Is.EqualTo(SceneLoaderMessages.SINGLE_TRANSITION_IN_PROGRESS));

                fixture.Loader.CompleteLoad(0);
                await singleLoad;
            });
        }

        /// <summary>
        /// Verifies that unloading with an invalid scene reference returns the expected error result.
        /// </summary>
        [UnityTest]
        public IEnumerator UnloadScene_ReturnsError_ForInvalidReference()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();

                LoadResult result = await fixture.Service.UnloadScene(null);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.INVALID_SCENE_REFERENCE));
            });
        }

        /// <summary>
        /// Verifies that unloading rejects scenes that are not currently loaded.
        /// </summary>
        [UnityTest]
        public IEnumerator UnloadScene_ReturnsError_WhenSceneIsNotLoaded()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                LoadResult result = await fixture.Service.UnloadScene(sceneA);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.NotLoaded(SCENE_A_GUID)));
            });
        }

        /// <summary>
        /// Verifies that disposal performs best-effort cleanup for all tracked loaded scenes.
        /// </summary>
        [UnityTest]
        public IEnumerator Dispose_PerformsBestEffortCleanupOfTrackedScenes()
        {
            return UniTask.ToCoroutine(async () =>
            {
                SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);
                AssetReference sceneB = CreateSceneReference(SCENE_B_GUID);

                UniTask<LoadResult> loadA = fixture.Service.LoadScene(sceneA);
                fixture.Loader.CompleteLoad(0);
                await loadA;

                UniTask<LoadResult> loadB = fixture.Service.LoadScene(sceneB, LoadSceneMode.Additive, activateOnLoad: false);
                fixture.Loader.CompleteLoad(1);
                await loadB;

                fixture.Service.Dispose();
                await UniTask.Yield();

                CollectionAssert.AreEquivalent(
                    new[] { SCENE_A_GUID, SCENE_B_GUID },
                    fixture.Loader.UnloadedSceneGuids);
            });
        }

        /// <summary>
        /// Verifies that repeated disposal of the service is safe and does not throw.
        /// </summary>
        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            SceneLoaderServiceFixture fixture = new();

            Assert.DoesNotThrow(() => fixture.Service.Dispose());
            Assert.DoesNotThrow(() => fixture.Service.Dispose());
            Assert.DoesNotThrow(fixture.Dispose);
        }

        /// <summary>
        /// Verifies that loading rejects new requests after the service has been disposed.
        /// </summary>
        [UnityTest]
        public IEnumerator LoadScene_ReturnsDisposedError_AfterDispose()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                fixture.Service.Dispose();
                LoadResult result = await fixture.Service.LoadScene(sceneA);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.SERVICE_DISPOSED));
            });
        }

        /// <summary>
        /// Verifies that activation rejects new requests after the service has been disposed.
        /// </summary>
        [UnityTest]
        public IEnumerator ActivateScene_ReturnsDisposedError_AfterDispose()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                fixture.Service.Dispose();
                LoadResult result = await fixture.Service.ActivateScene(sceneA);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.SERVICE_DISPOSED));
            });
        }

        /// <summary>
        /// Verifies that unloading rejects new requests after the service has been disposed.
        /// </summary>
        [UnityTest]
        public IEnumerator UnloadScene_ReturnsDisposedError_AfterDispose()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                fixture.Service.Dispose();
                LoadResult result = await fixture.Service.UnloadScene(sceneA);

                Assert.That(result.Success, Is.False);
                Assert.That(result.Error, Is.EqualTo(SceneLoaderMessages.SERVICE_DISPOSED));
            });
        }

        /// <summary>
        /// Verifies that unloading the last tracked single scene is rejected by policy.
        /// </summary>
        [UnityTest]
        public IEnumerator UnloadScene_RejectsLastTrackedSingleScene()
        {
            return UniTask.ToCoroutine(async () =>
            {
                using SceneLoaderServiceFixture fixture = new();
                AssetReference sceneA = CreateSceneReference(SCENE_A_GUID);

                UniTask<LoadResult> load = fixture.Service.LoadScene(sceneA);
                fixture.Loader.CompleteLoad(0);
                await load;

                LoadResult unloadResult = await fixture.Service.UnloadScene(sceneA);

                Assert.That(unloadResult.Success, Is.False);
                Assert.That(unloadResult.Error, Is.EqualTo(SceneLoaderMessages.LAST_LOADED_SINGLE_SCENE_UNLOAD_NOT_SUPPORTED));
                Assert.That(fixture.Service.GetState(sceneA), Is.EqualTo(ESceneLoadState.Loaded));
                Assert.That(fixture.Loader.UnloadedSceneGuids, Is.Empty);
            });
        }

        private static AssetReference CreateSceneReference(string guid)
        {
            return new AssetReference(guid);
        }

    }
}
