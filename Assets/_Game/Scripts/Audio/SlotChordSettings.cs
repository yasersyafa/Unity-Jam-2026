using UnityEngine;

public enum SlotNoteRoot
{
    C = 0,
    CSharp = 1,
    D = 2,
    DSharp = 3,
    E = 4,
    F = 5,
    FSharp = 6,
    G = 7,
    GSharp = 8,
    A = 9,
    ASharp = 10,
    B = 11
}

public enum SlotChordQuality
{
    Major,
    Minor,
    Suspended2,
    Suspended4,
    Diminished,
    Augmented,
    MajorSixth,
    DominantSeventhNo5,
    MajorSeventhNo5,
    MinorSeventhNo5,
    PowerOctave,
    Custom
}

/// <summary>
/// Defines a 3-note harmonic chord by pitch-shifting a shared slot SFX clip across reels 1-3.
/// </summary>
[CreateAssetMenu(menuName = "Game/Slot Chord Settings")]
public sealed class SlotChordSettings : ScriptableObject
{
    [Header("Base Clip Reference")]
    [Tooltip("Musical root pitch of the unshifted Slot 1 audio clip.")]
    [SerializeField] private SlotNoteRoot baseClipRoot = SlotNoteRoot.C;

    [Header("Target Chord")]
    [Tooltip("Root note for the 3-reel chord. When different from Base Clip Root, Reel 1 also shifts to this root.")]
    [SerializeField] private SlotNoteRoot chordRoot = SlotNoteRoot.C;
    [Tooltip("Chord interval shape across Reel 1, Reel 2, and Reel 3.")]
    [SerializeField] private SlotChordQuality chordQuality = SlotChordQuality.Major;
    [Tooltip("Optional octave shift applied to all 3 reels (-2 to +1 octaves).")]
    [SerializeField, Range(-2, 1)] private int octaveShift;

    [Header("Custom Intervals (Semitones from Chord Root)")]
    [Tooltip("Used when Chord Quality is set to Custom (X = Reel 1, Y = Reel 2, Z = Reel 3).")]
    [SerializeField] private Vector3Int customSemitones = new Vector3Int(0, 4, 7);

    public SlotNoteRoot BaseClipRoot => baseClipRoot;
    public SlotNoteRoot ChordRoot => chordRoot;
    public SlotChordQuality ChordQuality => chordQuality;
    public int OctaveShift => octaveShift;
    public Vector3Int CustomSemitones => customSemitones;

    /// <summary>Updates the chord configuration programmatically.</summary>
    public void Configure(
        SlotNoteRoot root,
        SlotChordQuality quality,
        int octaveOffset = 0,
        SlotNoteRoot clipRoot = SlotNoteRoot.C)
    {
        baseClipRoot = clipRoot;
        chordRoot = root;
        chordQuality = quality;
        octaveShift = Mathf.Clamp(octaveOffset, -2, 1);
    }

    /// <summary>Updates custom per-reel semitone intervals relative to the chord root.</summary>
    public void ConfigureCustom(
        SlotNoteRoot root,
        Vector3Int intervals,
        int octaveOffset = 0,
        SlotNoteRoot clipRoot = SlotNoteRoot.C)
    {
        Configure(root, SlotChordQuality.Custom, octaveOffset, clipRoot);
        customSemitones = intervals;
    }

    /// <summary>Returns the semitone shift relative to Base Clip Root for reel index 0, 1, or 2.</summary>
    public int GetSemitoneOffset(int reelIndex)
    {
        int clampedIndex = Mathf.Clamp(reelIndex, 0, 2);
        int rootOffset = (((int)chordRoot - (int)baseClipRoot) % 12 + 12) % 12;
        int interval = GetIntervalSemitones(chordQuality, customSemitones, clampedIndex);
        return rootOffset + interval + octaveShift * 12;
    }

    /// <summary>Returns the Unity AudioSource pitch multiplier for reel index 0, 1, or 2.</summary>
    public float GetPitchMultiplier(int reelIndex)
    {
        return SemitonesToPitch(GetSemitoneOffset(reelIndex));
    }

    /// <summary>Converts a semitone offset into a clamped AudioSource pitch multiplier.</summary>
    public static float SemitonesToPitch(int semitones)
    {
        return Mathf.Clamp(Mathf.Pow(2f, semitones / 12f), 0.1f, 3f);
    }

    private static int GetIntervalSemitones(SlotChordQuality quality, Vector3Int custom, int reelIndex)
    {
        switch (quality)
        {
            case SlotChordQuality.Major: return Select(reelIndex, 0, 4, 7);
            case SlotChordQuality.Minor: return Select(reelIndex, 0, 3, 7);
            case SlotChordQuality.Suspended2: return Select(reelIndex, 0, 2, 7);
            case SlotChordQuality.Suspended4: return Select(reelIndex, 0, 5, 7);
            case SlotChordQuality.Diminished: return Select(reelIndex, 0, 3, 6);
            case SlotChordQuality.Augmented: return Select(reelIndex, 0, 4, 8);
            case SlotChordQuality.MajorSixth: return Select(reelIndex, 0, 4, 9);
            case SlotChordQuality.DominantSeventhNo5: return Select(reelIndex, 0, 4, 10);
            case SlotChordQuality.MajorSeventhNo5: return Select(reelIndex, 0, 4, 11);
            case SlotChordQuality.MinorSeventhNo5: return Select(reelIndex, 0, 3, 10);
            case SlotChordQuality.PowerOctave: return Select(reelIndex, 0, 7, 12);
            case SlotChordQuality.Custom: return reelIndex == 0 ? custom.x : (reelIndex == 1 ? custom.y : custom.z);
            default: return Select(reelIndex, 0, 4, 7);
        }
    }

    private static int Select(int index, int first, int second, int third)
    {
        return index == 0 ? first : (index == 1 ? second : third);
    }
}
