using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private InputAction input;
    private float remaining;
    private float previousTimeScale;
    private bool open;
    private bool awarded;
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
    }
    private void Open()
    {
        previousTimeScale = Time.timeScale;
        open = true;
        awarded = false;
        Time.timeScale = 0f;
        shooting.SetShootingAllowed(false);
        machine.SetPanelVisible(true);
        machine.SetSpinAllowed(string.IsNullOrEmpty(rewards.MissingTiers));
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
            if (!player.IsDead && remaining <= 0f && input.WasPressedThisFrame()) Open();
        }
        var weapon = rewards.CurrentWeapon;
        status.text = (weapon != null ? weapon.name + " | " + weapon.Tier + "\n" : "")
            + (open ? (string.IsNullOrEmpty(rewards.MissingTiers) ? "Spin untuk lanjut" : "Isi weapon: " + rewards.MissingTiers)
            : player.IsDead ? "Player kalah" : remaining > 0f ? $"Slot: {remaining:0.0}s" : "Interact: buka slot");
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
