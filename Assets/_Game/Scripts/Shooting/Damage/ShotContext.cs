using UnityEngine;

/// <summary>Everything a <see cref="DamageType"/> needs to resolve one shot.</summary>
public readonly struct ShotContext
{
    public Camera Camera { get; }
    /// <summary>Aim position in screen pixels.</summary>
    public Vector2 AimScreen { get; }
    /// <summary>Camera ray through the aim position.</summary>
    public Ray Ray { get; }
    /// <summary>Base damage per shot from the weapon.</summary>
    public float Damage { get; }
    public LayerMask HitMask { get; }
    /// <summary>Where the weapon sits on screen (bottom centre); origin for cone attacks.</summary>
    public Vector2 MuzzleScreen { get; }

    public ShotContext(Camera camera, Vector2 aimScreen, float damage, LayerMask hitMask)
    {
        Camera = camera;
        AimScreen = aimScreen;
        Ray = camera.ScreenPointToRay(aimScreen);
        Damage = damage;
        HitMask = hitMask;
        MuzzleScreen = new Vector2(camera.pixelWidth * 0.5f, 0f);
    }

    /// <summary>Screen position projected onto the 2D gameplay plane.</summary>
    public Vector2 ToWorld(Vector2 screen) => Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Camera.nearClipPlane));
}
