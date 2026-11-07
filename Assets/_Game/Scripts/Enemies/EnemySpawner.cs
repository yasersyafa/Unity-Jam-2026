using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private EnemyApproach enemyPrefab;
    [SerializeField] private Transform enemyParent;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private EnemyFormation[] formations;
    [SerializeField, Min(0f)] private float waveDelay = 1f;
    [SerializeField] private Vector2 entryCenter = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 entryRandomOffset = new Vector2(0.08f, 0.05f);
    [SerializeField, Min(0f)] private float spawnInterval = 0.1f;
    [SerializeField] private EnemyWave[] waves;
    [SerializeField] private UnityEngine.Events.UnityEvent onBossReady;
    private readonly List<EnemyApproach> spawned = new List<EnemyApproach>();
    private void OnEnable() { StartCoroutine(Run()); }
    private IEnumerator Run()
    {
        yield return null;
        if (gameplayCamera == null || enemyPrefab == null || enemyPrefab.GetComponent<Enemy>().Data == null || enemyParent == null || player == null || formations == null || formations.Length == 0)
        {
            Debug.LogError("EnemySpawner requires camera, enemy prefab with data, parent, player and formations.", this);
            yield break;
        }
        bool campaign = waves != null && waves.Length > 0;
        if (campaign)
            foreach (var wave in waves)
                if (wave == null || wave.formation == null || wave.formation.Count == 0 || wave.enemies == null || wave.enemies.Length != wave.formation.Count || System.Array.Exists(wave.enemies, e => e == null || e.GetComponent<Enemy>() == null || e.GetComponent<Enemy>().Data == null))
                {
                    Debug.LogError("Invalid enemy wave configuration.", this);
                    yield break;
                }
        foreach (var formation in formations)
            if (formation == null || formation.Count == 0)
            {
                Debug.LogError("EnemySpawner requires nonempty formations.", this);
                yield break;
            }
        while (Time.timeScale <= 0f) yield return null;
        int index = 0;
        while (!player.IsDead)
        {
            spawned.RemoveAll(e => e == null);
            var wave = campaign ? waves[index] : null;
            var formation = campaign ? wave.formation : formations[index];
            var plane = new Plane(enemyParent.forward, enemyParent.position);
            Vector2 entry = entryCenter + new Vector2(Random.Range(-Mathf.Abs(entryRandomOffset.x), Mathf.Abs(entryRandomOffset.x)), Random.Range(-Mathf.Abs(entryRandomOffset.y), Mathf.Abs(entryRandomOffset.y)));
            Vector3 origin = PointOnPlane(plane, Vector2.zero);
            Vector3 right = PointOnPlane(plane, Vector2.right) - origin;
            Vector3 up = PointOnPlane(plane, Vector2.up) - origin;
            Vector3 start = PointOnPlane(plane, entry);
            for (int i = 0; i < formation.Count; i++)
            {
                while (Time.timeScale <= 0f) yield return null;
                if (player.IsDead) break;
                Vector2 slot = formation.GetSlot(i);
                Ray ray = gameplayCamera.ViewportPointToRay(new Vector3(slot.x, slot.y, 0f));
                if (!plane.Raycast(ray, out float distance))
                {
                    Debug.LogError("Enemy spawn plane must face the camera.", this);
                    enabled = false;
                    yield break;
                }
                var enemy = Instantiate(campaign ? wave.enemies[i] : enemyPrefab, start, enemyParent.rotation, enemyParent);
                if (campaign) enemy.ConfigureDifficulty(wave.approachDurationMultiplier, wave.swayAmplitudeMultiplier, wave.swayPeriodMultiplier);
                enemy.Initialize(player, ray.GetPoint(distance), i % 2 == 0 ? 1f : -1f, i * 2.399963f, right, up);
                spawned.Add(enemy);
                float interval = campaign ? wave.spawnInterval : spawnInterval;
                if (i + 1 < formation.Count && interval > 0f) yield return new WaitForSeconds(interval);
            }
            while (!player.IsDead && spawned.Exists(e => e != null && !e.IsFinished)) yield return null;
            if (player.IsDead) break;
            if (campaign && index == waves.Length - 1)
            {
                yield return new WaitForSeconds(2f);
                if (!player.IsDead) onBossReady?.Invoke();
                break;
            }
            yield return new WaitForSeconds(campaign ? wave.delay : waveDelay);
            index = (index + 1) % (campaign ? waves.Length : formations.Length);
        }
        Cleanup();
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
    }
    private void OnDisable() { StopAllCoroutines(); Cleanup(); }
}
