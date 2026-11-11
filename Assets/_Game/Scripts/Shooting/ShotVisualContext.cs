using UnityEngine;

/// <summary>Presentation snapshot captured before damage can destroy or move the hit collider.</summary>
public readonly struct ShotVisualContext
{
    public Vector2 DestinationScreen { get; }
    public bool HasImpact { get; }

    public ShotVisualContext(Vector2 destinationScreen, bool hasImpact)
    {
        DestinationScreen = destinationScreen;
        HasImpact = hasImpact;
    }

    public static ShotVisualContext Capture(in ShotContext shot)
    {
        if (!DamageQuery.First(shot, out RaycastHit2D hit)) return new ShotVisualContext(shot.AimScreen, false);
        return new ShotVisualContext(shot.Camera.WorldToScreenPoint(hit.point), true);
    }
}
