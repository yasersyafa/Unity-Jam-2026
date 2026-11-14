using System;
using System.Collections;
using System.Collections.Generic;
using GabrielBigardi.SpriteAnimator;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BossEncounter : MonoBehaviour, IDamageable
{
    private static readonly Vector2 OrbitCenterViewport = new Vector2(0.5f, 0.55f);
    private static readonly Vector2 PlayerHitViewport = new Vector2(0.5f, 0.5f);

    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private BoxCollider2D hitbox;
    [SerializeField] private BossAttackTarget attackPrefab;
    [SerializeField] private SpriteAnimator spinnerAnimator;

    private BossConfig config;
    private Camera cameraReference;
    private Transform planeReference;
    private PlayerHealth player;
    private ShootingController shooting;
    private SlotGameplayFlow slots;
    private readonly List<BossAttackTarget> targets = new List<BossAttackTarget>();
    private float health;
    private bool running;
    private GameplayPrefabPool attackPool;
    private GameObject hud;
    private UnityEngine.UI.Image fill;
    private TextMeshProUGUI rouletteLabel;
    private Func<float> randomSource = () => UnityEngine.Random.value;

    public bool IsVulnerable { get; private set; }
    public bool IsSpinningRoulette { get; private set; }
    public float CurrentHealth => health;
    public float MaxHealth => config != null ? config.Health : 0f;
    public BossRouletteResult LastRouletteResult { get; private set; }
    public IReadOnlyList<BossAttackTarget> ActiveTargets => targets;

    public event Action Defeated;
    public event Action<BossRouletteResult> RouletteCompleted;

    /// <summary>Overrides the RNG source [0, 1) used by the roulette spin (for deterministic tests).</summary>
    public void SetRandomSource(Func<float> source)
    {
        randomSource = source ?? (() => UnityEngine.Random.value);
    }

    public void BeginEncounter(
        BossConfig data,
        Camera camera,
        Transform plane,
        PlayerHealth target,
        ShootingController gun,
        SlotGameplayFlow slotFlow)
    {
        if (running || config != null || data == null || target == null || target.IsDead) return;
        config = data;
        cameraReference = camera;
        planeReference = plane;
        player = target;
        shooting = gun;
        slots = slotFlow;
        health = config.Health;
        running = true;

        if (visual == null || hitbox == null || attackPrefab == null)
        {
            Debug.LogError("Boss prefab requires visual, hitbox and attack prefab references.", this);
            running = false;
            return;
        }

        ResolveSpinnerAnimator();
        SetVulnerable(false);

        attackPool = new GameplayPrefabPool(transform);
        int poolCount = Mathf.Max(config.RedProjectileCount, config.BlueProjectileCount);
        attackPool.Prewarm(attackPrefab, poolCount);
        CreateHud();
        player.Died += PlayerDied;
        StartCoroutine(AttackLoop());
    }

    private void ResolveSpinnerAnimator()
    {
        if (spinnerAnimator != null) return;
        var rootAnimator = GetComponent<SpriteAnimator>();
        foreach (var candidate in GetComponentsInChildren<SpriteAnimator>(true))
        {
            if (candidate != rootAnimator)
            {
                spinnerAnimator = candidate;
                return;
            }
        }
    }

    private IEnumerator AttackLoop()
    {
        bool nextIsRed = true;
        while (running)
        {
            // Boss always spins roulette first upon appearing and before every attack; invulnerable while spinning.
            IsSpinningRoulette = true;
            SetVulnerable(false);
            PlaySpinnerAnimation("Spin");
            if (config.BossLaughSound != null) AudioService.Ensure().Play(config.BossLaughSound);
            SetRouletteHud("ROULETTE SPIN... (KEBAL)", new Color(1f, 0.85f, 0.25f, 1f));

            float spinElapsed = 0f;
            while (running && spinElapsed < config.RouletteSpinDuration)
            {
                yield return null;
                spinElapsed += Time.deltaTime;
            }
            if (!running) yield break;

            var roulette = config.RollRoulette(randomSource(), randomSource());
            LastRouletteResult = roulette;
            IsSpinningRoulette = false;
            PlaySpinnerAnimation("Idle");
            SetRouletteHud(FormatRouletteStatus(roulette), roulette.IsBuff ? new Color(0.35f, 1f, 0.45f, 1f) : new Color(1f, 0.42f, 0.42f, 1f));
            RouletteCompleted?.Invoke(roulette);

            SpawnAttack(nextIsRed, roulette);
            // Boss opens its hitbox once the attack is released.
            SetVulnerable(true);

            while (running && targets.Exists(t => t != null && t.isActiveAndEnabled))
                yield return null;

            CleanupTargets();
            if (!running) yield break;

            nextIsRed = !nextIsRed;
            if (config.DelayBetweenAttacks > 0f)
                yield return new WaitForSeconds(config.DelayBetweenAttacks);
        }
    }

    private void SpawnAttack(bool isRed, in BossRouletteResult roulette)
    {
        CleanupTargets();
        int count = isRed ? config.RedProjectileCount : config.BlueProjectileCount;
        float period = isRed ? config.RedOrbitPeriod : config.BlueOrbitPeriod;
        float speedFactor = isRed ? roulette.RedSpinSpeedMultiplier : roulette.BlueSpeedMultiplier;
        float angularSpeed = (Mathf.PI * 2f / period) * speedFactor;

        Vector3 spawnPosition = planeReference != null ? planeReference.position : Vector3.zero;
        Quaternion spawnRotation = planeReference != null ? planeReference.rotation : Quaternion.identity;

        for (int i = 0; i < count; i++)
        {
            float initialAngle = i * Mathf.PI * 2f / count;
            var attack = attackPool.Acquire(attackPrefab, planeReference, spawnPosition, spawnRotation);
            var launch = isRed
                ? new BossProjectileLaunchData(
                    BossProjectileKind.Red,
                    config.RedProjectileSprite,
                    OrbitCenterViewport,
                    PlayerHitViewport,
                    config.RedOrbitRadius,
                    config.RedOrbitRadius,
                    initialAngle,
                    angularSpeed,
                    roulette.RedWaitDuration,
                    config.RedChargeDuration,
                    config.ProjectileScaleRange,
                    1f,
                    0f,
                    roulette.ProjectileDamage,
                    cameraReference,
                    planeReference,
                    player,
                    config.ProjectileHitSound)
                : new BossProjectileLaunchData(
                    BossProjectileKind.Blue,
                    config.BlueProjectileSprite,
                    OrbitCenterViewport,
                    PlayerHitViewport,
                    config.BlueOrbitStartRadius,
                    config.BlueOrbitEndRadius,
                    initialAngle,
                    angularSpeed,
                    0f,
                    roulette.BlueFlightDuration,
                    config.ProjectileScaleRange,
                    config.BlueVisibleDuration,
                    config.BlueInvisibleDuration,
                    roulette.ProjectileDamage,
                    cameraReference,
                    planeReference,
                    player,
                    config.ProjectileHitSound);

            attack.Launch(launch);
            attack.gameObject.SetActive(true);
            targets.Add(attack);
        }
    }

    private void PlaySpinnerAnimation(string animationName)
    {
        if (spinnerAnimator != null && spinnerAnimator.HasAnimation(animationName))
            spinnerAnimator.Play(animationName);
    }

    private void SetVulnerable(bool vulnerable)
    {
        IsVulnerable = vulnerable && running;
        if (hitbox != null) hitbox.enabled = IsVulnerable;
    }

    public void TakeDamage(float damage)
    {
        if (!running || !IsVulnerable || !(damage > 0f) || Time.timeScale <= 0f) return;
        health = Mathf.Max(0f, health - damage);
        if (config.BossHitSound != null) AudioService.Ensure().Play(config.BossHitSound);
        if (fill != null) fill.rectTransform.anchorMax = new Vector2(health / config.Health, 1f);
        if (health > 0f) return;

        running = false;
        IsSpinningRoulette = false;
        SetVulnerable(false);
        StopAllCoroutines();
        CleanupTargets();
        if (visual != null) visual.enabled = false;
        if (spinnerAnimator != null && spinnerAnimator.TryGetComponent<SpriteRenderer>(out var spinnerRenderer))
            spinnerRenderer.enabled = false;
        if (slots != null) slots.enabled = false;
        if (shooting != null) shooting.SetShootingAllowed(false);

        if (hud != null)
            hud.SetActive(false);
        Defeated?.Invoke();
    }

    private static string FormatRouletteStatus(in BossRouletteResult result)
    {
        switch (result.Effect)
        {
            case BossRouletteEffect.BuffDamage:
                return $"ROULETTE BUFF: DAMAGE +50% ({result.ProjectileDamage:0})";
            case BossRouletteEffect.BuffSpeed:
                return "ROULETTE BUFF: SPEED +20%";
            case BossRouletteEffect.DebuffDamage:
                return $"ROULETTE DEBUFF: DAMAGE -50% ({result.ProjectileDamage:0})";
            case BossRouletteEffect.DebuffSpeed:
                return "ROULETTE DEBUFF: SPEED -20%";
            default:
                return "ROULETTE";
        }
    }

    private void SetRouletteHud(string message, Color color)
    {
        if (rouletteLabel == null) return;
        rouletteLabel.text = message;
        rouletteLabel.color = color;
    }

    private void CreateHud()
    {
        hud = new GameObject("Boss HUD", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var back = Panel("Boss HP", hud.transform, new Vector2(0.25f, 0.92f), new Vector2(0.75f, 0.97f), Color.black);
        fill = Panel("Health", back.transform, Vector2.zero, Vector2.one, Color.red).GetComponent<UnityEngine.UI.Image>();
        Label("BOSS", back.transform, Vector2.zero, Vector2.one);
        rouletteLabel = Label("ROULETTE", hud.transform, new Vector2(0.25f, 0.875f), new Vector2(0.75f, 0.915f), 20f);
    }

    private static GameObject Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = name == "Restart";
        return obj;
    }

    private static TextMeshProUGUI Label(string value, Transform parent, Vector2 min, Vector2 max, float fontSize = 28f)
    {
        var obj = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        var rect = (RectTransform)obj.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private void CleanupTargets()
    {
        foreach (var target in targets)
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
                GameplayLease.Despawn(target.gameObject);
            }
        }
        targets.Clear();
    }

    private void PlayerDied()
    {
        running = false;
        IsSpinningRoulette = false;
        StopAllCoroutines();
        CleanupTargets();
        SetVulnerable(false);
        if (hud != null) hud.SetActive(false);
    }

    private void OnDisable()
    {
        PlayerDied();
        attackPool?.Dispose();
        attackPool = null;
        if (player != null) player.Died -= PlayerDied;
        if (hud != null) Destroy(hud);
    }
}
