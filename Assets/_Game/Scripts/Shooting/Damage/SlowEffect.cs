using UnityEngine;

/// <summary>Slows targets that implement <see cref="ISlowable"/>; others are unaffected.</summary>
[CreateAssetMenu(menuName = "Game/Damage/Effects/Slow")]
public sealed class SlowEffect : HitEffect
{
    [Tooltip("Speed while slowed (0.6 = 40% slower).")]
    [SerializeField, Range(0.05f, 1f)] private float speedMultiplier = 0.6f;
    [Tooltip("Seconds; hitting again refreshes the duration.")]
    [SerializeField, Min(0.01f)] private float duration = 1.5f;

    public override void Apply(Component target)
    {
        if (target.TryGetComponent(out ISlowable slowable)) slowable.ApplySlow(speedMultiplier, duration);
    }
}
