using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public sealed class SlotGameplayFlow : MonoBehaviour
{
    private const string UnlimitedLabel = "Unlimited"; // plain text: the HUD fonts may not include an infinity glyph

    [SerializeField] private SlotMachineView machine;
    [SerializeField] private SlotWeaponRewards rewards;
    [SerializeField] private ShootingController shooting;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private InputActionReference interact;
    [SerializeField, Min(0f)] private float slotCooldown = 8f;
    [Tooltip("Disable when a GameStateManager decides when the first slot opens.")]
    [SerializeField] private bool openOnStart = true;
    [SerializeField] private TextMeshProUGUI status;
    [Header("Weapon Ammo UI")]
    [FormerlySerializedAs("weaponDurationSlider")]
    [SerializeField] private Slider ammoSlider;
    [SerializeField] private UnityEngine.UI.Image ammoFill;
    [Header("Ammo Fill Sprites by Rarity")]
    [SerializeField] private Sprite commonAmmoSprite;
    [SerializeField] private Sprite uncommonAmmoSprite;
    [SerializeField] private Sprite rareAmmoSprite;
    [SerializeField] private Sprite epicAmmoSprite;
    [SerializeField] private Sprite jackpotAmmoSprite;
    [FormerlySerializedAs("weaponDurationText")]
    [SerializeField] private TextMeshProUGUI ammoText;

    private InputAction input;
    private float remaining;
    private float previousTimeScale;
    private bool open;
    private bool awarded;
    private bool closing;
    private bool started;
    private TextMeshProUGUI lastStatusTarget, lastAmmoTarget;
    private WeaponData lastStatusWeapon, lastAmmoWeapon;
    private string lastMissingTiers;
    private SlotTier lastStatusTier;
    private double lastStatusCooldown = double.NaN;
    private int lastStatusAmmo = int.MinValue, lastAmmo = int.MinValue;
    private bool lastOpen, lastDead, lastCooldownActive, lastOutOfAmmo;

    private static double DisplayTime(float seconds) => System.Math.Round(seconds, 1, System.MidpointRounding.ToEven);

    public float SlotCooldown => slotCooldown;
    public float RemainingCooldown => remaining;
    public bool IsOpen => open;
    /// <summary>When false the player cannot open the slot with the interact input.</summary>
    public bool ManualOpenAllowed { get; set; } = true;
    /// <summary>Set before Start to stop the slot from opening automatically.</summary>
    public bool OpenOnStart { get => openOnStart; set => openOnStart = value; }

    private WeaponData CurrentWeapon => shooting != null ? shooting.CurrentWeapon : rewards != null ? rewards.CurrentWeapon : null;
    private bool HasLimitedAmmo => shooting != null && shooting.CurrentWeapon != null && !shooting.CurrentWeapon.UnlimitedAmmo;
    /// <summary>True when the current weapon is out of ammo, so the player must open the slot to attack again.</summary>
    public bool IsOutOfAmmo => shooting != null && !shooting.HasAmmo;
    /// <summary>Remaining ammo as 0..1; 1 for unlimited weapons or when no weapon is equipped.</summary>
    public float AmmoRatio => HasLimitedAmmo ? Mathf.Clamp01(shooting.Ammo / (float)shooting.CurrentWeapon.MaxAmmo) : 1f;

    public void ConfigureAmmoUI(Slider slider, TextMeshProUGUI text = null)
    {
        lastAmmoTarget = null;
        ammoSlider = slider;
        ammoText = text;
        UpdateAmmoUI();
    }

    private void OnEnable()
    {
        lastStatusTarget = lastAmmoTarget = null;
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
        UpdateAmmoUI();
    }

    public void Open()
    {
        if (open || (player != null && player.IsDead)) return;
        previousTimeScale = HitStop.UnfrozenTimeScale;
        open = true;
        awarded = false;
        closing = false;
        Time.timeScale = 0f;
        shooting.SetShootingAllowed(false);
        machine.SetPanelVisible(true);
        machine.SetSpinAllowed(string.IsNullOrEmpty(rewards.MissingTiers));
    }

    private void Update()
    {
        if (open)
        {
            if (machine == null || !machine.isActiveAndEnabled || (player != null && player.IsDead))
            {
                Time.timeScale = previousTimeScale;
                open = closing = false;
                if (machine != null) machine.HidePanelImmediate();
                if (shooting != null) shooting.SetShootingAllowed(player != null && !player.IsDead);
                return;
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
            // Running out of ammo never opens the slot by itself; the player has to choose to spin.
            if (ManualOpenAllowed && !player.IsDead && remaining <= 0f && input != null && input.WasPressedThisFrame())
            {
                Open();
            }
        }

        UpdateAmmoUI();
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
        bool outOfAmmo = IsOutOfAmmo;
        int ammo = HasLimitedAmmo ? shooting.Ammo : -1;
        double cooldown = !open && !dead && cooldownActive ? DisplayTime(remaining) : 0;
        if (lastStatusTarget == status && lastStatusWeapon == weapon && lastStatusTier == tier
            && lastMissingTiers == missing && lastOpen == open && lastDead == dead && lastCooldownActive == cooldownActive
            && lastOutOfAmmo == outOfAmmo && lastStatusAmmo == ammo && lastStatusCooldown == cooldown) return;
        lastStatusTarget = status; lastStatusWeapon = weapon; lastStatusTier = tier;
        lastMissingTiers = missing; lastOpen = open; lastDead = dead; lastCooldownActive = cooldownActive;
        lastOutOfAmmo = outOfAmmo; lastStatusAmmo = ammo; lastStatusCooldown = cooldown;
        string weaponInfo = weapon != null
            ? $"{weapon.name} | {tier} | Ammo: {(ammo < 0 ? UnlimitedLabel : $"{ammo}/{weapon.MaxAmmo}")}\n" : "";
        string empty = !open && !dead && outOfAmmo ? "Ammo habis! " : "";
        status.text = weaponInfo + empty
            + (open ? (string.IsNullOrEmpty(missing) ? "Spin untuk lanjut" : "Isi weapon: " + missing)
            : dead ? "Player kalah" : cooldownActive ? $"Slot: {remaining:0.0}s" : "Interact: buka slot");
    }

    private void UpdateAmmoUI()
    {
        var weapon = CurrentWeapon;
        bool limited = HasLimitedAmmo;
        int ammo = limited ? shooting.Ammo : -1;
        if (ammoFill != null)
        {
            Sprite raritySprite = (weapon != null ? weapon.Tier : SlotTier.Common) switch
            {
                SlotTier.Uncommon => uncommonAmmoSprite,
                SlotTier.Rare => rareAmmoSprite,
                SlotTier.Epic => epicAmmoSprite,
                SlotTier.Jackpot => jackpotAmmoSprite,
                _ => commonAmmoSprite
            };
            if (raritySprite == null) raritySprite = commonAmmoSprite;
            if (raritySprite != null && ammoFill.sprite != raritySprite)
                ammoFill.sprite = raritySprite;
            ammoFill.fillAmount = AmmoRatio;
        }
        else if (ammoSlider != null)
        {
            ammoSlider.minValue = 0f;
            ammoSlider.maxValue = limited ? weapon.MaxAmmo : 1f;
            ammoSlider.value = limited ? ammo : 1f;
        }

        if (ammoText != null)
        {
            if (lastAmmoTarget == ammoText && lastAmmoWeapon == weapon && lastAmmo == ammo) return;
            lastAmmoTarget = ammoText; lastAmmoWeapon = weapon; lastAmmo = ammo;
            string weaponName = weapon != null ? weapon.name : "Weapon";
            ammoText.text = ammoFill != null
                ? (limited ? $"{ammo}/{weapon.MaxAmmo}" : UnlimitedLabel)
                : (limited ? $"{weaponName}: {ammo} / {weapon.MaxAmmo}" : $"{weaponName}: {UnlimitedLabel}");
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
        closing = false;
    }
}
