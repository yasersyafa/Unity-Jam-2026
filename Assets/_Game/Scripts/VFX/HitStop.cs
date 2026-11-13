using PrimeTween;
using UnityEngine;

/// <summary>
/// Brief near-freeze of game time for impact. Uses a tiny non-zero time scale so systems that treat
/// timeScale 0 as "paused" (cursor, shooting, spawner) are not triggered. Anything that stores and
/// restores time scale (e.g. the slot pause) should read <see cref="UnfrozenTimeScale"/>.
/// </summary>
public static class HitStop
{
    private const float FrozenScale = 0.02f;
    private const float MaximumDuration = 0.15f;
    private static float restoreScale = 1f;
    private static float frozenSince;
    private static bool active;
    private static Tween timer;

    /// <summary>The time scale to return to once any running hit-stop ends.</summary>
    public static float UnfrozenTimeScale => active ? restoreScale : Time.timeScale;

    public static void Play(float seconds)
    {
        if (seconds <= 0f) return;
        if (!active)
        {
            if (Time.timeScale <= 0f) return; // already paused by something else
            restoreScale = Time.timeScale;
            frozenSince = Time.unscaledTime;
            active = true;
        }
        Time.timeScale = FrozenScale;
        // Overlapping kills extend the freeze, capped so a multi-kill never feels stuck.
        float remaining = Mathf.Min(seconds, MaximumDuration - (Time.unscaledTime - frozenSince));
        timer.Stop();
        if (remaining <= 0f) { Release(); return; }
        timer = Tween.Delay(remaining, Release, useUnscaledTime: true);
    }

    private static void Release()
    {
        if (!active) return;
        active = false;
        // Only restore if nobody else (slot pause, restart) changed time scale meanwhile.
        if (Mathf.Approximately(Time.timeScale, FrozenScale)) Time.timeScale = restoreScale;
    }
}
