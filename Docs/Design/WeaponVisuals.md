# Weapon visual prefabs

## Shot visuals from the weapon design CSV

All 15 CSV weapons are configured. `WeaponData > Shot Visual Profile` references
assets in `Assets/_Game/ScriptableObjects/WeaponVisuals/`. `_StandarWeapon` uses
the stone defaults; the older `Jackpot` asset uses hitscan defaults.

Damage, ammo, fire rate, recoil, slow and target selection still resolve instantly
through the existing shooting/damage system. A projectile is presentation only:
its destination is captured before damage, so it can finish after a target dies.
Arrival never applies another hit. A miss travels to the saved aim and disappears
without an impact. Hitscan impact is shown at the first collider under the aim;
piercing damage still reaches its existing target set.

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

- **Mode:** Hitscan, Projectile or Stream. This changes presentation only.
- **Sprite / Shape / Color / Size:** replace the code-native silhouette with a
  sprite, or tune its shape and tint. Size uses reference Canvas pixels.
- **Speed:** reference Canvas pixels per second. **Spin:** degrees per second.
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
Reset, disable, replacement and scene unload clear active effects. Travel and
effect lifetimes use scaled time. Each presentation has a maximum of 128 active
effects (including fragments), and reuses released objects through a local pool.
Visual overload drops additional effects without affecting damage.

`Explosion_Durian`, `Explosion_LPG` and `Explosion_Rocket` have their legacy
**Impact Vfx** references cleared: the shot profile now owns the arrival effect,
avoiding an additional explosion at the instant damage is applied.

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
`DamageTypePhysicsTests` also verifies instant damage and a destination that
survives target movement/destruction without a second damage application.
`WeaponShootingPresentationTests` exercises the accepted-shot controller path,
including final-ammo stream cancellation, permission resets and legacy equips.
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
