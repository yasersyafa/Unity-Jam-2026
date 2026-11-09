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

    [Header("Normal Spiral Movement")]
    [Tooltip("Initial spiral orbit radius (viewport fraction) when starting the approach.")]
    [SerializeField, Min(0f)] private float spiralStartRadius = 0.06f;
    [Tooltip("Maximum spiral orbit radius (viewport width fraction) near the end of the approach (0.42 = 84% screen width).")]
    [SerializeField, Min(0f)] private float spiralEndRadius = 0.42f;
    [Tooltip("Vertical-to-horizontal viewport scale of the spiral orbit to fit screen bounds cleanly.")]
    [SerializeField, Range(0.2f, 1.2f)] private float spiralVerticalScale = 0.84f;
    [Tooltip("Seconds per full revolution during a spiral approach.")]
    [SerializeField, Min(0.1f)] private float spiralPeriod = 5.5f;
    [Tooltip("How strongly the spiral orbit center pulls toward screen center as it approaches (0 = orbit slot, 1 = converge to center).")]
    [SerializeField, Range(0f, 1f)] private float spiralCenterPull = 0.9f;

    public float EntryDuration => Mathf.Max(0.01f, entryDuration);
    public float EntryCurvature => entryCurvature;
    public Vector2 SwayAmplitude => swayAmplitude;
    public float SwayPeriod => Mathf.Max(0.1f, swayPeriod);
    public float SpiralStartRadius => Mathf.Max(0f, spiralStartRadius);
    public float SpiralEndRadius => Mathf.Max(0f, spiralEndRadius);
    public float SpiralVerticalScale => Mathf.Clamp(spiralVerticalScale, 0.2f, 1.2f);
    public float SpiralPeriod => Mathf.Max(0.1f, spiralPeriod);
    public float SpiralCenterPull => Mathf.Clamp01(spiralCenterPull);
    public float ApproachDuration => Mathf.Max(0.1f, approachDuration);
    public float InitialScale => initialScale;
    public float FinalScale => finalScale;
    public float HealthPoint => Mathf.Max(1f, healthPoint);
    public float AttackPoint => Mathf.Max(0f, attackPoint);
}
