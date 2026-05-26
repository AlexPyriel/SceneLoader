# GDF Scene Loader

Addressables-backed runtime scene transition service for Unity.

Package name: `com.gdf.scene-loader`  
Assembly name: `GDF.SceneLoader`

## Features

- Public API: `ISceneLoaderService`, `ISceneLoadingProgress`, `LoadResult`, and `ESceneLoadState`.
- `LoadSceneMode.Single` transitions are exclusive.
- `LoadSceneMode.Additive` transitions can run in parallel for different scene references.
- `LoadSceneMode.Single` rejects additive loads while the single-scene transition is running.
- Duplicate protection:
  - same scene request joins the already in-flight load or is rejected when the activation policy conflicts;
  - duplicate additive requests for the same scene join the already in-flight load or are rejected when the activation policy conflicts;
  - additive requests return an explicit error while a single-scene transition is running;
- Loads a target scene by Unity Addressables `AssetReference` with configurable `LoadSceneMode`, `activateOnLoad`, and `reportProgress`.
- `LoadScene` can either activate the scene immediately or leave it preloaded for later `ActivateScene`.
- Completing a `LoadSceneMode.Single` transition cleans up previously tracked scene handles.
- `LoadSceneMode.Additive` is supported with per-scene in-progress guards.
- `GetState` reports the externally visible state of a scene reference.
- Manual flow support:
  - `ActivateScene` activates a loaded scene by `AssetReference`;
  - `UnloadScene` releases a loaded scene by reference.
- Explicit operation result via `LoadResult`.

## Requirements

- Unity `6000.0+`
- `com.unity.addressables` `2.8.1+`
- `com.cysharp.unitask`

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.scene-loader": "https://github.com/AlexPyriel/SceneLoader.git#v1.0.0"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/SceneLoader.git#v1.0.0`

## Quick Start

1. Resolve the target `AssetReference` in your application layer.
2. Register the package through the DI installer that matches your project's container.
3. Call `LoadScene`.

```csharp
using Cysharp.Threading.Tasks;
using SceneLoader;
using UnityEngine.AddressableAssets;

public sealed class GameFlow
{
    private readonly ISceneLoaderService _sceneLoader;
    private readonly AssetReference _gameplayScene;
    private readonly AssetReference _hudScene;

    public GameFlow(ISceneLoaderService sceneLoader, AssetReference gameplayScene, AssetReference hudScene)
    {
        _sceneLoader = sceneLoader;
        _gameplayScene = gameplayScene;
        _hudScene = hudScene;
    }

    public UniTask<LoadResult> LoadGameplay()
    {
        return _sceneLoader.LoadScene(_gameplayScene, reportProgress: true);
    }

    public UniTask<LoadResult> LoadHudOverlay()
    {
        return _sceneLoader.LoadScene(
            _hudScene,
            loadSceneMode: UnityEngine.SceneManagement.LoadSceneMode.Additive,
            activateOnLoad: false,
            reportProgress: false);
    }
}
```

For a manual activation flow, load the target scene from the loading scene and activate it only when the
loading screen is ready to go away:

```csharp
await _sceneLoader.LoadScene(
    gameplaySceneReference,
    loadSceneMode: UnityEngine.SceneManagement.LoadSceneMode.Additive,
    activateOnLoad: false,
    reportProgress: true);
await _sceneLoader.ActivateScene(gameplaySceneReference);
await _sceneLoader.UnloadScene(loadingSceneReference);
```

Use `reportProgress: true` only for the load that should own the shared `ISceneLoadingProgress` channel during a
transition. For example, a loading scene can be shown first with `reportProgress: false`, while the later additive
target-scene load reports the progress that drives the visible loading bar.

`GetState` is useful when the outer flow needs to make a decision before calling into the loader:

```csharp
if (_sceneLoader.GetState(gameplaySceneReference) == ESceneLoadState.Loading)
{
    return;
}
```

## Assembly Definition Notes

`GDF.SceneLoader` uses `autoReferenced: true`.

In most cases, no explicit `.asmdef` reference is required.

## DI Policy

The package exposes `ISceneLoadingProgress` for scene loading progress updates and keeps the writable progress sink
internal.
Use the DI installer that matches your project's container to wire the package services.

Scene reference resolution stays in the host project. Resolve scene keys or enums to `AssetReference` in the outer flow,
then pass the resolved reference into the package API.

### VContainer

Use the VContainer installer when your project uses VContainer. UPM installs are enabled automatically when the
`jp.hadashikick.vcontainer` package is present. Source-based or asset-based installs require the
`SCENE_LOADER_VCONTAINER` scripting define and a `VContainer` assembly in the project:

```csharp
new SceneLoader.Installers.VContainer.SceneLoaderVContainerInstaller().Install(builder);
```

### Zenject

Use the Zenject installer when your project uses Zenject or Extenject. UPM installs are enabled automatically when the
`com.mathijsbakker.extenject` package is present. Source-based or asset-based installs require the
`SCENE_LOADER_ZENJECT` scripting define and a `Zenject` assembly in the project.

```csharp
SceneLoader.Installers.Zenject.SceneLoaderZenjectInstaller.Install(Container);
```

### Reflex

Use the Reflex installer when your project uses Reflex. UPM installs are enabled automatically when the
`com.gustavopsantos.reflex` package is present. Source-based or asset-based installs require the
`SCENE_LOADER_REFLEX` scripting define and a `Reflex` assembly in the project.

Add `SceneLoader.Installers.Reflex.SceneLoaderReflexInstaller` to your Reflex `ProjectScope` prefab. Reflex discovers
`IInstaller` components on the scope automatically and installs the package services when the scope is built.

```csharp
// Add this component to your Reflex ProjectScope prefab.
public sealed class SceneLoaderReflexInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        // SceneLoader package services are registered here.
    }
}
```
