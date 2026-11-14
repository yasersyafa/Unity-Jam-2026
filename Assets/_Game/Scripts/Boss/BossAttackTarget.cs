using UnityEngine;

public enum BossProjectileKind
{
    Simple,
    Red,
    Blue
}

/// <summary>Initialization parameters for a pooled boss projectile.</summary>
public readonly struct BossProjectileLaunchData
{
    public readonly BossProjectileKind Kind;
    public readonly Sprite Sprite;
    public readonly Vector2 OrbitCenter;
    public readonly Vector2 TargetViewport;
    public readonly Vector2 StartRadius;
    public readonly Vector2 EndRadius;
    public readonly float InitialAngleRadians;
    public readonly float AngularSpeedRadians;
    public readonly float WaitDuration;
    public readonly float FlightDuration;
    public readonly Vector2 ScaleRange;
    public readonly float VisibleDuration;
    public readonly float InvisibleDuration;
    public readonly float Damage;
    public readonly Camera Camera;
    public readonly Transform Plane;
    public readonly PlayerHealth Player;

    public BossProjectileLaunchData(
        BossProjectileKind kind,
        Sprite sprite,
        Vector2 orbitCenter,
        Vector2 targetViewport,
        Vector2 startRadius,
        Vector2 endRadius,
        float initialAngleRadians,
        float angularSpeedRadians,
        float waitDuration,
        float flightDuration,
        Vector2 scaleRange,
        float visibleDuration,
        float invisibleDuration,
        float damage,
        Camera camera,
        Transform plane,
        PlayerHealth player)
    {
        Kind = kind;
        Sprite = sprite;
        OrbitCenter = orbitCenter;
        TargetViewport = targetViewport;
        StartRadius = startRadius;
        EndRadius = endRadius;
        InitialAngleRadians = initialAngleRadians;
        AngularSpeedRadians = angularSpeedRadians;
        WaitDuration = waitDuration;
        FlightDuration = flightDuration;
        ScaleRange = scaleRange;
        VisibleDuration = visibleDuration;
        InvisibleDuration = invisibleDuration;
        Damage = damage;
        Camera = camera;
        Plane = plane;
        Player = player;
    }
}

public sealed class BossAttackTarget : MonoBehaviour, IDamageable
{
    private SpriteRenderer visual;
    private Collider2D hitbox;
    private BossProjectileLaunchData launch;
    private Vector3 baseScale = Vector3.one;
    private bool configured;
    private bool canBeDamaged = true;
    private bool finished;
    private float elapsed;

