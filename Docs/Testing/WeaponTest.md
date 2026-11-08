# Weapon testing

Open `Assets/Scenes/WeaponTest.unity` and press Play. Choose a weapon from the
rarity/name dropdown and press **Uji Senjata**. Click the arena to begin firing.
Hold Attack for automatic weapons; click separately for semi-automatic weapons.

Press **Tab** to reopen or close the selector. Selection disables shooting without
pausing time. The selected weapon has no expiry timer. Three stationary practice
targets accept damage and respawn automatically.

The scene uses the same ShootingController, WeaponFeedback, recoil and camera
feedback code as gameplay. It contains no slot flow, enemy waves or boss encounter.
The test controller only equips WeaponData references; it does not write to those
assets. Editing a WeaponData asset yourself still changes real balance data.

After adding new WeaponData assets, stop Play Mode, open this scene, and run
**Tools > Weapon Testing > Refresh Weapon Catalog**, then save the scene. Null and
duplicate catalog entries are ignored. The initial weapon is Batu_Biasa.

The scene is deliberately excluded from Build Settings. Production scenes and
prefabs do not depend on its controller. UI appearance and mouse/keyboard feel
should be reviewed in the Game View after the automated headless checks.

## Validation

Unity 6000.6.8f1 compiled the new scripts and authored the scene in an isolated
project copy. A headless Play Mode run passed 25 checks covering catalog entries,
equipping, input permission, target damage/respawn, and controller reactivation.
Final authoring also verified one EventSystem, no slot/wave owners, and readable
button styling. Real mouse/keyboard interaction and visual layout remain manual.
