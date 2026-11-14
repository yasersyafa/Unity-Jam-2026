using UnityEngine;

public enum BossRouletteEffect
{
    BuffDamage,
    BuffSpeed,
    DebuffDamage,
    DebuffSpeed
}

/// <summary>Evaluated outcome of a single boss roulette spin.</summary>
public readonly struct BossRouletteResult
{
    public readonly BossRouletteEffect Effect;
    public readonly float ProjectileDamage;
    public readonly float RedWaitDuration;
    public readonly float RedSpinSpeedMultiplier;
    public readonly float BlueFlightDuration;
    public readonly float BlueSpeedMultiplier;

    public bool IsBuff => Effect == BossRouletteEffect.BuffDamage || Effect == BossRouletteEffect.BuffSpeed;
    public bool AffectsDamage => Effect == BossRouletteEffect.BuffDamage || Effect == BossRouletteEffect.DebuffDamage;

    public BossRouletteResult(
        BossRouletteEffect effect,
        float projectileDamage,
        float redWaitDuration,
        float redSpinSpeedMultiplier,
        float blueFlightDuration,
        float blueSpeedMultiplier)
    {
        Effect = effect;
        ProjectileDamage = projectileDamage;
        RedWaitDuration = redWaitDuration;
        RedSpinSpeedMultiplier = redSpinSpeedMultiplier;
        BlueFlightDuration = blueFlightDuration;
        BlueSpeedMultiplier = blueSpeedMultiplier;
    }
}

[CreateAssetMenu(menuName = "Game/Boss Config")]
public sealed class BossConfig : ScriptableObject
{
    [Header("Boss Core")]
    [SerializeField] private BossEncounter bossPrefab;
    [SerializeField, Min(1f)] private float health = 2500f;
    [SerializeField, HideInInspector] private Sprite sprite;
    [SerializeField, Min(0f)] private float delayBetweenAttacks = 2f;

    [Header("Enemy Speed References")]
    [Tooltip("Reference to Charged enemy speed data (uses lunge duration when Red projectiles charge player).")]
    [SerializeField] private EnemyData chargedEnemyData;
    [Tooltip("Reference to Normal enemy data (uses approach duration and scale curve for Blue projectiles).")]
    [SerializeField] private EnemyData normalEnemyData;

    [Header("Projectile Visuals & Base Damage")]
    [SerializeField] private Sprite redProjectileSprite;
    [SerializeField] private Sprite blueProjectileSprite;
    [SerializeField, Min(0f)] private float baseProjectileDamage = 10f;

    [Header("Roulette Mechanic")]
    [SerializeField, Min(0.1f)] private float rouletteSpinDuration = 2f;
    [SerializeField, Range(0f, 1f)] private float buffChance = 0.5f;
    [Tooltip("Flat damage added per projectile when Damage Buff rolls; opposite percentage applies to Damage Debuff.")]
    [SerializeField, Min(0f)] private float damageBonusPerProjectile = 5f;
    [Tooltip("Fractional wait-time reduction on Red attack when Speed Buff rolls (0.2 -> 10s becomes 8s; debuff makes 12s).")]
    [SerializeField, Range(0f, 0.9f)] private float redWaitBuffReduction = 0.2f;
    [Tooltip("Fractional speed bonus on Blue attack when Speed Buff rolls (0.2 -> +20% speed; debuff gives -20% speed).")]
    [SerializeField, Range(0f, 0.9f)] private float blueSpeedBuffFraction = 0.2f;

    [Header("Red Attack (8 Orbiting Projectiles -> Charge)")]
    [SerializeField, Min(1)] private int redProjectileCount = 8;
    [SerializeField, Min(0.1f)] private float redOrbitDuration = 10f;
    [SerializeField] private Vector2 redOrbitRadius = new Vector2(0.24f, 0.20f);
    [SerializeField, Min(0.1f)] private float redOrbitPeriod = 3f;

    [Header("Blue Attack (4 Advancing Blinking Projectiles)")]
    [SerializeField, Min(1)] private int blueProjectileCount = 4;
    [SerializeField] private Vector2 blueOrbitStartRadius = new Vector2(0.14f, 0.12f);
    [SerializeField] private Vector2 blueOrbitEndRadius = new Vector2(0.28f, 0.23f);
    [SerializeField, Min(0.1f)] private float blueOrbitPeriod = 3.5f;
    [SerializeField, Min(0.05f)] private float blueVisibleDuration = 1.5f;
    [SerializeField, Min(0.05f)] private float blueInvisibleDuration = 1f;

