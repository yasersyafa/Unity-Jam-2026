using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public sealed class EnemyData : ScriptableObject
{
    [SerializeField, Min(1f)] private float healthPoint = 30f;
    [SerializeField, Min(0f)] private float attackPoint = 10f;
    public float HealthPoint => Mathf.Max(1f, healthPoint);
    public float AttackPoint => Mathf.Max(0f, attackPoint);
}
