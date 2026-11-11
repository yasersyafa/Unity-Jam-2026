using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the gameplay session phases: Intro → (Tutorial on first play) → WeaponSelect → Combat → Boss → Victory, or GameOver
/// whenever the player dies. To add a phase, create a <see cref="GameState"/> subclass and add it
/// to <see cref="CreateStates"/>.
/// </summary>
[DefaultExecutionOrder(-300)] // Awake must run before the slot flow's Start and the spawner's OnEnable.
public sealed class GameStateManager : MonoBehaviour
{
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private SlotGameplayFlow slots;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private EnemySpawner spawner;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private PlayerHealth player;
    [Tooltip("Optional; found in the scene when empty.")]
    [SerializeField] private ShootingController shooting;
    [Tooltip("Optional; found in the scene when empty. Without one the tutorial is skipped.")]
    [SerializeField] private TutorialDirector tutorial;
    [Tooltip("Pause after the scene transition finishes before the first slot opens (unscaled seconds).")]
    [SerializeField, Min(0f)] private float introDelay = 0.4f;

    private readonly Dictionary<Type, GameState> states = new();
    private BossEncounter boss;

    public SlotGameplayFlow Slots => slots;
    public EnemySpawner Spawner => spawner;
    public PlayerHealth Player => player;
    public TutorialDirector Tutorial => tutorial;
    public float IntroDelay => introDelay;
    public GameState Current { get; private set; }
    /// <summary>Raised after every transition with (previous, next); previous is null for the first state.</summary>
    public event Action<GameState, GameState> StateChanged;

    private static IEnumerable<GameState> CreateStates()
    {
        yield return new IntroState();
        yield return new TutorialState();
        yield return new WeaponSelectState();
        yield return new CombatState();
        yield return new BossState();
        yield return new VictoryState();
        yield return new GameOverState();
    }

    private void Awake()
    {
        if (slots == null) slots = FindAnyObjectByType<SlotGameplayFlow>();
        if (spawner == null) spawner = FindAnyObjectByType<EnemySpawner>();
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (shooting == null) shooting = FindAnyObjectByType<ShootingController>();
        if (tutorial == null) tutorial = FindAnyObjectByType<TutorialDirector>();

        // The manager decides when these start instead of their own lifecycle callbacks.
        if (slots != null) slots.OpenOnStart = false;
        if (spawner != null) spawner.StartOnEnable = false;

        foreach (var state in CreateStates())
        {
            state.Bind(this);
            states.Add(state.GetType(), state);
        }
    }

    private void OnEnable()
    {
        if (player != null) player.Died += OnPlayerDied;
        if (spawner != null) spawner.BossSpawned += OnBossSpawned;
    }

    private void OnDisable()
    {
        if (player != null) player.Died -= OnPlayerDied;
        if (spawner != null) spawner.BossSpawned -= OnBossSpawned;
        if (boss != null) boss.Defeated -= OnBossDefeated;
    }

    private void Start() => ChangeState<IntroState>();

    private void Update() => Current?.Tick();

    public bool IsIn<T>() where T : GameState => Current is T;

    public void ChangeState<T>() where T : GameState
    {
        if (!states.TryGetValue(typeof(T), out var next))
        {
            Debug.LogError($"GameStateManager: {typeof(T).Name} is not registered in CreateStates().", this);
            return;
        }
        if (Current == next) return;
        var previous = Current;
        previous?.Exit();
        Current = next;
        next.Enter();
        StateChanged?.Invoke(previous, next);
    }

    /// <summary>Enables or disables the player's shooting and their ability to open the slot manually.</summary>
    public void SetPlayerControl(bool shooting, bool manualSlot)
    {
        if (this.shooting != null) this.shooting.SetShootingAllowed(shooting);
        if (slots != null) slots.ManualOpenAllowed = manualSlot;
    }

    private void OnPlayerDied()
    {
        if (Current == null || !Current.IsTerminal) ChangeState<GameOverState>();
    }

    private void OnBossSpawned(BossEncounter encounter)
    {
        if (Current != null && Current.IsTerminal) return;
        if (boss != null) boss.Defeated -= OnBossDefeated;
        boss = encounter;
        if (boss != null) boss.Defeated += OnBossDefeated;
        ChangeState<BossState>();
    }

    private void OnBossDefeated()
    {
        if (Current == null || !Current.IsTerminal) ChangeState<VictoryState>();
    }
}
