using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Wave")]
public sealed class EnemyWave : ScriptableObject
{
    public EnemyFormation formation;
    public EnemyApproach[] enemies;
    [Min(0f)] public float spawnInterval = 0.1f;
    [Min(0f)] public float delay = 1f;
    [Min(0.1f)] public float approachDurationMultiplier = 1f;
    [Min(0f)] public float swayAmplitudeMultiplier = 1f;
    [Min(0.1f)] public float swayPeriodMultiplier = 1f;
}
