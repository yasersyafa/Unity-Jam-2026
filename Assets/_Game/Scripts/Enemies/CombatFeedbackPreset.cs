using UnityEngine;

[CreateAssetMenu(menuName = "Game/Combat Feedback Preset")]
public sealed class CombatFeedbackPreset : ScriptableObject
{
    [Header("Camera")]
    [Min(0f)] public float shakeStrength = 0.06f;
    [Min(0.01f)] public float duration = 0.16f;
    [Header("Visual")]
    public Color damageColor = new Color(0.85f, 0.08f, 0.12f, 0.35f);
    [Range(0f, 1f)] public float criticalHealthRatio = 0.25f;
    [Header("Audio")]
    public AudioClip impactSound;
    [Range(0f, 1f)] public float volume = 0.7f;
}
