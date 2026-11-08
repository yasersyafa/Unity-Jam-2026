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
    [Min(0f)] public float spawnInterval = 0.1f;
    [Min(0f)] public float delay = 1f;
    [Min(0f)] public float swayAmplitudeMultiplier = 1f;
    [Min(0.1f)] public float swayPeriodMultiplier = 1f;
}
