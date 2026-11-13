using UnityEngine;

public sealed class EnemyProjectile : MonoBehaviour, IDamageable
{
    [SerializeField, Min(0.1f)] private float flightDuration = 6f;
    private PlayerHealth player;
    private float damage, elapsed;
    private Vector3 baseScale;

    public void Initialize(PlayerHealth target, float amount)
    {
        elapsed = 0f;
        player = target;
        damage = amount;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (player == null || player.IsDead) { GameplayLease.Despawn(gameObject); return; }
        elapsed += Time.deltaTime;
        transform.localScale = baseScale * Mathf.Lerp(0.4f, 1.2f, Mathf.Clamp01(elapsed / flightDuration));
        if (elapsed < flightDuration) return;
        gameObject.SetActive(false);
        player.TakeDamage(damage);
        GameplayLease.Despawn(gameObject);
    }

    public void TakeDamage(float amount)
    {
        if (!(amount > 0f) || !isActiveAndEnabled) return;
        gameObject.SetActive(false);
        GameplayLease.Despawn(gameObject);
    }
}
