using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runs the first-play tutorial inside GameScene. It only listens to existing gameplay events and unlocks player
/// controls step by step through <see cref="GameStateManager.SetPlayerControl"/>; it never changes the systems it teaches.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialDirector : MonoBehaviour
{
    private const string CompletedKey = "tutorial.completed";

    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private ShootingController shooting;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private SlotGameplayFlow slots;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private EnemySpawner spawner;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private StyleMeter style;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private PlayerHealth player;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private TutorialPresenter presenter;
    [Tooltip("Camera that renders the targets; Camera.main when empty.")]
    [SerializeField] private Camera gameplayCamera;
    [Tooltip("Enemy for the survive lesson; the first enemy of the first wave when empty.")]
    [SerializeField] private EnemyApproach scriptedEnemy;
    [SerializeField] private Vector2 scriptedEnemySlot = new(0.5f, 0.62f);
    [Tooltip("Target diameter as a fraction of screen height.")]
    [SerializeField, Range(0.02f, 0.3f)] private float targetSize = 0.1f;
    [Tooltip("Unscaled seconds without progress before the step hint appears.")]
    [SerializeField, Min(0.5f)] private float idleHintDelay = 4f;
    [Tooltip("Missed shots in a row before the coach teases the player.")]
    [SerializeField, Min(1)] private int teaseAfterMisses = 3;
    [Tooltip("Unscaled seconds Escape must be held to skip the tutorial.")]
    [SerializeField, Min(0.1f)] private float skipHoldDuration = 1f;
    [SerializeField] private string[] teases =
    {
        "BRO WHAT? Aim first, then shoot!",
        "COME ON! Put the crosshair on the target!",
        "Bullets aren't free, you know...",
    };

    private readonly List<TutorialTarget> targets = new();
    private List<TutorialStep> steps;
    private GameStateManager manager;
    private Enemy spawnedEnemy;
    private int index = -1;
    private float lastProgress;
    private bool hintShown;
    private float skipHeld;
    private int missStreak;
    private int teaseIndex;

    public static bool Completed => PlayerPrefs.GetInt(CompletedKey, 0) == 1;
    public bool IsRunning { get; private set; }
    public bool IsComplete { get; private set; }
    /// <summary>True once the player took a weapon from the slot during the tutorial.</summary>
    public bool WeaponGranted => WeaponsEquipped > 0;

    public ShootingController Shooting => shooting;
    public SlotGameplayFlow Slots => slots;
    public StyleMeter Style => style;
    public int WeaponsEquipped { get; private set; }
    public int HitCount { get; private set; }
    public int ActiveTargetCount
    {
        get
        {
            targets.RemoveAll(t => t == null || t.IsCleared);
            return targets.Count;
        }
    }
    public event Action PlayerDamaged;

    private static IEnumerable<TutorialStep> CreateSteps()
    {
        yield return new AimStep();
        yield return new ShootStep();
        yield return new SlotStep();
        yield return new StyleStep();
        yield return new SurviveStep();
        yield return new FinishStep();
    }

    private void Awake()
    {
        if (shooting == null) shooting = FindAnyObjectByType<ShootingController>();
        if (slots == null) slots = FindAnyObjectByType<SlotGameplayFlow>();
        if (spawner == null) spawner = FindAnyObjectByType<EnemySpawner>();
        if (style == null) style = FindAnyObjectByType<StyleMeter>();
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (presenter == null && !TryGetComponent(out presenter)) presenter = gameObject.AddComponent<TutorialPresenter>();
    }

    public void Begin(GameStateManager owner)
    {
        if (IsRunning) return;
        manager = owner;
        steps = new List<TutorialStep>(CreateSteps());
        foreach (var step in steps) step.Bind(this);
        IsRunning = true;
        IsComplete = false;
        WeaponsEquipped = HitCount = missStreak = 0;
        index = -1;
        Subscribe(true);
        presenter.Show(steps.Count);
        Next();
    }

    /// <summary>Called every frame by the tutorial game state.</summary>
    public void Tick()
    {
        if (!IsRunning) return;
        if (UpdateSkip()) return;
        var step = steps[index];
        if (!hintShown && Time.unscaledTime - lastProgress >= idleHintDelay)
        {
            hintShown = true;
            presenter.ShowHint(step.Hint);
        }
        if (step.Tick()) Next();
    }

    /// <summary>Ends the tutorial without marking it complete (e.g. the player died).</summary>
    public void Stop()
    {
        if (!IsRunning) return;
        if (index >= 0 && index < steps.Count) steps[index].Exit();
        Cleanup();
    }

    private void Next()
    {
        if (index >= 0) steps[index].Exit();
        index++;
        if (index >= steps.Count) { Finish(); return; }
        var step = steps[index];
        manager.SetPlayerControl(step.AllowShooting, step.AllowSlot);
        presenter.ShowStep(index, step.Title, step.Instruction);
        lastProgress = Time.unscaledTime;
        hintShown = false;
        missStreak = 0;
        step.Enter();
    }

    private void Finish()
    {
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
        Cleanup();
        IsComplete = true;
    }

    private void Cleanup()
    {
        Subscribe(false);
        // Ammo spent while learning should not count against the first real wave.
        if (shooting != null) shooting.RefillAmmo();
        IsRunning = false;
        foreach (var target in targets) if (target != null) Destroy(target.gameObject);
        targets.Clear();
        if (spawnedEnemy != null && !spawnedEnemy.IsDead) Destroy(spawnedEnemy.gameObject);
        spawnedEnemy = null;
        presenter.Hide();
    }

    private bool UpdateSkip()
    {
        bool holding = Keyboard.current != null && Keyboard.current.escapeKey.isPressed;
        skipHeld = holding ? skipHeld + Time.unscaledDeltaTime : 0f;
        presenter.SetSkipProgress(skipHeld / skipHoldDuration);
        if (skipHeld < skipHoldDuration) return false;
        skipHeld = 0f;
        steps[index].Exit();
        index = steps.Count;
        Finish();
        return true;
    }

    private void Subscribe(bool on)
    {
        if (shooting != null)
        {
            shooting.ShotResolved -= OnShotResolved;
            shooting.WeaponEquipped -= OnWeaponEquipped;
            if (on) { shooting.ShotResolved += OnShotResolved; shooting.WeaponEquipped += OnWeaponEquipped; }
        }
        if (player != null)
        {
            player.Damaged -= OnPlayerDamaged;
            if (on) player.Damaged += OnPlayerDamaged;
        }
    }

    private void OnShotResolved(bool hit)
    {
        if (hit)
        {
            HitCount++;
            missStreak = 0;
            lastProgress = Time.unscaledTime;
            return;
        }
        if (!steps[index].TeasesMisses || ++missStreak < teaseAfterMisses || teases.Length == 0) return;
        missStreak = 0;
        Coach(teases[teaseIndex++ % teases.Length]);
    }

    private void OnWeaponEquipped(WeaponData weapon) => WeaponsEquipped++;

    private void OnPlayerDamaged(float amount) => PlayerDamaged?.Invoke();

    // ---- API for steps ----

    /// <summary>Marks progress: resets the idle hint and pops a comic word.</summary>
    public void Progress(string word)
    {
        lastProgress = Time.unscaledTime;
        hintShown = false;
        presenter.HideHint();
        presenter.Pop(word);
    }

    public void Coach(string line) => presenter.ShowCoach(line);

    public TutorialTarget SpawnTarget(Vector2 viewport, bool shootable, Vector2 swingViewport = default, float swingSpeed = 0f)
    {
        if (spawner == null || !spawner.TryViewportToPlane(viewport, out Vector3 position)) return null;
        spawner.TryViewportToPlane(viewport + new Vector2(0f, targetSize), out Vector3 top);
        spawner.TryViewportToPlane(viewport + swingViewport, out Vector3 swingEnd);
        var template = scriptedEnemy != null ? scriptedEnemy : spawner.FirstWaveEnemy;
        int layer = template != null ? template.gameObject.layer : 0;
        var templateRenderer = template != null ? template.GetComponentInChildren<SpriteRenderer>(true) : null;
        int sortingLayer = templateRenderer != null ? templateRenderer.sortingLayerID : SortingLayer.NameToID("Enemy");
        var target = TutorialTarget.Create(spawner.EnemyParent, position, Vector3.Distance(position, top), layer,
            shootable, 1f, swingEnd - position, swingSpeed, sortingLayer);
        targets.Add(target);
        return target;
    }

    public bool IsAimOver(TutorialTarget target)
    {
        var view = gameplayCamera != null ? gameplayCamera : Camera.main;
        if (view == null || shooting == null || !shooting.HasAimPosition || target == null) return false;
        Vector2 screen = view.WorldToScreenPoint(target.transform.position);
        return Vector2.Distance(screen, shooting.AimScreen) <= targetSize * 0.5f * Screen.height;
    }

    public Enemy SpawnEnemy()
    {
        if (spawner == null) return null;
        var approach = spawner.SpawnScripted(scriptedEnemy != null ? scriptedEnemy : spawner.FirstWaveEnemy, scriptedEnemySlot);
        spawnedEnemy = approach != null ? approach.GetComponent<Enemy>() : null;
        return spawnedEnemy;
    }

    [ContextMenu("Reset Tutorial Progress")]
    private void ResetProgress()
    {
        PlayerPrefs.DeleteKey(CompletedKey);
        PlayerPrefs.Save();
    }
}
