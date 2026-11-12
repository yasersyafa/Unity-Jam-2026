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
    [SerializeField] private Sprite rewardBackground;
    [SerializeField, Min(0.01f)] private float flashIn = 0.08f;
    [SerializeField, Min(0.01f)] private float reveal = 0.22f;
    [SerializeField, Min(0f)] private float hold = 0.8f;
    [SerializeField, Min(0.01f)] private float flashOut = 0.2f;
    private Tween animation;
    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private Image rewardIcon, flash;
    private Image rewardTitle;
    public bool IsPlaying => animation.isAlive;

    private RectTransform Element(string objectName, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void InitializeOverlay()
    {
        if (overlay != null) return;
        var canvas = GetComponentInParent<Canvas>().rootCanvas;
        var root = Element("WeaponRewardOverlay", canvas.transform, Vector2.zero, Vector2.one);
        overlay = root.gameObject;
        overlayGroup = overlay.AddComponent<CanvasGroup>();
        var layer = overlay.AddComponent<Canvas>();
        layer.overrideSorting = true;
        layer.sortingOrder = canvas.sortingOrder + 100;
        overlay.AddComponent<GraphicRaycaster>();
        var background = Element("Background", root, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        background.sprite = rewardBackground;
        background.color = rewardBackground != null ? Color.white : new Color(1f, 0.88f, 0.12f);
        rewardIcon = Element("Weapon", root, new Vector2(0.25f, 0.12f), new Vector2(0.75f, 0.6f)).gameObject.AddComponent<Image>();
        rewardIcon.preserveAspect = true;
        rewardTitle = Element("Title", root, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.95f)).gameObject.AddComponent<Image>();
        rewardTitle.preserveAspect = true;
        rewardTitle.raycastTarget = false;
        flash = Element("Flash", root, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        flash.color = Color.white;
    }

    public void Present(WeaponData weapon)
    {
        Cancel();
        if (weapon == null || !isActiveAndEnabled) return;
        if (GetComponentInParent<Canvas>() == null) return;
        InitializeOverlay();
        overlay.SetActive(true);
        overlayGroup.alpha = 1;
        rewardIcon.sprite = weapon.RewardSprite;
        rewardIcon.enabled = rewardIcon.sprite != null;
        rewardTitle.sprite = weapon.RewardTitleSprite;
        rewardTitle.enabled = rewardTitle.sprite != null;
        float enter = Mathf.Max(0.01f, flashIn);
        float popTime = Mathf.Max(0.01f, reveal);
        float exit = Mathf.Max(0.01f, flashOut);
        float exitStart = enter + popTime + Mathf.Max(0, hold);
        float total = exitStart + exit;
        flash.color = Color.white;
        rewardIcon.transform.localScale = rewardTitle.transform.localScale = Vector3.one * 0.65f;
        animation = Tween.Custom(0f, total, total, elapsed =>
        {
            float t = Mathf.Clamp01((elapsed - enter) / popTime);
            float pop = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            float drift = Mathf.Clamp01((elapsed - enter - popTime) / Mathf.Max(0.01f, hold));
            rewardIcon.transform.localScale = Vector3.one * (Mathf.LerpUnclamped(0.65f, 1f, pop) + drift * 0.04f);
            rewardTitle.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, pop);
            rewardIcon.rectTransform.anchoredPosition = new Vector2(-18f * drift, 8f * drift);
            float exitT = Mathf.Clamp01((elapsed - exitStart) / exit);
            flash.color = new Color(1, 1, 1, elapsed < enter ? 1f - elapsed / enter : Mathf.Clamp01(exitT * 2));
            overlayGroup.alpha = 1f - Mathf.Clamp01((exitT - 0.5f) * 2);
        }, ease: Ease.Linear, useUnscaledTime: true).OnComplete(Cancel);
    }

    public void Cancel()
    {
        animation.Stop();
        if (group != null) group.alpha = 0;
        if (overlay != null) overlay.SetActive(false);
    }
    private void OnDestroy() { if (overlay != null) Destroy(overlay); }
    [ContextMenu("Preview Reward (Play Mode)")]
    private void Preview() { if (Application.isPlaying) Present(previewWeapon); }
    private void OnDisable() { Cancel(); }
}
