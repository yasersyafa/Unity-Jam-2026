using UnityEngine;

[RequireComponent(typeof(Enemy))]
public sealed class EnemyApproach : MonoBehaviour
{
    private Enemy enemy;
    private PlayerHealth target;
    private Vector3 originalScale;
    private float elapsed;
    private bool initialized;
    private Vector3 entryStart, destination, control, viewportRight, viewportUp;
    private float entryElapsed, phase;
    private bool entering;
    private bool finished;
    private float nextShot;
    private Transform projectileParent;
    public void SetProjectileParent(Transform parent) { projectileParent = parent; }
    private float amplitudeMultiplier = 1f, periodMultiplier = 1f;
    public void ConfigureDifficulty(float amplitude, float period)
    {
        amplitudeMultiplier = Mathf.Max(0f, amplitude);
        periodMultiplier = Mathf.Max(0.1f, period);
    }
    public bool IsFinished => enemy == null || enemy.IsDead || !isActiveAndEnabled || finished;
    public void Initialize(PlayerHealth player)
    {
        enemy = GetComponent<Enemy>();
        target = player;
        originalScale = transform.localScale;
        elapsed = 0f;
        nextShot = enemy.Data.ShotInterval;
        finished = false;
        initialized = enemy.Data != null;
        if (initialized) transform.localScale = originalScale * enemy.Data.InitialScale;
    }
    public void Initialize(PlayerHealth player, Vector3 destination, float bendDirection, float phase, Vector3 viewportRight, Vector3 viewportUp)
    {
        Initialize(player);
        this.destination = destination;
        this.phase = phase;
        this.viewportRight = viewportRight;
        this.viewportUp = viewportUp;
        entryStart = transform.position;
        Vector3 delta = destination - entryStart;
        Vector3 perpendicular = Vector3.Cross(viewportRight, viewportUp).normalized;
        perpendicular = Vector3.Cross(perpendicular, delta).normalized;
        control = (entryStart + destination) * 0.5f + perpendicular * (delta.magnitude * enemy.Data.EntryCurvature * bendDirection);
        entryElapsed = 0f;
        entering = true;
    }
    private void Update()
    {
        if (Time.deltaTime <= 0f) return;
        if (!initialized || IsFinished || target == null || target.IsDead) return;
        if (entering)
        {
            entryElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(entryElapsed / enemy.Data.EntryDuration);
            float u = 1f - Mathf.Pow(1f - progress, 3f);
            transform.position = (1f - u) * (1f - u) * entryStart + 2f * (1f - u) * u * control + u * u * destination;
            if (progress >= 1f) entering = false;
            return;
        }
        elapsed += Time.deltaTime;
        if (enemy.Data.Movement == EnemyData.Behavior.Ranged)
        {
            if (elapsed >= nextShot)
            {
                nextShot = elapsed + enemy.Data.ShotInterval;
                var projectile = Instantiate(enemy.Data.ProjectilePrefab, transform.position, transform.rotation, projectileParent);
                projectile.Initialize(target, enemy.Data.AttackPoint);
            }
            return;
        }
        if (viewportRight != Vector3.zero && enemy.Data.Movement != EnemyData.Behavior.Charged)
        {
            float angle = elapsed * Mathf.PI * 2f / (enemy.Data.SwayPeriod * periodMultiplier) + phase;
            float ramp = Mathf.Clamp01(elapsed / 0.2f);
            transform.position = destination + ramp * (viewportRight * (Mathf.Sin(angle) * enemy.Data.SwayAmplitude.x * amplitudeMultiplier) + viewportUp * (Mathf.Sin(angle * 1.3f) * enemy.Data.SwayAmplitude.y * amplitudeMultiplier));
        }
        float pause = Mathf.Max(0f, enemy.Data.ApproachDuration - 0.5f);
        float t = enemy.Data.Movement == EnemyData.Behavior.Charged
            ? Mathf.Clamp01((elapsed - pause) / 0.5f)
            : Mathf.Clamp01(elapsed / enemy.Data.ApproachDuration);
        transform.localScale = originalScale * Mathf.Lerp(enemy.Data.InitialScale, enemy.Data.FinalScale, t * t);
        if (t < 1f) return;
        finished = true;
        target.TakeDamage(enemy.Data.AttackPoint);
        Destroy(gameObject);
    }
}
