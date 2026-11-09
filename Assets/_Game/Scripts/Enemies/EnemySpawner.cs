using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private Vector2 entryCenter = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 entryRandomOffset = new Vector2(0.08f, 0.05f);
    [SerializeField] private EnemyWave[] waves;
    [SerializeField] private UnityEngine.Events.UnityEvent onBossReady;
    [SerializeField] private BossConfig bossConfig;

    [Header("Spawn Pacing Defaults")]
    [Tooltip("Default minimum delay between enemy spawns (seconds).")]
    [SerializeField, Min(0.05f)] private float defaultMinSpawnDelay = 1.0f;
    [Tooltip("Default maximum delay between enemy spawns (seconds).")]
    [SerializeField, Min(0.05f)] private float defaultMaxSpawnDelay = 2.5f;
    [Tooltip("Default chance to spawn 2 enemies simultaneously (max 2).")]
    [SerializeField, Range(0f, 1f)] private float defaultPairedSpawnChance = 0.3f;

    public event System.Action<int> WaveStarted;
    public event System.Action BossReady;
    private readonly List<EnemyApproach> spawned = new List<EnemyApproach>();
    private void OnEnable() { StartCoroutine(Run()); }
    private IEnumerator Run()
    {
        yield return null;
        if (gameplayCamera == null || enemyParent == null || player == null || waves == null || waves.Length == 0)
        {
            Debug.LogError("EnemySpawner requires camera, parent, player and nonempty waves.", this);
            yield break;
        }
        foreach (var wave in waves)
        {
            if (wave == null || wave.groups == null || wave.groups.Length == 0) { Debug.LogError("Wave requires groups.", this); yield break; }
            foreach (var group in wave.groups)
                if (group == null || group.formation == null || group.enemies == null || group.formation.Count == 0 || group.enemies.Length != group.formation.Count || System.Array.Exists(group.enemies, e => e == null || e.GetComponent<Enemy>() == null || e.GetComponent<Enemy>().Data == null || (e.GetComponent<Enemy>().Data.Movement == EnemyData.Behavior.Ranged && e.GetComponent<Enemy>().Data.ProjectilePrefab == null)))
                { Debug.LogError("Invalid wave group.", this); yield break; }
        }
        while (Time.timeScale <= 0f) yield return null;
        int index = 0;
        while (!player.IsDead)
        {
            spawned.RemoveAll(e => e == null);
            var wave = waves[index];
            WaveStarted?.Invoke(index + 1);
            foreach (var group in wave.groups)
            {
                var formation = group.formation;
                var plane = new Plane(enemyParent.forward, enemyParent.position);
                Vector3 origin = PointOnPlane(plane, Vector2.zero);
                Vector3 right = PointOnPlane(plane, Vector2.right) - origin;
                Vector3 up = PointOnPlane(plane, Vector2.up) - origin;

                int i = 0;
                while (i < formation.Count)
                {
                    while (Time.timeScale <= 0f) yield return null;
                    if (player.IsDead) break;

                    // Maksimal 2 musuh muncul bersamaan
                    float chance = wave.pairedSpawnChance > 0f ? wave.pairedSpawnChance : defaultPairedSpawnChance;
                    int batchSize = (i + 1 < formation.Count && Random.value < chance) ? 2 : 1;

                    for (int b = 0; b < batchSize; b++)
                    {
                        int enemyIndex = i + b;
                        Vector2 slot = formation.GetSlot(enemyIndex);
                        Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(slot.x, slot.y, 0f));
                        if (!plane.Raycast(ray, out float distance))
                        {
                            Debug.LogError("Enemy spawn plane must face the camera.", this);
                            enabled = false;
                            yield break;
                        }

                        Vector2 entry = entryCenter + new Vector2(
                            Random.Range(-Mathf.Abs(entryRandomOffset.x), Mathf.Abs(entryRandomOffset.x)),
                            Random.Range(-Mathf.Abs(entryRandomOffset.y), Mathf.Abs(entryRandomOffset.y))
                        );
                        Vector3 start = PointOnPlane(plane, entry);

                        var enemy = Instantiate(group.enemies[enemyIndex], start, enemyParent.rotation, enemyParent);
                        if (!enemy.TryGetComponent<EnemyHealthBar>(out _)) enemy.gameObject.AddComponent<EnemyHealthBar>();
                        enemy.SetProjectileParent(transform);
                        enemy.ConfigureDifficulty(wave.swayAmplitudeMultiplier, wave.swayPeriodMultiplier);
                        enemy.Initialize(player, ray.GetPoint(distance), enemyIndex % 2 == 0 ? 1f : -1f, enemyIndex * 2.399963f, right, up);
                        spawned.Add(enemy);
                    }

                    i += batchSize;

                    // Delay tidak konstan antara 0.5s sampai 1.5s
                    if (i < formation.Count)
                    {
                        float minD = wave.minSpawnDelay > 0f ? wave.minSpawnDelay : defaultMinSpawnDelay;
                        float maxD = wave.maxSpawnDelay >= minD ? wave.maxSpawnDelay : defaultMaxSpawnDelay;
                        float delay = Random.Range(minD, maxD);
                        yield return new WaitForSeconds(delay);
                    }
                }

                while (!player.IsDead && spawned.Exists(e => e != null && !e.IsFinished)) yield return null;
                var projectiles = GetComponentsInChildren<EnemyProjectile>();
                while (!player.IsDead && System.Array.Exists(projectiles, p => p != null && p.isActiveAndEnabled)) yield return null;
                if (player.IsDead) break;
                if (group != wave.groups[wave.groups.Length - 1]) yield return new WaitForSeconds(wave.groupDelay);
            }
            if (player.IsDead) break;
            if (index == waves.Length - 1)
            {
                yield return new WaitForSeconds(2f);
                if (!player.IsDead)
                {
                    onBossReady?.Invoke();
                    BossReady?.Invoke();
                    SpawnBoss();
                }
                break;
            }
            yield return new WaitForSeconds(wave.delay);
            index++;
        }
        Cleanup();
    }
    private BossEncounter activeBoss;

    [ContextMenu("Test Boss Now (Play Mode)")]
    private void TestBossNow()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode before testing the boss.", this);
            return;
        }
        if (bossConfig == null || bossConfig.BossPrefab == null || gameplayCamera == null || enemyParent == null || player == null || player.IsDead)
        {
            Debug.LogWarning("Boss test requires config, camera, parent and a living player.", this);
            return;
        }
        StopAllCoroutines();
        Cleanup();
        SpawnBoss();
    }

    private void SpawnBoss()
    {
        if (bossConfig == null || bossConfig.BossPrefab == null || activeBoss != null) return;
        activeBoss = Instantiate(bossConfig.BossPrefab, transform);
        activeBoss.BeginEncounter(bossConfig, gameplayCamera, enemyParent, player,
            transform.root.GetComponentInChildren<ShootingController>(),
            transform.root.GetComponentInChildren<SlotGameplayFlow>());
    }

    private Vector3 PointOnPlane(Plane plane, Vector2 viewport)
    {
        Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        if (!plane.Raycast(ray, out float distance)) return enemyParent.position;
        return ray.GetPoint(distance);
    }
    private void Cleanup()
    {
        foreach (var enemy in spawned)
            if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
        spawned.Clear();
        foreach (var projectile in GetComponentsInChildren<EnemyProjectile>())
        { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
    }
    private void OnDisable() { StopAllCoroutines(); Cleanup(); }
}
