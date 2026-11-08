using UnityEngine;

public sealed class EnemyProjectile : MonoBehaviour, IDamageable
{
    private PlayerHealth player;
    private float damage, elapsed;
    private Vector3 baseScale;
    public void Initialize(PlayerHealth target, float amount) { player = target; damage = amount; baseScale = transform.localScale; }
    private void Update()
    {
        if (player == null || player.IsDead) { Destroy(gameObject); return; }
        elapsed += Time.deltaTime;
        transform.localScale = baseScale * Mathf.Lerp(0.4f, 1.2f, Mathf.Clamp01(elapsed / 1.5f));
        if (elapsed < 1.5f) return;
        gameObject.SetActive(false);
        player.TakeDamage(damage);
        Destroy(gameObject);
    }
    public void TakeDamage(float amount) { if (!(amount > 0f) || !isActiveAndEnabled) return; gameObject.SetActive(false); Destroy(gameObject); }
}
