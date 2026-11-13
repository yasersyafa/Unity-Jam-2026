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

    [Header("Wave Alive Cap")]
    [Tooltip("Maximum alive enemies present at once at Wave 1 (increments by +1 at Wave 5, Wave 9, and Wave 10).")]
    [SerializeField, Min(1)] private int baseMaxAliveEnemies = 3;

    [Header("Wave Progression Pacing")]
    [Tooltip("Controls how strongly later waves favor shorter delays within [minSpawnDelay, maxSpawnDelay]. 1 = uniform; higher = higher chance of fast delays as waves advance.")]
    [SerializeField, Min(1f)] private float maxWaveDelayFastBias = 3.5f;

    [Header("Dynamic Asymmetry")]
    [Tooltip("Chance a group mirrors its formation slots horizontally to vary flank side.")]
    [SerializeField, Range(0f, 1f)] private float horizontalMirrorChance = 0.5f;
    [Tooltip("Random viewport offset applied to the whole group formation (clamped inside safe screen bounds).")]
    [SerializeField] private Vector2 groupViewportOffsetJitter = new Vector2(0.05f, 0.035f);
    [Tooltip("Subtle per-enemy viewport jitter added to formation slots.")]
    [SerializeField] private Vector2 slotJitter = new Vector2(0.015f, 0.015f);
    [Tooltip("When enabled, enemies in a group spawn in randomized slot order instead of fixed sequential order.")]
    [SerializeField] private bool randomizeSpawnOrder = true;
    [Tooltip("Disable when a GameStateManager starts the waves through Begin().")]
    [SerializeField] private bool startOnEnable = true;

    public event System.Action<int> WaveStarted;
    public event System.Action BossReady;
    public event System.Action<BossEncounter> BossSpawned;
    private bool runningWaves;
    private GameplayPrefabPool gameplayPool;
    /// <summary>Set before OnEnable to wait for an explicit <see cref="Begin"/>.</summary>
    public bool StartOnEnable { get => startOnEnable; set => startOnEnable = value; }
    private readonly List<EnemyApproach> spawned = new List<EnemyApproach>();
    private readonly List<int> spawnOrder = new List<int>();

    public int AliveEnemyCount
    {
        get
        {
            spawned.RemoveAll(e => e == null || !e.gameObject.activeSelf);
            int count = 0;
            for (int k = 0; k < spawned.Count; k++)
            {
                if (!spawned[k].IsFinished) count++;
            }
            return count;
        }
    }

    private void OnEnable() { if (startOnEnable) Begin(); }

    /// <summary>Starts the wave sequence once; ignored while it is already running.</summary>
    public void Begin()
    {
        if (!isActiveAndEnabled || runningWaves) return;
        gameplayPool ??= new GameplayPrefabPool(transform);
        runningWaves = true;
        StartCoroutine(Run());
    }

    public static int EvaluateMaxAliveEnemies(int waveIndex, int baseMaxAlive = 3, int overrideMaxAlive = 0)
    {
        if (overrideMaxAlive > 0) return overrideMaxAlive;
        int waveNumber = waveIndex + 1;
        int maxAlive = Mathf.Max(1, baseMaxAlive);
        if (waveNumber >= 5) maxAlive++;
        if (waveNumber >= 9) maxAlive++;
        if (waveNumber >= 10) maxAlive++;
        return maxAlive;
    }

    public static int ComputeBatchSize(int remainingInGroup, int aliveCount, int maxAlive, float pairedChance, float randomSample)
    {
        int availableSlots = maxAlive - aliveCount;
        if (remainingInGroup <= 0 || availableSlots <= 0) return 0;

        int cap = Mathf.Min(2, Mathf.Min(remainingInGroup, availableSlots));
        return (cap >= 2 && randomSample < pairedChance) ? 2 : 1;
    }

    public static float EvaluateSpawnDelay(float minDelay, float maxDelay, int waveIndex, int waveCount, float maxFastBias, float randomSample)
    {
        float safeMin = Mathf.Max(0.05f, minDelay);
        float safeMax = Mathf.Max(safeMin, maxDelay);
        float progress = waveCount > 1 ? Mathf.Clamp01((float)waveIndex / (waveCount - 1)) : 0f;
        float exponent = Mathf.Lerp(1f, Mathf.Max(1f, maxFastBias), progress);
        float u = Mathf.Clamp01(randomSample);
        float skewed = Mathf.Pow(u, exponent);
        return Mathf.Lerp(safeMin, safeMax, skewed);
    }

    public static Vector2 EvaluateCenterEntryViewport(Vector2 center, Vector2 randomOffset, float normalizedX, float normalizedY)
    {
        float rx = Mathf.Clamp(normalizedX, -1f, 1f);
        float ry = Mathf.Clamp(normalizedY, -1f, 1f);
        return center + new Vector2(Mathf.Abs(randomOffset.x) * rx, Mathf.Abs(randomOffset.y) * ry);
    }

    public static Vector2 TransformSlotViewport(Vector2 baseSlot, bool mirrorHorizontal, Vector2 groupOffset, Vector2 perSlotJitter)
    {
        float x = mirrorHorizontal ? 1f - baseSlot.x : baseSlot.x;
        float y = baseSlot.y;
        x = Mathf.Clamp(x + groupOffset.x + perSlotJitter.x, 0.18f, 0.82f);
        y = Mathf.Clamp(y + groupOffset.y + perSlotJitter.y, 0.38f, 0.74f);
        return new Vector2(x, y);
    }

    private static float ComputeBendDirection(Vector2 entryViewport, Vector2 targetViewport)
    {
        Vector2 chord = targetViewport - entryViewport;
        if (chord.sqrMagnitude < 0.0001f) return Random.value < 0.5f ? 1f : -1f;

        Vector2 perp = new Vector2(-chord.y, chord.x).normalized;
        Vector2 toCenter = new Vector2(0.5f, 0.52f) - (entryViewport + targetViewport) * 0.5f;
        float dot = Vector2.Dot(perp, toCenter);
        float inwardSign = Mathf.Abs(dot) < 0.05f
            ? (Random.value < 0.5f ? 1f : -1f)
            : (dot >= 0f ? 1f : -1f);
        float sign = Random.value < 0.65f ? inwardSign : -inwardSign;
        return sign * Random.Range(0.75f, 1.35f);
    }

    private void PrepareSpawnOrder(int count)
    {
        spawnOrder.Clear();
        for (int idx = 0; idx < count; idx++) spawnOrder.Add(idx);
        if (!randomizeSpawnOrder) return;
        for (int idx = count - 1; idx > 0; idx--)
        {
            int swap = Random.Range(0, idx + 1);
            (spawnOrder[idx], spawnOrder[swap]) = (spawnOrder[swap], spawnOrder[idx]);
        }
    }

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
            spawned.RemoveAll(e => e == null || !e.gameObject.activeSelf);
            var wave = waves[index];
            int maxAlive = EvaluateMaxAliveEnemies(index, baseMaxAliveEnemies, wave.maxAliveEnemies);
            WaveStarted?.Invoke(index + 1);
            foreach (var group in wave.groups)
            {
                var formation = group.formation;
                var plane = new Plane(enemyParent.forward, enemyParent.position);
                Vector3 origin = PointOnPlane(plane, Vector2.zero);
                Vector3 right = PointOnPlane(plane, Vector2.right) - origin;
                Vector3 up = PointOnPlane(plane, Vector2.up) - origin;
                Vector3 viewportCenter = origin + right * 0.5f + up * 0.5f;

                bool mirrorHorizontal = Random.value < horizontalMirrorChance;
                Vector2 groupOffset = new Vector2(
                    Random.Range(-Mathf.Abs(groupViewportOffsetJitter.x), Mathf.Abs(groupViewportOffsetJitter.x)),
                    Random.Range(-Mathf.Abs(groupViewportOffsetJitter.y), Mathf.Abs(groupViewportOffsetJitter.y))
                );
                float groupPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
                PrepareSpawnOrder(formation.Count);

                int i = 0;
                while (i < formation.Count)
                {
                    while (!player.IsDead && (Time.timeScale <= 0f || AliveEnemyCount >= maxAlive)) yield return null;
                    if (player.IsDead) break;

                    // Maksimal 2 musuh muncul bersamaan dan tidak melewati batas hidup wave
                    float chance = wave.pairedSpawnChance > 0f ? wave.pairedSpawnChance : defaultPairedSpawnChance;
                    int batchSize = ComputeBatchSize(formation.Count - i, AliveEnemyCount, maxAlive, chance, Random.value);

                    for (int b = 0; b < batchSize; b++)
                    {
                        int enemyIndex = spawnOrder[i + b];
                        Vector2 perSlotJitter = new Vector2(
                            Random.Range(-Mathf.Abs(slotJitter.x), Mathf.Abs(slotJitter.x)),
                            Random.Range(-Mathf.Abs(slotJitter.y), Mathf.Abs(slotJitter.y))
                        );
                        Vector2 slot = TransformSlotViewport(formation.GetSlot(enemyIndex), mirrorHorizontal, groupOffset, perSlotJitter);
                        Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(slot.x, slot.y, 0f));
                        if (!plane.Raycast(ray, out float distance))
                        {
                            Debug.LogError("Enemy spawn plane must face the camera.", this);
                            enabled = false;
                            yield break;
                        }

                        // Kemunculan selalu dari tengah arena seperti wave sebelumnya
                        Vector2 entry = EvaluateCenterEntryViewport(
                            entryCenter,
                            entryRandomOffset,
                            Random.Range(-1f, 1f),
                            Random.Range(-1f, 1f)
                        );
                        float phase = groupPhaseOffset + enemyIndex * 2.399963f;
                        Spawn(group.enemies[enemyIndex], plane, entry, slot, ray.GetPoint(distance), right, up, viewportCenter,
                            phase, wave.swayAmplitudeMultiplier, wave.swayPeriodMultiplier);
                    }

                    i += batchSize;

                    // Delay dalam range [minD, maxD], probabilitas cepat meningkat seiring naiknya wave
                    if (i < formation.Count)
                    {
                        float minD = wave.minSpawnDelay > 0f ? wave.minSpawnDelay : defaultMinSpawnDelay;
                        float maxD = wave.maxSpawnDelay >= minD ? wave.maxSpawnDelay : defaultMaxSpawnDelay;
                        float delay = EvaluateSpawnDelay(minD, maxD, index, waves.Length, maxWaveDelayFastBias, Random.value);
                        yield return new WaitForSeconds(delay);
                    }
                }

                while (!player.IsDead && AliveEnemyCount > 0) yield return null;
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
        BossSpawned?.Invoke(activeBoss);
    }

    private EnemyApproach Spawn(EnemyApproach prefab, Plane plane, Vector2 entry, Vector2 slot, Vector3 destination,
        Vector3 right, Vector3 up, Vector3 viewportCenter, float phase, float swayAmplitude, float swayPeriod)
    {
        Vector3 start = PointOnPlane(plane, entry);
        float bendDirection = ComputeBendDirection(entry, slot);
        float spinDirection = Random.value < 0.5f ? 1f : -1f;
        gameplayPool ??= new GameplayPrefabPool(transform);
        var enemy = gameplayPool.Acquire(prefab, enemyParent, start, enemyParent.rotation);
        if (!enemy.TryGetComponent<EnemyHealthBar>(out _)) enemy.gameObject.AddComponent<EnemyHealthBar>();
        enemy.SetProjectilePool(gameplayPool);
        enemy.SetProjectileParent(transform);
        enemy.ConfigureDifficulty(swayAmplitude, swayPeriod);
        enemy.Initialize(player, destination, bendDirection, phase, right, up, viewportCenter, spinDirection);
        enemy.gameObject.SetActive(true);
        spawned.Add(enemy);
        return enemy;
    }

    /// <summary>First enemy of the first wave; the default enemy for scripted encounters such as the tutorial.</summary>
    public EnemyApproach FirstWaveEnemy =>
        waves != null && waves.Length > 0 && waves[0] != null && waves[0].groups != null && waves[0].groups.Length > 0
        && waves[0].groups[0] != null && waves[0].groups[0].enemies != null && waves[0].groups[0].enemies.Length > 0
            ? waves[0].groups[0].enemies[0] : null;

    /// <summary>Projects a viewport point onto the enemy plane.</summary>
    public bool TryViewportToPlane(Vector2 viewport, out Vector3 point)
    {
        point = default;
        if (gameplayCamera == null || enemyParent == null) return false;
        var plane = new Plane(enemyParent.forward, enemyParent.position);
        Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(viewport.x, viewport.y, 0f));
        if (!plane.Raycast(ray, out float distance)) return false;
        point = ray.GetPoint(distance);
        return true;
    }

    public Transform EnemyParent => enemyParent;

    /// <summary>Spawns one enemy outside the wave sequence; it flies in from the arena centre to <paramref name="slot"/>.</summary>
    public EnemyApproach SpawnScripted(EnemyApproach prefab, Vector2 slot)
    {
        if (prefab == null || player == null || !TryViewportToPlane(slot, out Vector3 destination)) return null;
        var plane = new Plane(enemyParent.forward, enemyParent.position);
        Vector3 origin = PointOnPlane(plane, Vector2.zero);
        Vector3 right = PointOnPlane(plane, Vector2.right) - origin;
        Vector3 up = PointOnPlane(plane, Vector2.up) - origin;
        Vector2 entry = EvaluateCenterEntryViewport(entryCenter, entryRandomOffset, Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        return Spawn(prefab, plane, entry, slot, destination, right, up, origin + right * 0.5f + up * 0.5f,
            Random.Range(0f, Mathf.PI * 2f), 1f, 1f);
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
            if (enemy != null) { enemy.gameObject.SetActive(false); GameplayLease.Despawn(enemy.gameObject); }
        spawned.Clear();
        foreach (var projectile in GetComponentsInChildren<EnemyProjectile>())
        { projectile.gameObject.SetActive(false); GameplayLease.Despawn(projectile.gameObject); }
    }
    private void OnDisable() { StopAllCoroutines(); runningWaves = false; Cleanup(); gameplayPool?.Dispose(); gameplayPool = null; }
}
