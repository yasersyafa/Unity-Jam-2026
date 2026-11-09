# Automated tests

Open **Window > General > Test Runner**, select **EditMode** or **PlayMode**, and choose **Run All**. Test assemblies are excluded from normal player builds.

## Current coverage

- Edit Mode: slot pattern classification, invalid digits, guaranteed minimum reward tiers, generated result consistency, weapon fire-rate boundaries, duration clamping, complete and incomplete reward pools.
- Play Mode: player damage, event counts, death clamping, disabled players, audio singleton ownership, SFX voice limits, mixer routing, missing clips, cooldowns and music fades while paused.

Tests create temporary objects and audio clips; they do not require production sound files or change scene assets. Audio tests verify engine playback state, not perceived sound quality. Test teardown restores time scale and background execution settings.

Runtime code is compiled in Game.Runtime so both test assemblies can reference it. Editor-only audio authoring code has a separate editor assembly. Test Framework 1.8.0 is a direct dependency, matching the version already resolved by the project.

## Further integration coverage

Add tests alongside changes to shooting hit selection and fire modes, slot open/close input and weapon expiration, enemy waves and boss transitions, and scene-to-scene music persistence. Full scene tests require valid gameplay scene references; the isolated tests here do not certify scene wiring or visual quality.
