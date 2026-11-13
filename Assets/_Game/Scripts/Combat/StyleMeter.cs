using System;
using UnityEngine;

/// <summary>
/// Style meter: every shot that damages a target fills the current rank's gauge and full gauges promote the rank.
/// The gauge drains after a short idle delay (faster at higher ranks), misses cut a slice of it, and taking damage
/// knocks the rank down. Ranks fall one at a time; the combo only resets when the lowest rank empties.
/// </summary>
[DisallowMultipleComponent]
public sealed class StyleMeter : MonoBehaviour
{
    [Serializable]
    public struct Rank
    {
        public string label;
        public string text;
        [Tooltip("Hits needed to reach this rank from the one below it.")]
        [Min(0.01f)] public float hitsToFill;
        [Tooltip("Hits worth of gauge drained per second while idle at this rank.")]
        [Min(0f)] public float decayPerSecond;

        public Rank(string label, string text, float hitsToFill, float decayPerSecond)
        {
            this.label = label;
            this.text = text;
            this.hitsToFill = hitsToFill;
            this.decayPerSecond = decayPerSecond;
        }
    }

    [SerializeField] private ShootingController shooting;
    [SerializeField] private PlayerHealth player;
    [Tooltip("Lowest to highest.")]
    [SerializeField] private Rank[] ranks =
    {
        new Rank("C", "COME ON!", 1f, 0.3f),
        new Rank("B", "BRO WHAT?", 4f, 0.5f),
        new Rank("A", "ALL U GOT?", 5f, 0.8f),
        new Rank("S", "SMALL COMBO!", 10f, 1.2f),
        new Rank("SS", "SOME SHOOTING!!", 15f, 1.8f),
        new Rank("SSS", "SURE SO SATISFYING!!!", 15f, 2.5f),
    };
    [Tooltip("Seconds after the last hit before the gauge starts draining.")]
    [SerializeField, Min(0f)] private float decayDelay = 1.5f;
    [Tooltip("Fraction of the current gauge a missed shot removes.")]
    [SerializeField, Range(0f, 1f)] private float missPenalty = 0.35f;
    [Tooltip("Ranks lost when the player takes damage.")]
    [SerializeField, Min(0)] private int damageRankDrop = 2;

    private int rankIndex = -1;
    private float gauge;
    private float lastHitTime;

    /// <summary>Raised whenever the rank, gauge or hit count changes.</summary>
    public event Action Changed;

    /// <summary>-1 while unranked.</summary>
    public int RankIndex => rankIndex;
    public bool HasRank => rankIndex >= 0;
    public Rank CurrentRank => HasRank ? ranks[rankIndex] : default;
    public int HitCount { get; private set; }
    public float Gauge01 => HasRank ? Mathf.Clamp01(gauge / Required(rankIndex)) : 0f;

    private int TopRank => ranks.Length - 1;

    private void OnEnable()
    {
        if (ranks == null || ranks.Length == 0)
        {
            Debug.LogError("StyleMeter needs at least one rank.", this);
            enabled = false;
            return;
        }
        if (shooting != null) shooting.ShotResolved += OnShotResolved;
        if (player != null) player.Damaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        if (shooting != null) shooting.ShotResolved -= OnShotResolved;
        if (player != null) player.Damaged -= OnPlayerDamaged;
    }

    private void Update()
    {
        if (!HasRank || Time.time - lastHitTime < decayDelay) return;
        Drain(ranks[rankIndex].decayPerSecond * Time.deltaTime);
        Changed?.Invoke();
    }

    private void OnShotResolved(bool hit)
    {
        if (hit) RegisterHit();
        else if (HasRank)
        {
            Drain(missPenalty * Required(rankIndex));
            Log($"Miss | {Describe()}");
        }
        Changed?.Invoke();
    }

    private void RegisterHit()
    {
        HitCount++;
        lastHitTime = Time.time;
        gauge += 1f;
        while (rankIndex < TopRank && gauge >= Required(rankIndex))
        {
            gauge -= Required(rankIndex);
            rankIndex++;
            Log($"Rank UP → {RankName()}");
        }
        if (rankIndex == TopRank) gauge = Mathf.Min(gauge, Required(rankIndex));
        Log($"Hit | {Describe()}");
    }

    private void OnPlayerDamaged(float amount)
    {
        if (!HasRank) return;
        rankIndex = Mathf.Max(-1, rankIndex - damageRankDrop);
        gauge = 0f;
        if (HasRank) Log($"Damaged, rank DOWN → {RankName()}");
        else ResetCombo();
        Changed?.Invoke();
    }

    /// <summary>Removes gauge, falling through ranks with the remainder; emptying the lowest rank resets the combo.</summary>
    private void Drain(float amount)
    {
        gauge -= amount;
        while (gauge < 0f && HasRank)
        {
            rankIndex--;
            if (!HasRank) { ResetCombo(); return; }
            gauge += Required(rankIndex);
            Log($"Rank DOWN → {RankName()}");
        }
    }

    private void ResetCombo()
    {
        Log($"Combo reset (was {HitCount})");
        rankIndex = -1;
        gauge = 0f;
        HitCount = 0;
    }

    /// <summary>Gauge capacity while sitting at <paramref name="index"/>: the next rank's cost, or the top rank's own.</summary>
    private float Required(int index) => ranks[Mathf.Min(index + 1, TopRank)].hitsToFill;

    private string RankName() => HasRank ? $"{ranks[rankIndex].label} - {ranks[rankIndex].text}" : "-";

    private string Describe() => $"Combo {HitCount} | {RankName()} | {Mathf.RoundToInt(Gauge01 * 100f)}%";

    private void Log(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"[StyleMeter] {message}", this);
#endif
    }
}
