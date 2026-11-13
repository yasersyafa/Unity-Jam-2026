using PrimeTween;
using UnityEngine;

/// <summary>
/// Comic pop-up sprite (BOOM!) that waits a beat, springs in with an elastic bounce and a random tilt, then fades.
/// Spawned through <see cref="VfxPool"/>, which reactivates the instance, so the animation restarts in OnEnable.
/// Runs in unscaled time so it still plays through the kill hit-stop.
/// </summary>
[DisallowMultipleComponent]
public sealed class ComicPop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [Tooltip("Unscaled seconds hidden before popping, so it lands just after the death smoke.")]
    [SerializeField, Min(0f)] private float delay = 0.12f;
    [Tooltip("Visible time after the delay, including the fade.")]
    [SerializeField, Min(0.05f)] private float duration = 0.7f;
    [Tooltip("Length of the elastic scale-in at the start of the visible time.")]
    [SerializeField, Min(0.05f)] private float popDuration = 0.35f;
    [SerializeField, Range(0f, 45f)] private float maxRoll = 12f;
    [SerializeField] private float rise = 0.25f;
    private Transform body;
    private Vector3 scale, position;
    private Quaternion rotation;
    private Color color;
    private bool initialized;
    private Tween animation;

    private void Initialize()
    {
        if (initialized || sprite == null) return;
        initialized = true;
        body = sprite.transform;
        scale = body.localScale;
        position = body.localPosition;
        rotation = body.localRotation;
        color = sprite.color;
    }

    private void OnEnable()
    {
        Initialize();
        if (sprite == null) return;
        body.localScale = Vector3.zero; // hidden during the delay
        body.localPosition = position;
        body.localRotation = rotation * Quaternion.Euler(0f, 0f, Random.Range(-maxRoll, maxRoll));
        sprite.color = color;
        animation = Tween.Custom(0f, duration, duration, Animate, startDelay: delay, useUnscaledTime: true);
    }

    private void Animate(float time)
    {
        body.localScale = scale * OutElastic(Mathf.Clamp01(time / popDuration));
        body.localPosition = position + Vector3.up * (rise * time / duration);
        float fadeStart = duration * 0.7f;
        Color tint = color;
        tint.a = color.a * (time < fadeStart ? 1f : 1f - (time - fadeStart) / (duration - fadeStart));
        sprite.color = tint;
    }

    // Overshoots past 1 and settles with a few shrinking bounces.
    private static float OutElastic(float x)
    {
        if (x <= 0f) return 0f;
        if (x >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * x) * Mathf.Sin((x * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
    }

    private void OnDisable()
    {
        animation.Stop();
        if (!initialized) return;
        body.localScale = scale;
        body.localPosition = position;
        body.localRotation = rotation;
        sprite.color = color;
    }
}
