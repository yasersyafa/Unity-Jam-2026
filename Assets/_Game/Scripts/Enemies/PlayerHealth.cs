using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Min(1f)] private float maximumHealth = 100f;
    public float CurrentHealth { get; private set; }
    public float MaximumHealth => maximumHealth;
    public bool IsDead => CurrentHealth <= 0f;
    public event Action HealthChanged;
    public event Action Died;
    private void Awake() { CurrentHealth = maximumHealth; }
    public void TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || IsDead || !(damage > 0f)) return;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        HealthChanged?.Invoke();
        if (IsDead) Died?.Invoke();
    }
}
