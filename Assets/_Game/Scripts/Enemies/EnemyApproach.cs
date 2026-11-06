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
    public bool IsFinished => enemy == null || enemy.IsDead || !isActiveAndEnabled || (initialized && elapsed >= enemy.Data.ApproachDuration);
    public void Initialize(PlayerHealth player)
    {
        enemy = GetComponent<Enemy>();
        target = player;
        originalScale = transform.localScale;
        elapsed = 0f;
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
        if (viewportRight != Vector3.zero)
        {
            float angle = elapsed * Mathf.PI * 2f / enemy.Data.SwayPeriod + phase;
            float ramp = Mathf.Clamp01(elapsed / 0.2f);
            transform.position = destination + ramp * (viewportRight * (Mathf.Sin(angle) * enemy.Data.SwayAmplitude.x) + viewportUp * (Mathf.Sin(angle * 1.3f) * enemy.Data.SwayAmplitude.y));
        }
        float t = Mathf.Clamp01(elapsed / enemy.Data.ApproachDuration);
        transform.localScale = originalScale * Mathf.Lerp(enemy.Data.InitialScale, enemy.Data.FinalScale, t * t);
        if (t < 1f) return;
        target.TakeDamage(enemy.Data.AttackPoint);
        Destroy(gameObject);
    }
}
