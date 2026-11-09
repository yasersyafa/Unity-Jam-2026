# Audio setup

Audio is wired into the existing scenes, gameplay and enemy prefabs, slot reels, rewards, and UI buttons. No production audio clips are included.

## Designer workflow

1. Import your audio clips into this folder.
2. Select the cue assets in `Assets/_Game/ScriptableObjects/Audio/` and fill their Clips arrays. Use Preview and Stop in the Inspector.
3. Set each scene's Scene Music component to its music clip, volume, and fade duration.
4. Override Shot Sound on individual WeaponData assets when weapons need distinct sounds.
5. Balance Music, SFX, and UI in GameAudio.mixer under Master.

AudioService loads the routed GameAudioService resource prefab automatically. All sounds are 2D. Music and cooldowns use unscaled time so the slot panel can pause gameplay without freezing audio.

For a new UI action, add AudioTrigger and connect its Play method through the action's UnityEvent. For combat and slot objects, use CombatAudio and SlotAudio and assign their references in the Inspector.

Empty cue assets warn once per service lifetime and produce no sound. PlayerDamageFeedback uses its original preset audio only when Damage Sound is unassigned.

The Game > Audio > Install Audio Wiring menu installs default references across game prefabs and scenes. Save all scenes first. This authoring tool changes assets; it is intended for initial setup, not repeated use after custom cue assignments.
