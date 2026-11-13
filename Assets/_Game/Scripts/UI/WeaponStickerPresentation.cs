using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class WeaponStickerPresentation : MonoBehaviour
{
    [SerializeField] private CombatHudAnimationProfile profile;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;

    private RectTransform visual;
    private CanvasGroup group;
    private Vector3 baseScale;
    private Vector2 basePosition;
    private Sequence animation;
    private Tween shake;
    private bool initialized;
    private bool warned;
    private bool hasContent;
    private bool automatic;
    private bool continuous;
    private bool holdingShot;
    private bool readyToExit;

    private void Awake()
    {
        Initialize();
        Cancel();
    }

    private void Initialize()
    {
        if (initialized) return;
        visual = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        baseScale = visual.localScale;
        basePosition = visual.anchoredPosition;
        group.interactable = false;
        group.blocksRaycasts = false;
        initialized = true;
    }

    public void SetWeapon(WeaponData weapon)
    {
        Cancel();
        automatic = weapon != null && weapon.FiringMode == WeaponData.FireMode.Automatic;
        hasContent = weapon != null && (weapon.StickerSprite != null || !string.IsNullOrWhiteSpace(weapon.StickerText));
        if (icon != null)
        {
            icon.sprite = weapon != null ? weapon.StickerSprite : null;
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
        }
        if (label != null)
        {
            label.text = weapon != null ? weapon.StickerText : string.Empty;
            label.enabled = !string.IsNullOrWhiteSpace(label.text);
            label.raycastTarget = false;
        }
    }

    public void PlayShot()
    {
        if (!isActiveAndEnabled || !hasContent) return;
        if (profile == null || icon == null || label == null)
        {
            if (!warned) Debug.LogWarning("WeaponStickerPresentation requires profile, icon and label references.", this);
            warned = true;
            return;
        }
        if (holdingShot && continuous) return;

        StopAnimation();
        Initialize();
        holdingShot = automatic && continuous;
        readyToExit = false;
        visual.anchoredPosition = basePosition;
        visual.localScale = baseScale * 0.55f;
        group.alpha = 0f;
        animation = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(visual, baseScale, profile.StickerPopDuration, Ease.OutBack))
            .Group(Tween.Alpha(group, 1f, profile.StickerPopDuration))
            .ChainDelay(profile.StickerHoldDuration)
            .ChainCallback(this, target => target.FinishPop());
        if (holdingShot && profile.StickerShakePixels > 0f)
            shake = Tween.ShakeLocalPosition(visual, new Vector3(profile.StickerShakePixels, profile.StickerShakePixels, 0f),
                1f, frequency: profile.StickerShakeFrequency, enableFalloff: false, cycles: -1, useUnscaledTime: true);
    }

    public void SetContinuousFire(bool value)
    {
        continuous = value;
        if (value || !holdingShot) return;
        holdingShot = false;
        StopShake();
        if (readyToExit) PlayExit();
    }

    private void FinishPop()
    {
        readyToExit = true;
        if (!holdingShot) PlayExit();
    }

    private void PlayExit()
    {
        animation = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(visual, baseScale * 0.8f, profile.StickerExitDuration, Ease.InQuad))
            .Group(Tween.Alpha(group, 0f, profile.StickerExitDuration))
            .ChainCallback(this, target => target.Cancel());
    }

    private void StopShake()
    {
        shake.Stop();
        shake = default;
        if (initialized && visual != null) visual.anchoredPosition = basePosition;
    }

    private void StopAnimation()
    {
        animation.Stop();
        animation = default;
        StopShake();
    }

    public void Cancel()
    {
        StopAnimation();
        continuous = holdingShot = readyToExit = false;
        if (!initialized) return;
        if (group != null) group.alpha = 0f;
        if (visual != null) visual.localScale = baseScale;
    }

    private void OnDisable() { Cancel(); }
    private void OnDestroy() { Cancel(); }
}
