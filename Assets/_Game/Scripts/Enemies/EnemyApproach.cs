using UnityEngine;

[RequireComponent(typeof(Enemy))]
public sealed class EnemyApproach : MonoBehaviour, ISlowable
{
    public const float SafeSpiralViewportHalfWidth = 0.43f;
    public const float SafeSpiralViewportHalfHeight = 0.35f;

    private Enemy enemy;
    private PlayerHealth target;
    private Vector3 originalScale;
    private float elapsed;
    private bool initialized;
    private Vector3 entryStart, destination, control, viewportRight, viewportUp, viewportCenter;
    private float entryElapsed, phase;
    private float spinDirection = 1f;
    private bool hasViewportCenter;
    private bool entering;
    private bool finished;
    private float nextShot;
    private Transform projectileParent;
    private GameplayPrefabPool projectilePool;
    public void SetProjectilePool(GameplayPrefabPool pool) { projectilePool = pool; }
    public void SetProjectileParent(Transform parent) { projectileParent = parent; }
    private float amplitudeMultiplier = 1f, periodMultiplier = 1f;
    public void ConfigureDifficulty(float amplitude, float period)
    {
        amplitudeMultiplier = Mathf.Max(0f, amplitude);
        periodMultiplier = Mathf.Max(0.1f, period);
    }
    public bool IsFinished => enemy == null || enemy.IsDead || !isActiveAndEnabled || finished;

    private float slowMultiplier = 1f;
    private float slowRemaining;
    /// <summary>Current speed scale applied to every timed behaviour (movement, entry, ranged fire).</summary>
    public float SpeedMultiplier => slowRemaining > 0f ? slowMultiplier : 1f;

