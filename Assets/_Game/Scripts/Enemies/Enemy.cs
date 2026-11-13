using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemyFeedback feedback;
    public EnemyData Data => data;
    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public event Action Damaged;
    public event Action Died;
    private Collider2D hitbox;

    private void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        if (data == null)
        {
            Debug.LogError("Enemy requires EnemyData.", this);
            hitbox.enabled = false;
            enabled = false;
            return;
        }
        CurrentHealth = data.HealthPoint;
    }

    private void OnEnable()
    {
        if (data == null) return;
        CurrentHealth = data.HealthPoint;
        IsDead = false;
        if (hitbox == null) hitbox = GetComponent<Collider2D>();
        hitbox.enabled = true;
    }

    public void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || data == null || !(damage > 0f)) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        if (CurrentHealth > 0f)
        {
            if (feedback != null) feedback.PlayHit();
            Damaged?.Invoke();
            return;
        }
        IsDead = true;
        Damaged?.Invoke();
        hitbox.enabled = false;
        float delay = feedback != null ? feedback.PlayDeath() : 0f;
        Died?.Invoke();
        GameplayLease.Despawn(gameObject, delay);
    }
}
