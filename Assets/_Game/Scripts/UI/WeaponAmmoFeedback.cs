using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Pulses the ammo HUD accent when ammo runs low or out. Unlimited weapons never pulse.</summary>
[DisallowMultipleComponent]
public sealed class WeaponAmmoFeedback : MonoBehaviour
{
    [SerializeField] private SlotGameplayFlow flow;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private CombatHudAnimationProfile profile;
    [SerializeField] private Image accent;
    private Tween pulse;
    private Color color;
    private bool critical;

    private void Awake()
    {
        if (accent != null) color = accent.color;
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (flow == null || player == null || profile == null || accent == null) return;

        // AmmoRatio is 1 for unlimited weapons, so only limited weapons running low (or empty) pulse.
        bool next = profile.AnimateCritical && !player.IsDead && flow.AmmoRatio <= profile.CriticalWeaponRatio;
        if (next == critical) return;
        critical = next;
        pulse.Stop();
        accent.color = color;
        if (next) pulse = Tween.Custom(0f, 1f, profile.PulsePeriod, t =>
        {
            Color value = color;
            value.a *= 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
            accent.color = value;
        }, cycles: -1, useUnscaledTime: true);
    }

    private void OnDisable()
    {
        pulse.Stop();
        critical = false;
        if (accent != null) accent.color = color;
    }
}
