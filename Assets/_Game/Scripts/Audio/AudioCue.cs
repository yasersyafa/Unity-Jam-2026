using UnityEngine;

[CreateAssetMenu(menuName = "Game/Audio Cue")]
public sealed class AudioCue : ScriptableObject
{
    [Tooltip("Variants selected randomly. Add at least one clip before playback.")]
    public AudioClip[] clips;
    [Range(0, 1)] public float volume = 1;
    [Tooltip("Random pitch range; 1 preserves original pitch.")]
    public Vector2 pitch = Vector2.one;
    [Min(0)] public float cooldown;
    [Min(1)] public int maximumVoices = 4;
    [Tooltip("Route through UI instead of SFX mixer group.")]
    public bool ui;
}
