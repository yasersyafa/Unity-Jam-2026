using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SlotRewardPresentation : MonoBehaviour
{
    [SerializeField] private SlotPresentationProfile profile;
    [SerializeField] private RectTransform visual;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;
    [Tooltip("Optional weapon used only by the Play Mode context-menu preview.")]
    [SerializeField] private WeaponData previewWeapon;
    private Tween animation;
    private Vector3 scale;
    private bool initialized;
    public bool IsPlaying => animation.isAlive;
    public void Present(WeaponData weapon)
    {
        Cancel();
        if (weapon == null || profile == null || !isActiveAndEnabled) return;
        if (!initialized) { scale = visual.localScale; initialized = true; }
        icon.sprite = weapon.Sprite;
        icon.preserveAspect = true;
        icon.enabled = weapon.Sprite != null;
        label.text = $"{weapon.name}\n{weapon.Tier}";
        label.color = profile.RarityColor(weapon.Tier);
        group.alpha = 1f;
        float popDuration = profile.RewardPopDuration;
        float total = popDuration + profile.Hold(weapon.Tier);
        animation = Tween.Custom(0f, total, total, elapsed =>
        {
            float t = Mathf.Clamp01(elapsed / popDuration);
            float pop = profile.AnimateReward ? 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2) : 1f;
            float initial = 0.8f - (int)weapon.Tier * 0.025f;
            visual.localScale = scale * Mathf.LerpUnclamped(initial, 1f, pop);
        }, ease: Ease.Linear, useUnscaledTime: true).OnComplete(() => { visual.localScale = scale; });
    }
    public void Cancel()
    {
        animation.Stop();
        if (initialized) visual.localScale = scale;
        if (group != null) group.alpha = 0f;
    }
    [ContextMenu("Preview Reward (Play Mode)")]
    private void Preview() { if (Application.isPlaying) Present(previewWeapon); }
    private void OnDisable() { Cancel(); }
}
