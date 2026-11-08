# Comic UI animation

The saved `SlotDemo` scene is wired to two reusable profiles under
`Assets/_Game/ScriptableObjects/UI/`:

- **SlotPresentation** controls panel timing, button scales, and reward colors
  and hold times. Reward arrays follow Common, Uncommon, Rare, Epic, Jackpot.
- **CombatHudAnimation** controls HP fill/trail, damage impact, critical accents,
  weapon equip, and wave banner timing. Each animation family has an enable toggle.

All presentation tweens use unscaled time. The slot flow owns pausing and resumes
combat after reel feedback, reward presentation, and panel exit have completed.
Repeated auto-roll calls are ignored while the slot is open. Reward presentation
never grants weapons; the existing reward system remains authoritative.

## Artist workflow

Edit the profiles to tune timing without code. Edit the prefab or scene references
to change art. Keep these visual transforms separate from their layout/input roots:

- `Machine`: animated slot panel; its fullscreen black backdrop stays fixed.
- `ComicButtonVisual`: button artwork and label; the button hit area stays fixed.
- `HealthVisual`: bar artwork and label; the Slider's fill reference is preserved.
- `WeaponEquipVisual`: equip movement; the weapon image child retains shot recoil.
- `ComicReward` and `WaveBanner`: editable Image/TMP content with nonblocking groups.

`DamageTrail` is a horizontal filled Image behind the current-health fill. It waits
briefly after a hit before shrinking. The displayed HP number always shows actual
health. Initial activation and rebinding synchronize immediately. Healing resets
the trail without playing damage impact.

If `PlayerDamageFeedback` uses a `CombatFeedbackPreset`, assign that same asset to
`PlayerHealthUI > Damage Preset` to share the critical-health threshold. Otherwise
both defaults are 25%. Weapon-duration warning defaults to 20% remaining.

Use the component context menus to preview panel entry and HP impact in Play Mode.
Use actual slot rewards to preview rarity colors and timings. Empty optional artwork
can be replaced by dragging sprites into the authored Image components.

## Rebuilding saved wiring

`Tools > Comic UI > Configure Saved Prefabs and SlotDemo` creates missing profiles
and visual objects, then updates saved assets. Close `SlotDemo` first; the tool
refuses to overwrite a loaded copy of that scene. Save any prefab edits beforehand.
The operation is idempotent; existing profiles retain their tuned values.

## Validation scenarios

Check slot opening at time scale zero, Skip, rapid Spin clicks, automatic rolls,
disable during entry/exit, each reward tier, rapid damage, fatal damage, critical
HP, and weapon expiry. Check at 16:9 and 16:10. The center aiming area should remain
clear, the backdrop should cover the screen throughout the transition, and no
visual transform should retain an animation offset after disable.

## Validation performed

Unity 6000.6.8f1 compiled the runtime and editor scripts in an isolated copy of the
project. A Play Mode smoke run passed 31 checks covering pause-safe transitions,
Skip and duplicate Spin, queued auto-roll, all five reward tiers, rapid damage and
trail rendering, HP re-enable, button hover/reset, critical timer and HP accents,
deferred wave banners, equip cleanup, and fatal damage. The run advanced 12,384
frames; it was not a frozen-frame inspection.

The isolated Editor also raised an unrelated `UnityEditor.Search.SearchDatabase`
indexing exception on startup. The harness recorded that known Editor stack
separately and continued; gameplay exceptions still failed the run. Visual review
at 16:9 and 16:10 remains manual because the smoke run used `-nographics`.

The repeatable smoke sources live under `Tools/Validation/ComicUI/`, outside the
Unity Assets tree so they do not enter player builds. Run them only in a disposable
project copy: they configure saved UI assets, change test Editor settings, and
exercise fatal damage before exiting the Editor.
