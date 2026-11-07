using System.Collections;
using UnityEngine;

public sealed class SlotStopFeedback : MonoBehaviour
{
    private RectTransform reel;
    [SerializeField] private RectTransform popup;
    [SerializeField] private CanvasGroup popupGroup;
    private Vector2 restPosition;
    private Vector3 restScale;
    private Quaternion restRotation;
    private float duration;
    private float strength;
    private float popupDuration;
    private Vector2 popupPosition;
    private Vector3 popupScale;
    private Quaternion popupRotation;
    private Coroutine animation;
    public bool IsPlaying => animation != null;

    public void Initialize(RectTransform target, float shakeDuration, float shakeStrength, float bangDuration)
    {
        reel = target;
        restPosition = reel.anchoredPosition;
        restScale = reel.localScale;
        restRotation = reel.localRotation;
        duration = Mathf.Max(0.01f, shakeDuration);
        strength = Mathf.Max(0f, shakeStrength);
        popupDuration = Mathf.Max(duration, bangDuration);
        popupPosition = popup.anchoredPosition;
        popupScale = popup.localScale;
        popupRotation = popup.localRotation;
        popup.gameObject.SetActive(false);
    }

    public void Play(SlotReelView stoppedReel)
    {
        if (!isActiveAndEnabled) return;
        ResetFeedback();
        popup.gameObject.SetActive(true);
        popup.SetAsLastSibling();
        animation = StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        while (elapsed < popupDuration)
        {
            float shake = Mathf.Clamp01(elapsed / duration);
            float decay = (1f - shake) * (1f - shake);
            reel.anchoredPosition = restPosition + new Vector2(Mathf.Sin(shake * 65f), Mathf.Sin(shake * 91f)) * strength * decay;
            reel.localScale = Vector3.Scale(restScale, new Vector3(1f + decay * 0.09f, 1f - decay * 0.07f, 1f));
            reel.localRotation = restRotation * Quaternion.Euler(0, 0, Mathf.Sin(shake * 48f) * decay * 2.5f);
            float t = elapsed / popupDuration;
            float pop = Mathf.Clamp01(t / 0.22f);
            float overshoot = 1f + 2.70158f * Mathf.Pow(pop - 1f, 3) + 1.70158f * Mathf.Pow(pop - 1f, 2);
            popup.localScale = popupScale * Mathf.LerpUnclamped(0.25f, 1f, overshoot);
            popup.localRotation = popupRotation * Quaternion.Euler(0, 0, Mathf.Lerp(-18f, -8f, pop));
            popup.anchoredPosition = popupPosition + Vector2.up * (24f * t);
            popupGroup.alpha = 1f - Mathf.InverseLerp(0.65f, 1f, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        animation = null;
        ResetFeedback();
    }

    private void ResetFeedback()
    {
        if (animation != null) { StopCoroutine(animation); animation = null; }
        if (reel != null)
        {
            reel.anchoredPosition = restPosition;
            reel.localScale = restScale;
            reel.localRotation = restRotation;
        }
        if (popup != null) popup.gameObject.SetActive(false);
    }

    private void OnDisable() { ResetFeedback(); }
}
