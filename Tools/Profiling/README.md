# Performance optimization validation

Starting commit: c26468d74174ab58f6954a69104c8301c089b239.
Machine: Intel Core i7-14700F, NVIDIA RTX 3080 Ti, driver 32.0.15.9186.
Unity: 6000.6.8f1. Targets: Windows x64 and desktop WebGL, 1920x1080, 60 FPS.

The supplied capture contains only 2,000 frames (~5.46 seconds) near victory.
It is not a full-session baseline. No measured before/after performance gain has
been established. Do not infer release performance from Editor test results.

The working tree initially already contained changes to:
- Assets/Settings/UniversalRenderPipelineGlobalSettings.asset
- ProjectSettings/EditorBuildSettings.asset
- ProjectSettings/ProjectAuditorSettings.asset
- ProjectSettings/ProjectSettings.asset

These user changes must be preserved.

## Reproducible builds

Run Tools/Profiling/Build-Performance.ps1 with -Target StandaloneWindows64 or
WebGL, and optionally -Development. The script uses the installed Unity CLI,
explicit build options, the existing enabled scenes and current project settings.
It does not enable Deep Profile or silently alter stripping, quality or heap size.
Build output is under Builds/Performance. Use build status to verify completion.

## Measurement gates

Capture three comparable runs per target, with the same input sequence and random
seed: startup, slot animation, early/dense waves, both boss phases, victory/restart.
Separate loading from gameplay. Record P50/P95/P99, GC allocation, CPU/GPU time,
pool misses, peak active instances per prefab, peak heap, build size and startup.
Do not use the old capture to choose pool capacities or browser heap size.

Accept only visually equivalent changes; retain current subdivision, fragment
counts and artistic render passes. BC7 overrides, Canvas separation, feature
removal, stripping level and Wasm optimization require actual A/B builds and
visual comparisons before adoption. Initial browser heap must be computed from
measured peak used heap plus 20%, rounded up to 16 MiB, maximum 2048 MiB.

Acceptance: P95 <= 16.67 ms, P99 <= 20 ms, no recurrent attributable >33.33 ms
hitch, no P95/P99 regression >5%, and no recurring pool miss after calibrated
prewarm. Full-session measurements and visual comparison remain required.

## Implementation and validation status

Implemented: reusable Genie vertex storage and matrix/per-panel caches; original
TMP text geometry caching; per-prefab scene-owned gameplay pools; enemy and
projectile reset; boss pool with nine inactive prewarmed targets; generation-aware
projectile target validation; cached health-bar font; cached procedural weapon
outlines. Enemy health-bar objects are retained with their pooled enemy.

Validation performed on 2026-11-13:
- Play Mode suite: 73/73 passed (includes pool reuse, random preservation,
  cap behavior and stale projectile generation).
- Edit Mode suite: 90/90 passed after disabling Profiler recording. The first
  run's exporter test was blocked by recording being active.
- Windows development build: succeeded, 0 errors, 11 shader warnings,
  293,813,907 bytes. Output: Builds/Performance/StandaloneWindows64/Development.
- Windows release build: succeeded, 0 errors. Output:
  Builds/Performance/StandaloneWindows64/Release.
- Web Build Support installation completed successfully via Unity CLI.
- WebGL development and release builds both failed with CS7036 in
  com.unity.collections 6.6.0, NativeList.cs lines 937 and 951: NativeArray<T>.ReadOnly
  requires a safety argument. PackageCache has not been patched.

Still pending (the complete P1/P2 plan is NOT finished):
- Full-session before/after captures, three runs per platform and visual review.
- Measured enemy/projectile/VFX/shot-effect prewarm capacities, staged prewarm
  behind the transition, and VFX/shot-effect prewarm integration.
- Allocation/resize/interruption tests for Genie and extended death/restart tests.
- A/B sub-Canvas experiment, BC7 visual comparisons and renderer feature tests.
- Browser heap measurement, stripping Medium/High validation and Wasm LTO A/B.
- Browser launch, actual runtime frame percentiles and proof of the 60 FPS target.

Acceptance is therefore incomplete. Successful tests/builds do not establish a
performance improvement or visual equivalence. Shader warnings include a pow()
negative-base warning in FullscreenWind; that shader was not changed here.