    public void ApplySlow(float speedMultiplier, float duration)
    {
        float multiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);
        // Strongest slow wins while active; any hit refreshes the duration.
        slowMultiplier = slowRemaining > 0f ? Mathf.Min(slowMultiplier, multiplier) : multiplier;
        slowRemaining = Mathf.Max(slowRemaining, duration);
    }
    public void Initialize(PlayerHealth player)
    {
        enemy = GetComponent<Enemy>();
        target = player;
        originalScale = transform.localScale;
        elapsed = 0f;
        slowRemaining = 0f;
        slowMultiplier = 1f;
        entering = false;
        hasViewportCenter = false;
        viewportRight = viewportUp = Vector3.zero;
        nextShot = enemy.Data.ShotInterval;
        finished = false;
        initialized = enemy.Data != null;
        if (initialized) transform.localScale = originalScale * enemy.Data.InitialScale;
    }
    public void Initialize(PlayerHealth player, Vector3 destination, float bendDirection, float phase, Vector3 viewportRight, Vector3 viewportUp)
    {
        Initialize(player, destination, bendDirection, phase, viewportRight, viewportUp, destination, bendDirection);
        hasViewportCenter = false;
    }
    public void Initialize(PlayerHealth player, Vector3 destination, float bendDirection, float phase, Vector3 viewportRight, Vector3 viewportUp, Vector3 viewportCenter, float spinDirection)
    {
        Initialize(player);
        this.destination = destination;
        this.phase = phase;
        this.viewportRight = viewportRight;
        this.viewportUp = viewportUp;
        this.viewportCenter = viewportCenter;
        this.spinDirection = spinDirection >= 0f ? 1f : -1f;
        hasViewportCenter = true;
        entryStart = transform.position;
        Vector3 delta = destination - entryStart;
        Vector3 perpendicular = Vector3.Cross(viewportRight, viewportUp).normalized;
        perpendicular = Vector3.Cross(perpendicular, delta).normalized;
        control = (entryStart + destination) * 0.5f + perpendicular * (delta.magnitude * enemy.Data.EntryCurvature * bendDirection);
        entryElapsed = 0f;
        entering = true;
    }
    public static Vector3 EvaluateSpiralPosition(
        Vector3 destination,
        Vector3 center,
        Vector3 viewportRight,
        Vector3 viewportUp,
        float elapsed,
        float approachProgress,
        float phase,
        float spinDirection,
        float startRadius,
        float endRadius,
        float period,
        float centerPull,
        float amplitudeMultiplier,
        float periodMultiplier,
        float verticalScale = 0.84f)
    {
        float progress = Mathf.Clamp01(approachProgress);
        Vector3 orbitCenter = Vector3.Lerp(destination, center, Mathf.Clamp01(centerPull) * progress);
        if (viewportRight == Vector3.zero || viewportUp == Vector3.zero) return orbitCenter;

        float blendedPeriodMult = Mathf.Lerp(1f, Mathf.Max(0.1f, periodMultiplier), 0.3f);
        float safePeriod = Mathf.Max(0.1f, period * blendedPeriodMult);
        float dir = spinDirection >= 0f ? 1f : -1f;
        float easedTime = elapsed * (1.4f - 0.4f * progress);
        float angle = phase + dir * (easedTime * Mathf.PI * 2f / safePeriod);
        float ramp = Mathf.Clamp01(elapsed / 0.4f);

        float clampedAmp = Mathf.Max(0f, amplitudeMultiplier);
        float spiralAmpScale = clampedAmp <= 0f ? 0f : Mathf.Lerp(1f, clampedAmp, 0.25f);
        float radiusX = Mathf.Lerp(Mathf.Max(0f, startRadius), Mathf.Max(0f, endRadius), progress) * spiralAmpScale * ramp;
        float radiusY = radiusX * Mathf.Clamp(verticalScale, 0.2f, 1.2f);

        Vector3 candidate = orbitCenter
            + viewportRight * (Mathf.Cos(angle) * radiusX)
            + viewportUp * (Mathf.Sin(angle) * radiusY);

        Vector3 fromCenter = candidate - center;
        float rightLenSq = viewportRight.sqrMagnitude;
        float upLenSq = viewportUp.sqrMagnitude;
        if (rightLenSq > 0.0001f && upLenSq > 0.0001f)
        {
            float vx = Vector3.Dot(fromCenter, viewportRight) / rightLenSq;
            float vy = Vector3.Dot(fromCenter, viewportUp) / upLenSq;
            float normSq = (vx / SafeSpiralViewportHalfWidth) * (vx / SafeSpiralViewportHalfWidth)
                         + (vy / SafeSpiralViewportHalfHeight) * (vy / SafeSpiralViewportHalfHeight);
            if (normSq > 1f)
            {
                float inv = 1f / Mathf.Sqrt(normSq);
                return center + viewportRight * (vx * inv) + viewportUp * (vy * inv);
            }
        }

        return candidate;
    }
    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        if (!initialized || IsFinished || target == null || target.IsDead) return;
        // Every behaviour below runs off elapsed time, so scaling the step slows all of it at once.
        float step = Time.deltaTime * SpeedMultiplier;
        slowRemaining = Mathf.Max(0f, slowRemaining - Time.deltaTime);
        if (entering)
        {
            entryElapsed += step;
            float progress = Mathf.Clamp01(entryElapsed / enemy.Data.EntryDuration);
            float u = 1f - Mathf.Pow(1f - progress, 3f);
            transform.position = (1f - u) * (1f - u) * entryStart + 2f * (1f - u) * u * control + u * u * destination;
            if (progress >= 1f) entering = false;
            return;
        }
        elapsed += step;
        if (enemy.Data.Movement == EnemyData.Behavior.Ranged)
        {
            if (elapsed >= nextShot)
            {
                nextShot = elapsed + enemy.Data.ShotInterval;
                var projectile = projectilePool != null ? projectilePool.Acquire(enemy.Data.ProjectilePrefab, projectileParent, transform.position, transform.rotation) : Instantiate(enemy.Data.ProjectilePrefab, transform.position, transform.rotation, projectileParent);
                projectile.Initialize(target, enemy.Data.AttackPoint);
                projectile.gameObject.SetActive(true);
            }
            return;
        }
        float pause = Mathf.Max(0f, enemy.Data.ApproachDuration - 0.5f);
        float t = enemy.Data.Movement == EnemyData.Behavior.Charged
            ? Mathf.Clamp01((elapsed - pause) / 0.5f)
            : Mathf.Clamp01(elapsed / enemy.Data.ApproachDuration);
        if (viewportRight != Vector3.zero)
        {
            if (enemy.Data.Movement == EnemyData.Behavior.Normal)
            {
                transform.position = EvaluateSpiralPosition(
                    destination,
                    hasViewportCenter ? viewportCenter : destination,
                    viewportRight,
                    viewportUp,
                    elapsed,
                    t,
                    phase,
                    spinDirection,
                    enemy.Data.SpiralStartRadius,
                    enemy.Data.SpiralEndRadius,
                    enemy.Data.SpiralPeriod,
                    enemy.Data.SpiralCenterPull,
                    amplitudeMultiplier,
                    periodMultiplier,
                    enemy.Data.SpiralVerticalScale);
            }
            else if (enemy.Data.Movement != EnemyData.Behavior.Charged
                && enemy.Data.Movement != EnemyData.Behavior.ApproachOnly)
            {
                float angle = elapsed * Mathf.PI * 2f / (enemy.Data.SwayPeriod * periodMultiplier) + phase;
                float ramp = Mathf.Clamp01(elapsed / 0.2f);
                transform.position = destination + ramp * (viewportRight * (Mathf.Sin(angle) * enemy.Data.SwayAmplitude.x * amplitudeMultiplier) + viewportUp * (Mathf.Sin(angle * 1.3f) * enemy.Data.SwayAmplitude.y * amplitudeMultiplier));
            }
        }
        transform.localScale = originalScale * Mathf.Lerp(enemy.Data.InitialScale, enemy.Data.FinalScale, t * t);
        if (t < 1f) return;
        finished = true;
        target.TakeDamage(enemy.Data.AttackPoint);
        GameplayLease.Despawn(gameObject);
    }
}
