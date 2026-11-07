using UnityEngine;

public sealed class BossAttackTarget : MonoBehaviour, IDamageable
{
    public void TakeDamage(float damage)
    {
        if (!(damage > 0f) || !isActiveAndEnabled) return;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