    public BossEncounter BossPrefab => bossPrefab;
    public float Health => Mathf.Max(1f, health);
    public float DelayBetweenAttacks => Mathf.Max(0f, delayBetweenAttacks);
    public Sprite RedProjectileSprite => redProjectileSprite;
    public Sprite BlueProjectileSprite => blueProjectileSprite;
    public float BaseProjectileDamage => Mathf.Max(0f, baseProjectileDamage);
    public float RouletteSpinDuration => Mathf.Max(0.1f, rouletteSpinDuration);
    public float BuffChance => Mathf.Clamp01(buffChance);
    public float DamageBonusPerProjectile => Mathf.Max(0f, damageBonusPerProjectile);
    public float RedWaitBuffReduction => Mathf.Clamp(redWaitBuffReduction, 0f, 0.9f);
    public float BlueSpeedBuffFraction => Mathf.Clamp(blueSpeedBuffFraction, 0f, 0.9f);

    public int RedProjectileCount => Mathf.Max(1, redProjectileCount);
    public float RedOrbitDuration => Mathf.Max(0.1f, redOrbitDuration);
    public Vector2 RedOrbitRadius => redOrbitRadius;
    public float RedOrbitPeriod => Mathf.Max(0.1f, redOrbitPeriod);
    public float RedChargeDuration => chargedEnemyData != null
        ? Mathf.Min(EnemyApproach.ChargedLungeDuration, chargedEnemyData.ApproachDuration)
        : EnemyApproach.ChargedLungeDuration;

    public int BlueProjectileCount => Mathf.Max(1, blueProjectileCount);
    public Vector2 BlueOrbitStartRadius => blueOrbitStartRadius;
    public Vector2 BlueOrbitEndRadius => blueOrbitEndRadius;
    public float BlueOrbitPeriod => Mathf.Max(0.1f, blueOrbitPeriod);
    public float BlueVisibleDuration => Mathf.Max(0.05f, blueVisibleDuration);
    public float BlueInvisibleDuration => Mathf.Max(0.05f, blueInvisibleDuration);
    public float BlueBaseFlightDuration => normalEnemyData != null ? normalEnemyData.ApproachDuration : 8.5f;
    public Vector2 ProjectileScaleRange => normalEnemyData != null
        ? new Vector2(normalEnemyData.InitialScale, normalEnemyData.FinalScale)
        : new Vector2(0.4f, 1.6f);

    /// <summary>Rolls 50/50 Buff vs Debuff and selects Damage or Speed symmetrically.</summary>
    public BossRouletteResult RollRoulette(float outcomeRoll, float categoryRoll)
    {
        bool isBuff = outcomeRoll < BuffChance;
        bool affectsDamage = categoryRoll < 0.5f;
        BossRouletteEffect effect;
        if (isBuff)
            effect = affectsDamage ? BossRouletteEffect.BuffDamage : BossRouletteEffect.BuffSpeed;
        else
            effect = affectsDamage ? BossRouletteEffect.DebuffDamage : BossRouletteEffect.DebuffSpeed;
        return EvaluateRoulette(effect);
    }

    /// <summary>Computes exact projectile damage, wait timers, and speeds for a roulette result.</summary>
    public BossRouletteResult EvaluateRoulette(BossRouletteEffect effect)
    {
        float baseDmg = BaseProjectileDamage;
        float damageFraction = baseDmg > 0f ? DamageBonusPerProjectile / baseDmg : 0f;
        float damageMultiplier = 1f;
        float redWaitMultiplier = 1f;
        float redSpinSpeed = 1f;
        float blueSpeedMultiplier = 1f;

        switch (effect)
        {
            case BossRouletteEffect.BuffDamage:
                damageMultiplier = 1f + damageFraction;
                break;
            case BossRouletteEffect.DebuffDamage:
                damageMultiplier = Mathf.Max(0f, 1f - damageFraction);
                break;
            case BossRouletteEffect.BuffSpeed:
                redWaitMultiplier = 1f - RedWaitBuffReduction;
                redSpinSpeed = 1f + RedWaitBuffReduction;
                blueSpeedMultiplier = 1f + BlueSpeedBuffFraction;
                break;
            case BossRouletteEffect.DebuffSpeed:
                redWaitMultiplier = 1f + RedWaitBuffReduction;
                redSpinSpeed = Mathf.Max(0.1f, 1f - RedWaitBuffReduction);
                blueSpeedMultiplier = Mathf.Max(0.1f, 1f - BlueSpeedBuffFraction);
                break;
        }

        float finalDamage = Mathf.Max(0f, baseDmg * damageMultiplier);
        float finalRedWait = Mathf.Max(0.1f, RedOrbitDuration * redWaitMultiplier);
        float finalBlueDuration = BlueBaseFlightDuration / Mathf.Max(0.1f, blueSpeedMultiplier);

        return new BossRouletteResult(
            effect,
            finalDamage,
            finalRedWait,
            redSpinSpeed,
            finalBlueDuration,
            blueSpeedMultiplier);
    }
}
