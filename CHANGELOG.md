# Changelog

## [1.0.0] - 2026-05-26

- Initial public release of the package.
- Low-level scene loading API exposed through `ISceneLoaderService`.
- Scene state queries exposed through `ESceneLoadState`.
- Scene loading progress exposed through `ISceneLoadingProgress`.
- Scene loading results exposed through `LoadResult`.
- DI installers for VContainer, Zenject/Extenject, and Reflex.
- Additive scene loads, single-scene transitions, explicit progress reporting, activation control, and request cancellation.