    public BossProjectileKind Kind => configured ? launch.Kind : BossProjectileKind.Simple;
    public bool IsInvulnerable => !canBeDamaged;
    public bool IsVisible => visual == null || visual.enabled;
    public float Elapsed => elapsed;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        CacheComponents();
        if (!configured)
        {
            canBeDamaged = true;
            SetVisualAndHitbox(visible: true, shootable: true);
        }
    }

    private void OnDisable()
    {
        configured = false;
        finished = false;
        elapsed = 0f;
        canBeDamaged = true;
        SetVisualAndHitbox(visible: true, shootable: true);
    }

    /// <summary>Configures a Red or Blue boss projectile before activating it.</summary>
    public void Launch(in BossProjectileLaunchData data)
    {
        CacheComponents();
        launch = data;
        baseScale = transform.localScale;
        elapsed = 0f;
        finished = false;
        configured = data.Kind != BossProjectileKind.Simple;
        if (visual != null && data.Sprite != null)
            visual.sprite = data.Sprite;
        ApplyState(0f);
    }

    private void Update()
    {
        if (!configured || finished || Time.deltaTime <= 0f) return;
        Step(Time.deltaTime);
    }

    /// <summary>Advances projectile simulation by <paramref name="deltaTime"/> seconds.</summary>
    public void Step(float deltaTime)
    {
        if (!configured || finished || deltaTime < 0f) return;
        if (launch.Player != null && launch.Player.IsDead)
        {
            Despawn();
            return;
        }
        elapsed += deltaTime;
        ApplyState(elapsed);
    }

    public void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || !(damage > 0f) || !canBeDamaged) return;
        Despawn();
    }

    private void ApplyState(float time)
    {
        if (launch.Kind == BossProjectileKind.Red)
            ApplyRedState(time);
        else if (launch.Kind == BossProjectileKind.Blue)
            ApplyBlueState(time);
    }

    private void ApplyRedState(float time)
    {
        float waitDuration = Mathf.Max(0.01f, launch.WaitDuration);
        if (time < waitDuration)
        {
            SetVisualAndHitbox(visible: true, shootable: true);
            Vector2 viewport = EvaluateOrbitViewport(time, launch.StartRadius);
            transform.position = PointOnPlane(viewport);
            transform.localScale = baseScale * launch.ScaleRange.x;
            return;
        }

        // After the spin timer expires, surviving Red projectiles charge the player and become indestructible.
        SetVisualAndHitbox(visible: true, shootable: false);
        float chargeDuration = Mathf.Max(0.05f, launch.FlightDuration);
        float chargeProgress = Mathf.Clamp01((time - waitDuration) / chargeDuration);
        float eased = chargeProgress * chargeProgress;
        Vector2 launchViewport = EvaluateOrbitViewport(waitDuration, launch.StartRadius);
        Vector2 viewportPos = Vector2.Lerp(launchViewport, launch.TargetViewport, eased);
        transform.position = PointOnPlane(viewportPos);
        transform.localScale = baseScale * Mathf.Lerp(launch.ScaleRange.x, launch.ScaleRange.y, eased);
        if (chargeProgress >= 1f)
            StrikePlayer();
    }

    private void ApplyBlueState(float time)
    {
        float flightDuration = Mathf.Max(0.05f, launch.FlightDuration);
        float progress = Mathf.Clamp01(time / flightDuration);
        float eased = progress * progress;

        // Movement and scale advance even while the projectile is phased out and invisible.
        Vector2 radius = Vector2.Lerp(launch.StartRadius, launch.EndRadius, progress);
        Vector2 viewport = ClampViewport(EvaluateOrbitViewport(time, radius));
        transform.position = PointOnPlane(viewport);
        transform.localScale = baseScale * Mathf.Lerp(launch.ScaleRange.x, launch.ScaleRange.y, eased);

        float cycle = Mathf.Max(0f, launch.VisibleDuration) + Mathf.Max(0f, launch.InvisibleDuration);
        bool visible = cycle <= 0f || (time % cycle) < launch.VisibleDuration;
        SetVisualAndHitbox(visible, visible);

        if (progress >= 1f)
            StrikePlayer();
    }

    private Vector2 EvaluateOrbitViewport(float time, Vector2 radius)
    {
        float angle = launch.InitialAngleRadians + launch.AngularSpeedRadians * time;
        return launch.OrbitCenter + new Vector2(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y);
    }

    private static Vector2 ClampViewport(Vector2 viewport)
    {
        return new Vector2(Mathf.Clamp(viewport.x, 0.06f, 0.94f), Mathf.Clamp(viewport.y, 0.08f, 0.92f));
    }

    private Vector3 PointOnPlane(Vector2 viewport)
    {
        if (launch.Camera == null || launch.Plane == null)
            return new Vector3(viewport.x - 0.5f, viewport.y - 0.55f, -0.1f);
        var plane = new Plane(launch.Plane.forward, launch.Plane.position);
        var ray = launch.Camera.ViewportPointToRay(viewport);
        Vector3 world = plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : launch.Plane.position;
        return world - launch.Camera.transform.forward * 0.1f;
    }

    private void StrikePlayer()
    {
        if (finished) return;
        finished = true;
        if (launch.Player != null && !launch.Player.IsDead && launch.Damage > 0f)
            launch.Player.TakeDamage(launch.Damage);
        Despawn();
    }

    private void Despawn()
    {
        finished = true;
        gameObject.SetActive(false);
        GameplayLease.Despawn(gameObject);
    }

    private void CacheComponents()
    {
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
    }

    private void SetVisualAndHitbox(bool visible, bool shootable)
    {
        canBeDamaged = shootable;
        if (visual != null) visual.enabled = visible;
        if (hitbox != null) hitbox.enabled = shootable;
    }
}
