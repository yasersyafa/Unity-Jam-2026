# Unity project context

Inspected 2026-11-11 at commit `782bb14`, including the weapon visual working-tree
changes. Root: `D:/Gamedev/Unity/Unity-Jam-2026`.

## Confirmed environment and structure

- Unity 6000.6.8f1; URP 17.6.0; Input System 1.20.1; uGUI 2.6.0;
  Test Framework 1.8.0; PrimeTween is used by presentation code.
- First-party runtime code lives in `Assets/_Game/Scripts/` (`Game.Runtime`).
  Features include shooting/damage, slots, game flow, enemies, audio and UI.
  Runtime references Input System, TMP, uGUI and PrimeTween.
- EditMode and PlayMode assemblies exist in `Assets/_Game/Tests/`; audio and
  profiling have separate Editor assemblies. General authoring utilities live
  in `Assets/_Game/Editor/`.
- Enabled build scenes are MenuScene, GameScene and SampleScene, in that order.
  WeaponTest is a separate development scene excluded from builds.
- `AGENTS.md` has older structure/testing statements. The manifest, actual code
  and build settings above are the current evidence; its style and asset-safety
  guidance still applies.

## Confirmed ownership and conventions

- MonoBehaviour composition with serialized private references and ScriptableObject
  configuration. C# uses four spaces and PascalCase types/methods.
- `GameStateManager` owns Intro -> WeaponSelect -> Combat -> Boss -> Victory,
  plus GameOver; it coordinates slots, spawning and shooting permission.
- `ShootingController` owns cloned input, cooldown and ammo. `DamageType`
  subclasses resolve single, piercing, explosion and cone attacks; hit effects
  include slow. Hitscan/Stream damage is immediate. Projectile mode captures a
  target at launch and uses `ProjectileShotRunner` to deliver its captured damage
  and hit effects only at arrival; area queries use the impact world position.
- `WeaponData` stores balance, audio, sprite/prefab and shot visual configuration.
  `WeaponFeedback` owns recoil, equip and presentation instances. `WeaponVisual`
  owns the held weapon/muzzle flash; `WeaponShotPresentation` owns pooled Canvas
  effects and observes the gameplay flight state. Weapon swaps/input resets keep
  launched projectiles; runner disable/scene unload cancels them. Visual pool
  capacity cannot drop gameplay damage. See `Docs/Design/WeaponVisuals.md`.
- `SlotWeaponRewards` selects existing WeaponData references by tier. Do not
  create a parallel catalog or mutate weapon assets at runtime.
- World ParticleSystem effects use `VfxPool`; Canvas shot effects use an
  owner-local pool because their transforms and lifecycle differ.

## Validation and tooling

- Unity CLI 1.0.0-beta.17 can reach the live Editor through the already installed
  `com.unity.pipeline` 0.10.0-exp.1 package. Compilation, asset authoring, tests,
  Play Mode and screenshots are available. No Unity MCP provider was exposed.
- Use the existing Test Runner assemblies. For visual review use WeaponTest,
  then verify GameScene integration. No custom CI/build command was found.
- Preserve asset GUIDs and meta files. Author prefabs/assets through Unity;
  avoid unrelated scene, package, render pipeline and build setting changes.
- Multiplayer usage, device-specific performance and release-platform build
  readiness were not established by this feature inspection.

Evidence: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`,
`ProjectSettings/EditorBuildSettings.asset`, runtime/test asmdefs, GameStateManager,
ShootingController, WeaponData, WeaponFeedback, WeaponVisual, DamageType subclasses,
SlotWeaponRewards, VfxPool, WeaponTestController and the design/testing docs.
