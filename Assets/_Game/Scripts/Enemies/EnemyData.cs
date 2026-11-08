using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Data")]
public sealed class EnemyData : ScriptableObject
{
    public enum Behavior { Normal, Ranged, Tank, Charged }
    [SerializeField] private Behavior behavior;
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField, Min(0.1f)] private float shotInterval = 3f;
    public Behavior Movement => behavior;
    public EnemyProjectile ProjectilePrefab => projectilePrefab;
    public float ShotInterval => Mathf.Max(0.1f, shotInterval);
    [SerializeField, Min(1f)] private float healthPoint = 30f;
    [SerializeField, Min(0f)] private float attackPoint = 10f;
    [SerializeField, Min(0.1f)] private float approachDuration = 5f;
    [SerializeField, Min(0.01f)] private float initialScale = 0.4f;
    [SerializeField, Min(0.01f)] private float finalScale = 1.6f;
    [SerializeField, Min(0.01f)] private float entryDuration = 0.8f;
    [SerializeField, Min(0f)] private float entryCurvature = 0.15f;
    [SerializeField] private Vector2 swayAmplitude = new Vector2(0.012f, 0.008f);
    [SerializeField, Min(0.1f)] private float swayPeriod = 1.6f;
    public float EntryDuration => Mathf.Max(0.01f, entryDuration);
    public float EntryCurvature => entryCurvature;
    public Vector2 SwayAmplitude => swayAmplitude;
    public float SwayPeriod => Mathf.Max(0.1f, swayPeriod);
    public float ApproachDuration => Mathf.Max(0.1f, approachDuration);
    public float InitialScale => initialScale;
    public float FinalScale => finalScale;
    public float HealthPoint => Mathf.Max(1f, healthPoint);
    public float AttackPoint => Mathf.Max(0f, attackPoint);
}
