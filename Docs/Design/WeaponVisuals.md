# Weapon visual prefabs

## Shot visuals from the weapon design CSV

All 15 CSV weapons are configured. `WeaponData > Shot Visual Profile` references
assets in `Assets/_Game/ScriptableObjects/WeaponVisuals/`. `_StandarWeapon` uses
the stone defaults; the older `Jackpot` asset uses hitscan defaults.

Projectile-mode weapons lock the first hit at launch and deliver damage **only on
arrival**. Ammo, cooldown, shot sound, muzzle flash and recoil occur at launch;
damage, slow, explosion damage and hitmarker occur at impact. Hitscan and Stream
weapons retain immediate damage through the existing damage types.

On a damageable hit, the projectile follows the centre of that collider,
independently of later cursor movement. If the target disappears or its hitbox
is disabled during flight, it finishes at the last known position and never
reacquires a respawned target. Single-target shots then deal no damage; explosions
still affect enemies around the last known world position. Area targets and
falloff are evaluated at arrival, not at launch.

A miss travels to the saved cursor position and disappears without impact or
damage, even if a new enemy later moves under that cursor. Non-damageable
obstacles retain their fixed impact point. Hitscan impact remains at the first
collider under the aim; piercing hitscan damage keeps its existing target set.

| Weapons | Mode and defaults |
| --- | --- |
| Bambu Runcing | Spear-shaped projectile, aligned to travel |
| Batu, Ketapel | Spinning stone with wind streak |
| Sandal | Spinning slipper silhouette with wind streak |
| Nerf Gun | Long orange dart |
| Water Gun | Blue stream while firing |
| Durian | Upright, no spin; 12 radial spikes at impact |
| LPG | Upright, no spin; orange explosion at impact |
| Glock, Uzi, M4A1, AWP, M134-A2 | Immediate impact, no airborne bullet |
| Flamethrower | Wide orange stream while firing |
| Rocket Launcher | Oriented rocket, orange fire trail, explosion on arrival |

### Adjusting in the Inspector

- **Mode:** Projectile delivers damage on arrival; Hitscan and Stream resolve it
  immediately. All weapons assigned Projectile mode use the arrival rule.
- **Sprite / Shape / Color / Size:** replace the code-native silhouette with a
  sprite, or tune its shape and tint. Size uses reference Canvas pixels.
- **Speed:** reference Canvas pixels per second; initial distance / speed sets
  travel time and therefore damage delay. **Spin:** degrees per second.
  **Orient To Travel** sets the launch angle; disable it to keep an object upright.
  Art should point along local +X before this rotation.
- **Trail:** tint/alpha, length and width. Zero alpha or length disables it.
- **Stream Length / Width:** Canvas pixel dimensions. Length is capped at the
  aim distance; visual reach does not change the damage type's world-space range.
- **Impact:** optional sprite, silhouette, color, diameter and lifetime. Fragment
  count/distance control radial spikes. Zero fragments disables the burst.
- **WeaponData > Visual Prefab > Muzzle Anchor:** position the explicit nozzle
  in Prefab Mode. Without an anchor, shots start at the top centre of the legacy
  weapon Image. Recoil and equip transforms also move the muzzle.

Most weapon bodies and all new shot effects initially use simple mesh silhouettes.
Replace body art on `WeaponSilhouette > Weapon Effect Graphic` in the visual prefab;
replace projectile/impact art on its shot profile. The existing M4 artwork and
animated muzzle flash are retained.

Streams begin only after an actual shot. Repeated shots keep one stream alive;
release, empty ammo, UI hover, focus loss, pause and shooting suspension stop it.
Weapon swaps and shooting resets stop the muzzle/stream but preserve launched
projectiles and their original damage payload. Pause freezes flight and damage.
Disabling/destroying the projectile runner or unloading its scene cancels pending
damage. Hiding or exhausting the VFX pool never cancels gameplay shots.

`ProjectileShotRunner`, attached to the shooting rig when first needed, owns
flight and damage. `WeaponShotPresentation` observes that same flight state and
owns only graphics. Explicit visual resets/disable clear graphics; normal weapon
replacement retains flying graphics. Both use scaled time. Each presentation
has a maximum of 128 active effects including fragments and reuses released
objects through a local pool; additional gameplay projectiles still resolve.

