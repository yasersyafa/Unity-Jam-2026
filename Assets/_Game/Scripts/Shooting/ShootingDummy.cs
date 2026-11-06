using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class ShootingDummy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("Damage required to defeat this practice target.")]
    [SerializeField, Min(1f)] private float maximumHealth = 30f;
    [Tooltip("Seconds before a defeated target becomes active again.")]
    [SerializeField, Min(0.1f)] private float respawnDelay = 1f;
    private float health;
    private float respawnAt;
    private SpriteRenderer visual;
    private BoxCollider2D hitbox;
    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        health = maximumHealth;
    }
    public void TakeDamage(float damage)
    {
        if (!hitbox.enabled) return;
        health -= Mathf.Max(0f, damage);
        visual.color = Color.red;
        if (health > 0f) return;
        visual.enabled = hitbox.enabled = false;
        respawnAt = Time.time + respawnDelay;
    }
    private void Update()
    {
        if (hitbox.enabled || Time.time < respawnAt) return;
        health = maximumHealth;
        visual.color = Color.white;
        visual.enabled = hitbox.enabled = true;
    }
}
