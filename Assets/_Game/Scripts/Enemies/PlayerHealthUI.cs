using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private Color normalColor = new Color32(0xB3, 0xFC, 0x2D, 0xFF);
    [SerializeField] private Color criticalColor = new Color32(0xFC, 0x30, 0x2D, 0xFF);
    [Header("Comic Presentation")]
    [SerializeField] private CombatHudAnimationProfile profile;
    [Tooltip("Use the same CombatFeedbackPreset as PlayerDamageFeedback to share its critical threshold.")]
    [SerializeField] private CombatFeedbackPreset damagePreset;
    [SerializeField] private Image damageTrail;
    [Tooltip("Dedicated visual parent containing the bar, trail and label.")]
    [SerializeField] private RectTransform impactRoot;
    [SerializeField] private Image criticalAccent;
    private PlayerHealth subscribedHealth;
    private Tween fillTween;
    private Tween trailTween;
    private Tween impactTween;
    private Tween criticalTween;
    private Vector2 position;
    private Color accentColor;
    private float target;
    private bool initialized;
    private bool critical;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponentInParent<PlayerHealth>() ?? FindAnyObjectByType<PlayerHealth>();
        if (impactRoot != null) position = impactRoot.anchoredPosition;
        if (criticalAccent != null) accentColor = criticalAccent.color;
    }
    public void Configure(PlayerHealth health, Image fill)
    {
        Unsubscribe();
        StopAnimations();
        playerHealth = health;
        healthFill = fill;
        initialized = false;
        if (isActiveAndEnabled) Subscribe();
        UpdateUI();
    }
    private void Subscribe()
    {
        if (subscribedHealth == playerHealth) return;
        Unsubscribe();
        subscribedHealth = playerHealth;
        if (subscribedHealth != null) subscribedHealth.HealthChanged += UpdateUI;
    }
    private void Unsubscribe()
    {
        if (subscribedHealth != null) subscribedHealth.HealthChanged -= UpdateUI;
        subscribedHealth = null;
    }
    private void Start() { initialized = false; UpdateUI(); }
    private void OnEnable() { Subscribe(); initialized = false; UpdateUI(); }
    private void OnDisable() { Unsubscribe(); StopAnimations(); initialized = false; }
    private void StopAnimations()
    {
        fillTween.Stop(); trailTween.Stop(); impactTween.Stop(); criticalTween.Stop();
        critical = false;
        if (impactRoot != null) impactRoot.anchoredPosition = position;
        if (criticalAccent != null) criticalAccent.color = accentColor;
    }
    public void UpdateUI()
    {
        if (playerHealth == null) return;
        float max = Mathf.Max(1f, playerHealth.MaximumHealth);
        float current = Mathf.Clamp(playerHealth.CurrentHealth, 0f, max);
        bool damaged = initialized && current < target;
        target = current;
        fillTween.Stop(); trailTween.Stop();
        bool animate = initialized && profile != null && profile.AnimateHealth && isActiveAndEnabled;
        float ratio = Mathf.Clamp01(current / max);
        float threshold = damagePreset != null ? damagePreset.criticalHealthRatio : profile != null ? profile.CriticalHealthRatio : 0.25f;
        if (healthFill != null)
        {
            healthFill.color = ratio <= threshold ? criticalColor : normalColor;
            if (animate) fillTween = Tween.Custom(healthFill.fillAmount, ratio, profile.FillDuration,
                value => healthFill.fillAmount = value, useUnscaledTime: true);
            else healthFill.fillAmount = ratio;
        }
        if (damageTrail != null)
        {
            if (profile != null) damageTrail.color = profile.TrailColor;
            if (animate && damaged)
                trailTween = Tween.Custom(Mathf.Max(damageTrail.fillAmount, current / max), current / max,
                    profile.TrailDuration, value => damageTrail.fillAmount = value,
                    startDelay: profile.TrailDelay, useUnscaledTime: true);
            else damageTrail.fillAmount = current / max;
        }
        if (animate && damaged) PlayImpact();
        SetCritical(!playerHealth.IsDead && current / max <= threshold);
        if (playerHealth.IsDead)
        {
            impactTween.Stop();
            if (impactRoot != null) impactRoot.anchoredPosition = position;
        }
        initialized = true;
    }
    private void PlayImpact()
    {
        impactTween.Stop();
        impactTween = Tween.Custom(0f, 1f, profile.HealthImpactDuration, t =>
        {
            if (impactRoot != null) impactRoot.anchoredPosition = position + Vector2.right * (Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * profile.HealthShakePixels);
        }, useUnscaledTime: true);
    }
    private void SetCritical(bool value)
    {
        value &= profile != null && profile.AnimateCritical && criticalAccent != null && isActiveAndEnabled;
        if (critical == value) return;
        critical = value;
        criticalTween.Stop();
        if (!value) { if (criticalAccent != null) criticalAccent.color = accentColor; return; }
        criticalTween = Tween.Custom(0f, 1f, profile.PulsePeriod, t =>
        {
            Color color = accentColor;
            color.a *= 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
            criticalAccent.color = color;
        }, cycles: -1, useUnscaledTime: true);
    }
    [ContextMenu("Preview Impact (Play Mode)")]
    private void Preview() { if (Application.isPlaying && profile != null && isActiveAndEnabled) PlayImpact(); }
}
