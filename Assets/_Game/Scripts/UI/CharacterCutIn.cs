using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterCutIn : MonoBehaviour
{
    [SerializeField] private RectTransform animatedRoot;
    [SerializeField] private CanvasGroup visibility;
    [SerializeField] private Image portrait;
    [SerializeField] private Image accent;
    [SerializeField] private TMP_Text quote;
    [SerializeField, Min(0.1f)] private float duration = 0.9f;
    [SerializeField] private bool leftSide;
    [SerializeField] private bool flipPortrait;
    [SerializeField] private Vector2 offset;
    [SerializeField, Min(0.1f)] private float scale = 1f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip whoosh;
    [SerializeField] private AudioClip impact;
    private float elapsed;
    private bool playing;
    private bool impacted;

    public void Show(Sprite image, string text, Color color)
    {
        if (!isActiveAndEnabled) return;
        portrait.sprite = image;
        portrait.enabled = image != null;
        quote.text = text ?? string.Empty;
        accent.color = color;
        elapsed = 0f;
        impacted = false;
        playing = true;
        if (audioSource != null && whoosh != null) audioSource.PlayOneShot(whoosh);
        Render();
    }

    private void OnEnable() { Hide(); }
    private void OnDisable() { Hide(); }
    private void Hide()
    {
        playing = false;
        if (visibility != null) visibility.alpha = 0f;
        if (audioSource != null) audioSource.Stop();
    }
    private void Update()
    {
        if (!playing) return;
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= duration) { Hide(); return; }
        Render();
    }
    private void Render()
    {
        float t = elapsed / duration * 0.9f;
        float enter = Mathf.Clamp01((t - 0.04f) / 0.12f);
        float exit = Mathf.Clamp01((t - 0.66f) / 0.24f);
        float ease = 1f + 2.70158f * Mathf.Pow(enter - 1f, 3f) + 1.70158f * Mathf.Pow(enter - 1f, 2f);
        var parent = (RectTransform)transform;
        Rect safe = Screen.safeArea;
        float sx = parent.rect.width / Mathf.Max(1, Screen.width);
        float sy = parent.rect.height / Mathf.Max(1, Screen.height);
        float edge = leftSide ? safe.xMin : Screen.width - safe.xMax;
        animatedRoot.anchorMin = animatedRoot.anchorMax = new Vector2(leftSide ? 0f : 1f, 0.42f);
        animatedRoot.pivot = new Vector2(leftSide ? 0f : 1f, 0.5f);
        animatedRoot.sizeDelta = new Vector2(parent.rect.width * 0.42f, parent.rect.height * 0.32f);
        float direction = leftSide ? -1f : 1f;
        animatedRoot.anchoredPosition = offset + new Vector2(-direction * (edge * sx + parent.rect.width * 0.04f) + direction * (1f - ease + exit * exit) * parent.rect.width * 0.6f, Mathf.Max(0f, safe.yMin * sy - parent.rect.height * 0.2f));
        animatedRoot.localScale = Vector3.one * scale;
        float p = Mathf.Clamp01((t - 0.08f) / 0.14f);
        portrait.rectTransform.anchorMin = new Vector2(leftSide ? 0f : 0.55f, 0f);
        portrait.rectTransform.anchorMax = new Vector2(leftSide ? 0.45f : 1f, 1f);
        float size = Mathf.Lerp(1.1f, 1f, p);
        portrait.rectTransform.localScale = new Vector3(flipPortrait ? -size : size, size, 1f);
        portrait.rectTransform.anchoredPosition = new Vector2(direction * (1f-p) * 60f - direction * Mathf.Clamp01((t-0.24f)/0.42f) * animatedRoot.rect.width * 0.01f, 0f);
        quote.rectTransform.anchorMin = new Vector2(leftSide ? 0.45f : 0.04f, 0.12f);
        quote.rectTransform.anchorMax = new Vector2(leftSide ? 0.96f : 0.55f, 0.88f);
        float reveal = Mathf.Clamp01((t - 0.14f) / 0.1f);
        quote.alpha = reveal * (1f - exit);
        quote.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, reveal);
        visibility.alpha = Mathf.Clamp01(t / 0.04f) * (1f-exit);
        if (!impacted && t >= 0.14f)
        {
            impacted = true;
            if (audioSource != null && impact != null) audioSource.PlayOneShot(impact);
        }
    }
}