`Explosion_Durian`, `Explosion_LPG` and `Explosion_Rocket` have their legacy
**Impact Vfx** references cleared: the shot profile now owns the arrival effect,
avoiding an additional legacy explosion alongside the projectile impact graphic.

**Tools > Weapon Visuals > Create Missing Visuals** recreates missing default
profiles/prefabs and fills empty weapon references. It preserves existing profile
and prefab tuning, wires the M4 muzzle if missing, and clears the three legacy
explosion VFX references. It does not import or modify the CSV or balance values.

### Preview and validation

Open `Assets/Scenes/WeaponTest.unity`, press Play, select each weapon and use
**Uji Senjata**. Inspect travel direction, muzzle alignment and hit/miss feedback.
Hold Water Gun or Flamethrower and release; also try ammo exhaustion, UI hover,
Escape, pause, focus loss, Tab selection and switching weapons mid-flight.
Review at 16:9 and a narrower aspect ratio. The same WeaponData references are
used by gameplay, so no separate scene wiring is needed.

`WeaponVisualProfileTests` checks all 15 mappings, upright throws, bounds and
legacy compatibility. `WeaponShotPresentationTests` covers lifecycle, stream
gating, pool reuse/capacity, flight/impact, fragments and scaled-time pause.
`DamageTypePhysicsTests` verifies hitscan damage, target locking and visual-only
previews that never apply damage. `ProjectileShotTests` verifies arrival damage,
moving targets, area/slow effects, lost targets, pause, cancellation and damage
independence from the visual pool limit.
`WeaponShootingPresentationTests` exercises the accepted-shot controller path,
including final-ammo stream cancellation, delayed lethal impacts and in-flight
damage surviving weapon replacement and permission resets.
Those tests inject accepted shots, independently of OS focus; physical mouse
interaction remains a Game View check.

## Held weapon and muzzle flash

WeaponData now has an optional **Visual Prefab** field. Leave it empty to keep the
existing sprite-based presentation. Damage, fire rate, input, reward selection,
and hit detection continue to use the existing shooting system.

## Artist workflow

1. Duplicate `Assets/_Game/Prefabs/Weapons/M4_Visual.prefab` for another weapon.
2. Replace `WeaponImage`'s sprite. Its Image stretches inside the existing weapon
   HUD rectangle, matching the old sprite presentation.
3. Position `MuzzleAnchor` at the barrel tip. Anchor coordinates are relative to
   the weapon rectangle, so the effect follows weapon recoil and equip movement.
4. Replace `MuzzleFlash`'s sprite or the **Muzzle Frames** array on WeaponVisual.
   Frames play in array order. **Flash Duration** controls one sprite cycle and
   the lifetime of a single shot or release tail; automatic fire loops while held.
5. Assign the new prefab's WeaponVisual component to **WeaponData > Visual Prefab**.
6. Try it in WeaponTest. Adjust the muzzle anchor and flash size in Prefab Mode.

The M4 example uses the existing M4A1 sprite and three FireVFX sprites, with a
0.06-second flash. Its muzzle position is an initial art-based placement; review
and adjust it in the Game View to fit the final HUD composition.

The root has a 500 x 340 authoring rectangle for preview. At runtime, the instance
fills the existing weapon Image rectangle; the original HUD layout remains the
size authority. All visual graphics have raycast targets disabled.

Effects are optional. For a weapon with no muzzle flash, clear the Muzzle Flash
reference and frame list. The optional **On Shot** UnityEvent can trigger custom
child animations without weapon-name checks. Child animations should be finite
and self-contained. This event does not implement new projectile mechanics.

Repeated shots refresh visibility without restarting the sprite sequence or creating
more effect objects. Automatic fire loops the sprites continuously while Attack is
held after the first actual shot. Release finishes one short flash tail; a single
shot also finishes normally. Semi-automatic fire remains tied to actual shots.
Pause, shooting reset, disable, and weapon replacement hide the flash. Swapping to
a sprite-only weapon restores the legacy Image and immediately deactivates the
previous visual before destroying it.
