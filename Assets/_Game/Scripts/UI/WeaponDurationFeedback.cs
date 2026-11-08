using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WeaponDurationFeedback : MonoBehaviour
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
        if (flow == null) flow = FindFirstObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (flow == null) flow = FindFirstObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
        if (flow == null || player == null || profile == null || accent == null) return;

        bool next = profile.AnimateCritical && !player.IsDead && flow.CurrentWeaponDuration > 0f
            && flow.WeaponRemainingDuration > 0f && flow.WeaponRemainingDuration / flow.CurrentWeaponDuration <= profile.CriticalWeaponRatio;
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
