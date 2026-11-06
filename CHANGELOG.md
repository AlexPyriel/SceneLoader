# Changelog

## [2.0.0] - 2026-11-06

Breaking release for projects on Zenject or Reflex. VContainer projects upgrade with no code changes.

### Changed

- `SceneLoaderService`, `AddressableSceneLoader`, `UnitySceneActivationState` and `SceneLoadingProgress` are
  `public sealed` with public constructors.
- `IAddressableSceneLoader`, `ISceneActivationState` and `ISceneLoadingProgressReporter` are public, so a project can
  replace the Addressables loader or the active-scene probe.

### Removed

- Zenject installer (`SceneLoaderZenjectInstaller`) and Reflex installer (`SceneLoaderReflexInstaller`). The README
  section "Using with Zenject / Reflex" shows the replacement bindings.

## [1.0.0] - 2026-05-26

- Initial public release of the package.
- Low-level scene loading API exposed through `ISceneLoaderService`.
- Scene state queries exposed through `ESceneLoadState`.
- Scene loading progress exposed through `ISceneLoadingProgress`.
- Scene loading results exposed through `LoadResult`.
- DI installers for VContainer, Zenject/Extenject, and Reflex.
- Additive scene loads, single-scene transitions, explicit progress reporting, activation control, and request cancellation.
