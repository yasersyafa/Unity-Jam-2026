using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Wave")]
public sealed class EnemyWave : ScriptableObject
{
    [System.Serializable]
    public sealed class Group
    {
        public EnemyFormation formation;
        public EnemyApproach[] enemies;
    }
    public Group[] groups;
    [Min(0f)] public float groupDelay = 0.75f;
    [Tooltip("Legacy interval; minSpawnDelay and maxSpawnDelay are used instead.")]
    [Min(0f)] public float spawnInterval = 0.1f;

    [Header("Spawn Pacing (Randomized Delay)")]
    [Tooltip("Minimum delay between spawns in seconds")]
    [Min(0.05f)] public float minSpawnDelay = 0.5f;

    [Tooltip("Maximum delay between spawns in seconds")]
    [Min(0.05f)] public float maxSpawnDelay = 1.5f;

    [Tooltip("Chance that up to 2 enemies spawn simultaneously (never more than 2)")]
    [Range(0f, 1f)] public float pairedSpawnChance = 0.3f;

    [Min(0f)] public float delay = 1f;
    [Min(0f)] public float swayAmplitudeMultiplier = 1f;
    [Min(0.1f)] public float swayPeriodMultiplier = 1f;
}
