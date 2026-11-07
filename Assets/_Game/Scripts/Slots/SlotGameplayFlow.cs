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
    [SerializeField] private TextMeshProUGUI status;
    [Header("Weapon Duration UI")]
    [SerializeField] private Slider weaponDurationSlider;
    [SerializeField] private TextMeshProUGUI weaponDurationText;

    private InputAction input;
    private float remaining;
    private float previousTimeScale;
    private bool open;
    private bool awarded;
    private float currentWeaponDuration;
    private float weaponRemainingDuration;
    private bool weaponActive;

    public float SlotCooldown => slotCooldown;
    public float RemainingCooldown => remaining;
    public float CurrentWeaponDuration => currentWeaponDuration;
    public float WeaponRemainingDuration => weaponRemainingDuration;
    public bool IsOpen => open;

    public void ConfigureDurationUI(Slider slider, TextMeshProUGUI text = null)
    {
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
        input = interact.action.Clone();
        input.Enable();
        rewards.SetShootingController(shooting);
        rewards.WeaponAwarded += Awarded;
        Open();
    }

    private void Awarded(WeaponData weapon)
    {
        awarded = true;
        machine.SetSpinAllowed(false);
        currentWeaponDuration = GetEffectiveWeaponDuration(weapon);
        weaponRemainingDuration = currentWeaponDuration;
        weaponActive = true;
        UpdateWeaponDurationUI();
    }

    public void Open()
    {
        previousTimeScale = Time.timeScale;
        open = true;
        awarded = false;
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
            machine.Spin();
        }
    }

    private void Update()
    {
        if (open)
        {
            if (awarded && !machine.IsPresenting)
            {
                machine.SetPanelVisible(false);
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

            if (!player.IsDead && remaining <= 0f && input != null && input.WasPressedThisFrame())
            {
                Open();
            }
        }

        var weapon = rewards.CurrentWeapon;
        string weaponInfo = weapon != null
            ? $"{weapon.name} | {weapon.Tier} | Durasi: {weaponRemainingDuration:0.0}s\n"
            : "";

        status.text = weaponInfo
            + (open ? (string.IsNullOrEmpty(rewards.MissingTiers) ? "Spin untuk lanjut" : "Isi weapon: " + rewards.MissingTiers)
            : player.IsDead ? "Player kalah" : remaining > 0f ? $"Slot: {remaining:0.0}s" : "Interact: buka slot");
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
            if (machine != null) machine.SetPanelVisible(false);
        }
        open = false;
    }
}
