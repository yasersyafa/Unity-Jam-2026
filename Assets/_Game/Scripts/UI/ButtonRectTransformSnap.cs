using UnityEngine;
using PrimeTween;
using TMPro;
using UnityEngine.UI;

/// <summary>Attach to the target RectTransform and bind Play to the source Button's On Click.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class ButtonRectTransformSnap : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Button button;

    [Header("Initial Pose")]
    [SerializeField] private Vector3 initialPosition = new Vector3(648.8f, 116.2f, 0f);
    [SerializeField] private Vector3 initialRotation = Vector3.zero;

    [Header("Clicked Pose")]
    [SerializeField] private Vector3 clickedPosition = new Vector3(648.8f, 137f, 0f);
    [SerializeField] private Vector3 clickedRotation = new Vector3(0f, 180f, -180f);
    [Tooltip("Seconds in real time before returning to the initial pose. Repeated clicks restart the timer.")]
    [SerializeField, Min(0f)] private float returnDelay = 0.5f;

    [Header("Impact")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.15f;
    [SerializeField] private Vector2 shakePosition = new Vector2(6f, 4f);
    [SerializeField, Min(0f)] private float shakeRotation = 4f;
    [SerializeField, Min(0f)] private float shakeFrequency = 24f;

    [Header("Sticker")]
    [SerializeField] private string stickerText = "CRACK!";
    [SerializeField] private Color stickerTextColor = Color.yellow;
    [SerializeField] private Sprite stickerSprite;
    [SerializeField] private Vector2 stickerOffset = new Vector2(0f, 70f);
    [SerializeField] private Vector2 stickerSize = new Vector2(180f, 90f);
    [SerializeField] private float stickerAngle = -10f;
    [SerializeField, Min(0.001f)] private float stickerPopDuration = 0.08f;
    [SerializeField, Min(0f)] private float stickerHoldDuration = 0.12f;
    [SerializeField, Min(0.001f)] private float stickerExitDuration = 0.1f;

    private Tween impact;
    private Sequence stickerAnimation;
    private RectTransform sticker;
    private CanvasGroup stickerGroup;
    private Image stickerImage;
    private TextMeshProUGUI stickerLabel;
    private RectTransform target;
    private bool waitingToReturn;
    private double returnTime;

    private void Awake()
    {
        target = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        RestoreInitialPose();
    }

    public void Play()
    {
        if (!isActiveAndEnabled || button == null || !button.isActiveAndEnabled || !button.IsInteractable())
            return;

        impact.Stop();
        target.anchoredPosition3D = clickedPosition;
        target.localRotation = Quaternion.Euler(clickedRotation);
        returnTime = Time.unscaledTimeAsDouble + Mathf.Max(0f, returnDelay);
        waitingToReturn = true;

        PlaySticker();
        if (shakeDuration > 0f)
            impact = Tween.Custom(0f, 1f, shakeDuration, progress =>
            {
                float angle = progress * shakeDuration * shakeFrequency * Mathf.PI * 2f;
                float falloff = (1f - progress) * (1f - progress);
                target.anchoredPosition3D = clickedPosition + new Vector3(
                    Mathf.Sin(angle) * shakePosition.x, Mathf.Sin(angle * 1.3f) * shakePosition.y, 0f) * falloff;
                target.localRotation = Quaternion.Euler(clickedRotation) * Quaternion.Euler(
                    0f, 0f, Mathf.Sin(angle) * shakeRotation * falloff);
            }, ease: Ease.Linear, useUnscaledTime: true);

        if (returnDelay <= 0f)
            RestoreInitialPose();
    }

    private void Update()
    {
        if (waitingToReturn && Time.unscaledTimeAsDouble >= returnTime)
            RestoreInitialPose();
    }

    private void OnDisable()
    {
        RestoreInitialPose();
        stickerAnimation.Stop();
        if (stickerGroup != null) stickerGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        impact.Stop();
        stickerAnimation.Stop();
        if (sticker != null) Destroy(sticker.gameObject);
    }

    private void PlaySticker()
    {
        stickerAnimation.Stop();
        if (sticker == null)
        {
            sticker = (RectTransform)new GameObject("Snap Sticker", typeof(RectTransform), typeof(CanvasGroup)).transform;
            stickerGroup = sticker.GetComponent<CanvasGroup>();
            stickerGroup.interactable = false;
            stickerGroup.blocksRaycasts = false;
            stickerImage = new GameObject("Sprite", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            stickerLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            foreach (RectTransform child in new[] { stickerImage.rectTransform, stickerLabel.rectTransform })
            {
                child.SetParent(sticker, false);
                child.anchorMin = Vector2.zero;
                child.anchorMax = Vector2.one;
                child.sizeDelta = Vector2.zero;
                child.anchoredPosition = Vector2.zero;
            }
            stickerImage.raycastTarget = false;
            stickerImage.preserveAspect = true;
            stickerLabel.raycastTarget = false;
            stickerLabel.alignment = TextAlignmentOptions.Center;
            stickerLabel.enableAutoSizing = true;
            stickerLabel.fontSizeMin = 8f;
            stickerLabel.fontSizeMax = 48f;
            stickerLabel.fontStyle = FontStyles.Bold;
        }
        sticker.SetParent(target.parent, false);
        sticker.anchorMin = target.anchorMin;
        sticker.anchorMax = target.anchorMin;
        sticker.pivot = new Vector2(0.5f, 0.5f);
        sticker.position = target.TransformPoint(target.rect.center);
        sticker.anchoredPosition += stickerOffset;
        sticker.sizeDelta = stickerSize;
        sticker.localRotation = Quaternion.Euler(0f, 0f, stickerAngle);
        sticker.SetAsLastSibling();
        stickerImage.sprite = stickerSprite;
        stickerImage.enabled = stickerSprite != null;
        stickerLabel.text = stickerText;
        stickerLabel.color = stickerTextColor;
        stickerLabel.enabled = !string.IsNullOrWhiteSpace(stickerText);
        stickerGroup.alpha = 0f;
        sticker.localScale = Vector3.one * 0.55f;
        stickerAnimation = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(sticker, Vector3.one, Mathf.Max(0.001f, stickerPopDuration), Ease.OutBack))
            .Group(Tween.Alpha(stickerGroup, 1f, Mathf.Max(0.001f, stickerPopDuration)))
            .ChainDelay(Mathf.Max(0f, stickerHoldDuration))
            .Chain(Tween.Alpha(stickerGroup, 0f, Mathf.Max(0.001f, stickerExitDuration)));
    }

    private void RestoreInitialPose()
    {
        impact.Stop();
        waitingToReturn = false;
        if (target == null)
            return;

        target.anchoredPosition3D = initialPosition;
        target.localRotation = Quaternion.Euler(initialRotation);
    }
}
