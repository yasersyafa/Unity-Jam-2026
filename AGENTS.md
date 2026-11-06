# Repository Guidelines

## Project Structure & Module Organization

This is a Unity 6 project using Editor **6000.6.8f1**, Universal Render Pipeline (URP), and the Input System.

- `Assets/Scenes/` contains `SampleScene.unity` and `GameScene.unity`. Only `SampleScene` is currently enabled in build settings.
- `Assets/_Game/Arts/Environment/` contains game-specific background and texture assets. Keep new gameplay code and assets grouped under `Assets/_Game/`.
- `Assets/Settings/` contains desktop/mobile URP assets, renderers, and volume profiles.
- `Assets/InputSystem_Actions.inputactions` defines input actions.
- `Packages/manifest.json` and `Packages/packages-lock.json` record dependencies; `ProjectSettings/` stores shared editor configuration.

No gameplay C# scripts or test assemblies are currently present in the working tree.

## Build, Test, and Development Commands

Use Unity Hub to open the repository with the pinned Editor version. For a macOS terminal launch:

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.6.8f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -projectPath "$PWD"
```

Open the intended scene and press Play for local development. Use **File > Build Profiles** to select the target platform, verify the scene list, and build. Add `GameScene` explicitly when it should ship. No custom build command or CI workflow is currently defined.

## Coding Style & Naming Conventions

For new C# code, use four-space indentation, PascalCase for types and methods, and camelCase for parameters and private fields. Match MonoBehaviour filenames to class names, such as `PlayerController.cs`. Prefer serialized private fields over public mutable fields. No repository formatter or lint configuration is present.

Move or rename assets through Unity, and commit their `.meta` files to preserve GUID references. Avoid unrelated scene and project-setting changes.

## Testing Guidelines

No test suite, coverage threshold, or explicit Unity Test Framework dependency is configured. Before submitting, confirm scripts compile, inspect the Console, and exercise affected scenes in Play Mode. Check desktop/mobile rendering when changing shared URP settings.

When adding automated tests, configure Unity Test Framework and separate Edit Mode and Play Mode assemblies. Use descriptive names such as `PlayerMovementTests.cs` and run them through Unity Test Runner.

## Commit & Pull Request Guidelines

Git history contains only `Initial check-in`; no established commit convention exists. Use concise, imperative subjects describing one focused change.

Pull requests should explain behavior changes, list affected scenes/assets, link relevant issues, and report validation performed. Include screenshots or video for visual changes. Exclude generated `Library/`, `Temp/`, `Logs/`, and build output, as specified by `.gitignore`.
