using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public sealed class SlotGameplayFlow : MonoBehaviour
{
    [SerializeField] private SlotMachineView machine;
    [SerializeField] private SlotWeaponRewards rewards;
    [SerializeField] private ShootingController shooting;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private InputActionReference interact;
    [SerializeField, Min(0f)] private float slotCooldown = 8f;
    [Tooltip("Disable when a GameStateManager decides when the first slot opens.")]
    [SerializeField] private bool openOnStart = true;
    [SerializeField] private TextMeshProUGUI status;
    [Header("Weapon Duration UI")]
    [SerializeField] private Slider weaponDurationSlider;
    [SerializeField] private TextMeshProUGUI weaponDurationText;

    private InputAction input;
    private float remaining;
    private float previousTimeScale;
    private bool open;
    private bool awarded;
    private bool closing;
    private bool queuedSpin;
    private bool started;
    private float currentWeaponDuration;
    private float weaponRemainingDuration;
    private bool weaponActive;
    private TextMeshProUGUI lastStatusTarget, lastDurationTarget;
    private WeaponData lastStatusWeapon, lastDurationWeapon;
    private string lastMissingTiers;
    private SlotTier lastStatusTier;
    private double lastStatusWeaponTime = double.NaN, lastStatusCooldown = double.NaN;
    private double lastDurationTime = double.NaN, lastDurationMaximum = double.NaN;
    private bool lastOpen, lastDead, lastCooldownActive;

    private static double DisplayTime(float seconds) => System.Math.Round(seconds, 1, System.MidpointRounding.ToEven);

    public float SlotCooldown => slotCooldown;
    public float RemainingCooldown => remaining;
    public float CurrentWeaponDuration => currentWeaponDuration;
    public float WeaponRemainingDuration => weaponRemainingDuration;
    public bool IsOpen => open;
    /// <summary>When false the player cannot open the slot with the interact input.</summary>
    public bool ManualOpenAllowed { get; set; } = true;
    /// <summary>Set before Start to stop the slot from opening automatically.</summary>
    public bool OpenOnStart { get => openOnStart; set => openOnStart = value; }

    public void ConfigureDurationUI(Slider slider, TextMeshProUGUI text = null)
    {
        lastDurationTarget = null;
        weaponDurationSlider = slider;
        weaponDurationText = text;
        UpdateWeaponDurationUI();
    }

    public float GetEffectiveWeaponDuration(WeaponData weapon)
    {
        float configuredDuration = weapon != null ? weapon.UsageDuration : slotCooldown;
        return Mathf.Max(slotCooldown, configuredDuration);
    }

    private void OnEnable()
    {
        lastStatusTarget = lastDurationTarget = null;
        input = interact.action.Clone();
        input.Enable();
        rewards.SetShootingController(shooting);
        rewards.WeaponAwarded += Awarded;
        if (started && openOnStart) Open();
    }

    private void Start()
    {
        // Health initializes in Awake; this flow runs before other components in OnEnable.
        started = true;
        if (openOnStart) Open();
    }

    private void Awarded(WeaponData weapon)
    {
        awarded = true;
        machine.PresentReward(weapon);
        machine.SetSpinAllowed(false);
        currentWeaponDuration = GetEffectiveWeaponDuration(weapon);
        weaponRemainingDuration = currentWeaponDuration;
        weaponActive = true;
        UpdateWeaponDurationUI();
    }

    public void Open()
    {
        if (open || (player != null && player.IsDead)) return;
        previousTimeScale = HitStop.UnfrozenTimeScale;
        open = true;
        awarded = false;
        closing = false;
        queuedSpin = false;
        Time.timeScale = 0f;
        shooting.SetShootingAllowed(false);
        machine.SetPanelVisible(true);
        machine.SetSpinAllowed(string.IsNullOrEmpty(rewards.MissingTiers));
    }

    public void ForceRoll()
    {
        if (open || (player != null && player.IsDead)) return;
        Open();
        if (machine != null)
        {
            queuedSpin = true;
        }
    }

    private void Update()
    {
        if (open)
        {
            if (machine == null || !machine.isActiveAndEnabled || (player != null && player.IsDead))
            {
                Time.timeScale = previousTimeScale;
                open = queuedSpin = closing = false;
                if (machine != null) machine.HidePanelImmediate();
                if (shooting != null) shooting.SetShootingAllowed(player != null && !player.IsDead);
                return;
            }
            if (queuedSpin && !machine.IsTransitioning)
            {
                queuedSpin = false;
                machine.Spin();
            }
            if (awarded && !machine.IsPresenting && !closing)
            {
                closing = true;
                machine.SetPanelVisible(false);
            }
            if (closing && !machine.IsTransitioning)
            {
                Time.timeScale = previousTimeScale;
                open = false;
                remaining = slotCooldown;
                shooting.SetShootingAllowed(true);
            }
        }
        else
        {
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);

            if (weaponActive)
            {
                weaponRemainingDuration = Mathf.Max(0f, weaponRemainingDuration - Time.deltaTime);
                UpdateWeaponDurationUI();

                if (!player.IsDead && weaponRemainingDuration <= 0f)
                {
                    ForceRoll();
                }
            }

            if (ManualOpenAllowed && !player.IsDead && remaining <= 0f && input != null && input.WasPressedThisFrame())
            {
                Open();
            }
        }

        UpdateStatusUI();
    }

    private void UpdateStatusUI()
    {
        if (status == null) return;
        var weapon = rewards.CurrentWeapon;
        SlotTier tier = weapon != null ? weapon.Tier : default;
        string missing = rewards.MissingTiers;
        bool dead = player != null && player.IsDead;
        bool cooldownActive = remaining > 0f;
        double weaponTime = weapon != null ? DisplayTime(weaponRemainingDuration) : 0;
        double cooldown = !open && !dead && cooldownActive ? DisplayTime(remaining) : 0;
        if (lastStatusTarget == status && lastStatusWeapon == weapon && lastStatusTier == tier
            && lastMissingTiers == missing && lastOpen == open && lastDead == dead && lastCooldownActive == cooldownActive
            && lastStatusWeaponTime == weaponTime && lastStatusCooldown == cooldown) return;
        lastStatusTarget = status; lastStatusWeapon = weapon; lastStatusTier = tier;
        lastMissingTiers = missing; lastOpen = open; lastDead = dead; lastCooldownActive = cooldownActive;
        lastStatusWeaponTime = weaponTime; lastStatusCooldown = cooldown;
        string weaponInfo = weapon != null ? $"{weapon.name} | {tier} | Durasi: {weaponRemainingDuration:0.0}s\n" : "";
        status.text = weaponInfo
            + (open ? (string.IsNullOrEmpty(missing) ? "Spin untuk lanjut" : "Isi weapon: " + missing)
            : dead ? "Player kalah" : cooldownActive ? $"Slot: {remaining:0.0}s" : "Interact: buka slot");
    }

    private void UpdateWeaponDurationUI()
    {
        if (weaponDurationSlider != null)
        {
            weaponDurationSlider.minValue = 0f;
            weaponDurationSlider.maxValue = currentWeaponDuration > 0f ? currentWeaponDuration : 1f;
            weaponDurationSlider.value = weaponRemainingDuration;
        }

        if (weaponDurationText != null)
        {
            var weapon = rewards != null ? rewards.CurrentWeapon : null;
            double time = DisplayTime(weaponRemainingDuration);
            double maximum = DisplayTime(currentWeaponDuration);
            if (lastDurationTarget == weaponDurationText && lastDurationWeapon == weapon
                && lastDurationTime == time && lastDurationMaximum == maximum) return;
            lastDurationTarget = weaponDurationText; lastDurationWeapon = weapon;
            lastDurationTime = time; lastDurationMaximum = maximum;
            string weaponName = weapon != null ? weapon.name : "Weapon";
            weaponDurationText.text = $"{weaponName}: {weaponRemainingDuration:0.0}s / {currentWeaponDuration:0.0}s";
        }
    }

    private void OnDisable()
    {
        rewards.WeaponAwarded -= Awarded;
        input?.Dispose();
        if (open)
        {
            Time.timeScale = previousTimeScale;
            if (shooting != null) shooting.SetShootingAllowed(true);
            if (machine != null) machine.HidePanelImmediate();
        }
        open = false;
        queuedSpin = closing = false;
    }
}
