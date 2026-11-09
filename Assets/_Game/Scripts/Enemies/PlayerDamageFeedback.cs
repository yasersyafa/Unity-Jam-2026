using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDamageFeedback : MonoBehaviour
{
    [SerializeField] private CombatFeedbackPreset preset;
    [SerializeField] private CombatCameraFeedback cameraFeedback;
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Preferred damage cue. Empty uses the existing preset audio.")]
    [SerializeField] private AudioCue damageSound;
    [Header("Damage Artwork")]
    [Tooltip("Drag a transparent comic border sprite here. Import PNG as Sprite (2D and UI). Keep the center transparent. Stretches to fill the screen.")]
    [SerializeField] private Sprite damageOverlaySprite;
    [Tooltip("White preserves the artwork colors. Alpha controls maximum opacity.")]
    [SerializeField] private Color overlayTint = Color.white;
    [Tooltip("Fade duration in seconds, independent of camera shake.")]
    [SerializeField, Min(0.01f)] private float overlayDuration = 0.2f;
    [Tooltip("Overlay opacity multiplier while health is critical.")]
    [SerializeField, Range(0f, 1f)] private float criticalOpacity = 0.15f;
    private PlayerHealth health;
    private Canvas overlay;
    private UnityEngine.UI.Image damageImage;
    private float remaining;
    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        if (cameraFeedback == null && Camera.main != null)
            cameraFeedback = Camera.main.GetComponent<CombatCameraFeedback>() ?? Camera.main.gameObject.AddComponent<CombatCameraFeedback>();
        GameObject canvasObject = new GameObject("Damage Overlay", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 100;
        var imageObject = new GameObject("Comic Damage Artwork", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        damageImage = imageObject.GetComponent<UnityEngine.UI.Image>();
        damageImage.raycastTarget = false;
        damageImage.type = UnityEngine.UI.Image.Type.Simple;
        var rect = damageImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        overlay.enabled = false;
    }

    private void OnEnable() { health.Damaged += PlayDamage; }
    private void OnDisable()
    {
        health.Damaged -= PlayDamage;
        remaining = 0f;
        if (overlay != null) overlay.enabled = false;
    }
    [ContextMenu("Preview Damage Feedback (Play Mode)")]
    private void Preview() { if (Application.isPlaying) PlayDamage(10f); }
    private void PlayDamage(float damage)
    {
        remaining = Mathf.Max(0.01f, overlayDuration);
        if (cameraFeedback != null) cameraFeedback.Play(preset != null ? preset.shakeStrength : 0.06f, preset != null ? preset.duration : 0.16f);
        if (damageSound != null) AudioService.Ensure().Play(damageSound);
        else if (audioSource != null && preset != null && preset.impactSound != null)
            audioSource.PlayOneShot(preset.impactSound, preset.volume);
    }
    private void Update()
    {
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        float critical = preset != null ? preset.criticalHealthRatio : 0.25f;
        Color color = overlayTint;
        float healthRatio = health.CurrentHealth / Mathf.Max(1f, health.MaximumHealth);
        color.a *= Mathf.Max(remaining / Mathf.Max(0.01f, overlayDuration),
            !health.IsDead && healthRatio <= critical ? criticalOpacity : 0f);
        damageImage.sprite = damageOverlaySprite;
        damageImage.color = color;
        overlay.enabled = damageOverlaySprite != null && color.a > 0f;
    }
}
