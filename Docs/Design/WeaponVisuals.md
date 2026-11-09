# Weapon visual prefabs

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
