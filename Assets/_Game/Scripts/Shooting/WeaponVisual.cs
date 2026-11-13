using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class WeaponVisual : MonoBehaviour
{
    [SerializeField] private Image weaponImage;
    [Tooltip("Explicit nozzle position used by projectile and stream presentation.")]
    [SerializeField] private RectTransform muzzleAnchor;
    public RectTransform MuzzleAnchor => muzzleAnchor;
    [Tooltip("Optional UI Image under a designer-positioned MuzzleAnchor.")]
    [SerializeField] private Image muzzleFlash;
    [Tooltip("Optional animation frames. Leave empty to flash the Image's existing sprite.")]
    [SerializeField] private Sprite[] muzzleFrames;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.06f;
    [Tooltip("Optional hooks for additional child animations. Triggered only on actual shots.")]
    [SerializeField] private UnityEvent onShot;
    private float elapsed;
    private bool flashing;
    private bool continuousFire;
    private float remaining;

    public void Initialize(Sprite fallbackSprite)
    {
        if (weaponImage != null && weaponImage.sprite == null) weaponImage.sprite = fallbackSprite;
        foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        ResetEffects();
    }
    public void PlayShot()
    {
        if (!isActiveAndEnabled) return;
        if (!flashing) elapsed = 0f;
        remaining = Mathf.Max(0.01f, flashDuration);
        flashing = muzzleFlash != null;
        if (flashing)
        {
            if (elapsed == 0f && muzzleFrames != null && muzzleFrames.Length > 0) muzzleFlash.sprite = muzzleFrames[0];
            muzzleFlash.enabled = true;
        }
        onShot?.Invoke();
    }
    public void SetContinuousFire(bool value)
    {
        if (continuousFire && !value && flashing) remaining = Mathf.Max(0.01f, flashDuration);
        continuousFire = value;
    }
    private void Update()
    {
        if (!flashing) return;
        float duration = Mathf.Max(0.01f, flashDuration);
        elapsed = Mathf.Repeat(elapsed + Time.deltaTime, duration);
        if (!continuousFire)
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f) { ResetEffects(); return; }
        }
        if (muzzleFrames != null && muzzleFrames.Length > 0)
            muzzleFlash.sprite = muzzleFrames[(int)(elapsed / duration * muzzleFrames.Length) % muzzleFrames.Length];
    }
    public void ResetEffects()
    {
        flashing = false;
        continuousFire = false;
        remaining = 0f;
        elapsed = 0f;
        if (muzzleFlash != null) muzzleFlash.enabled = false;
    }
    private void OnDisable() { ResetEffects(); }
    [ContextMenu("Preview Shot (Play Mode)")]
    private void Preview() { if (Application.isPlaying) PlayShot(); }
}
