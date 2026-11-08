using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public sealed class PlayerDamageFeedback : MonoBehaviour
{
    [SerializeField] private CombatFeedbackPreset preset;
    [SerializeField] private CombatCameraFeedback cameraFeedback;
    [SerializeField] private AudioSource audioSource;
    private PlayerHealth health;
    private Canvas overlay;
    private Image[] edges;
    private float remaining;
    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        if (cameraFeedback == null && Camera.main != null)
            cameraFeedback = Camera.main.GetComponent<CombatCameraFeedback>() ?? Camera.main.gameObject.AddComponent<CombatCameraFeedback>();
        GameObject canvasObject = new GameObject("Damage Border", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 100;
        edges = new Image[4];
        for (int i = 0; i < edges.Length; i++)
        {
            var edge = new GameObject("Border", typeof(RectTransform), typeof(Image));
            edge.transform.SetParent(canvasObject.transform, false);
            edges[i] = edge.GetComponent<Image>();
            edges[i].raycastTarget = false;
            var rect = edges[i].rectTransform;
            rect.anchorMin = i == 0 ? new Vector2(0, 0.97f) : i == 3 ? new Vector2(0.98f, 0) : Vector2.zero;
            rect.anchorMax = i == 1 ? new Vector2(1, 0.03f) : i == 2 ? new Vector2(0.02f, 1) : Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            edges[i].color = Color.clear;
        }
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
        remaining = preset != null ? preset.duration : 0.16f;
        if (cameraFeedback != null) cameraFeedback.Play(preset != null ? preset.shakeStrength : 0.06f, remaining);
        if (audioSource != null && preset != null && preset.impactSound != null)
            audioSource.PlayOneShot(preset.impactSound, preset.volume);
    }
    private void Update()
    {
        remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        float duration = preset != null ? preset.duration : 0.16f;
        float critical = preset != null ? preset.criticalHealthRatio : 0.25f;
        Color color = preset != null ? preset.damageColor : new Color(0.85f, 0.08f, 0.12f, 0.35f);
        color.a *= Mathf.Max(remaining / Mathf.Max(0.01f, duration), !health.IsDead && health.CurrentHealth / health.MaximumHealth <= critical ? 0.2f : 0f);
        overlay.enabled = color.a > 0f;
        foreach (var edge in edges) edge.color = color;
    }
}
