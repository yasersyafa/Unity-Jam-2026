using UnityEngine;

public sealed class SlotAudio : MonoBehaviour
{
    [SerializeField] private SlotReelView reel;
    [SerializeField] private SlotWeaponRewards rewards;
    [SerializeField] private AudioCue stopped, awarded;
    [Header("Reel Chord Harmony")]
    [Tooltip("Optional shared chord settings used to pitch-shift reel stop sounds into harmony.")]
    [SerializeField] private SlotChordSettings stopChord;
    [Tooltip("Note slot in the chord for this reel (0 = root, 1 = middle tone, 2 = top tone).")]
    [SerializeField, Range(0, 2)] private int chordNoteIndex;

    private void OnEnable()
    {
        if (reel != null) reel.Stopped += Stop;
        if (rewards != null) rewards.WeaponAwarded += Award;
    }

    private void OnDisable()
    {
        if (reel != null) reel.Stopped -= Stop;
        if (rewards != null) rewards.WeaponAwarded -= Award;
    }

    private void Stop(SlotReelView value)
    {
        float pitch = stopChord != null ? stopChord.GetPitchMultiplier(chordNoteIndex) : 1f;
        AudioService.Ensure().Play(stopped, pitch);
    }

    private void Award(WeaponData value) { AudioService.Ensure().Play(awarded); }
}
