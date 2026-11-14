using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class EndScreenPresentation : MonoBehaviour
{
    [SerializeField] private CanvasGroup root;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Image panelImage;
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private Image titleImage;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Artwork")]
    [SerializeField] private Sprite winPanelSprite;
    [SerializeField] private Sprite losePanelSprite;
    [SerializeField] private Sprite victoryTitleSprite;
    [SerializeField] private Sprite loseTitleSprite;
    [SerializeField] private Vector2 victoryTitleSize = new(1248f, 384f);
    [SerializeField] private Vector2 loseTitleSize = new(743f, 310f);

    [Header("Entrance Animation")]
    [SerializeField, Min(100f)] private float panelSlideDistance = 2000f;
    [SerializeField, Min(0.05f)] private float panelSlideDuration = 0.22f;
    [SerializeField, Min(0f)] private float titleFadeDelay = 0.16f;
    [SerializeField, Min(0.05f)] private float titleFadeDuration = 0.16f;
    [SerializeField, Min(1f)] private float titlePopStartScale = 1.14f;

    [Header("Title Idle Animation (Kedat-Kedut)")]
    [SerializeField, Min(0f)] private float idleScaleAmplitude = 0.055f;
    [SerializeField, Min(0f)] private float idleTiltDegrees = 1.8f;
    [SerializeField, Min(0.1f)] private float idleFrequency = 3.2f;

    [Header("Scene Navigation")]
    [SerializeField] private string gameScene = "GameScene";
    [SerializeField] private string menuScene = "MenuScene";

    private Tween entranceTween;
    private Vector2 panelOrigin;
    private Vector2 titleOrigin;
    private bool initialized;
    private bool showing;
    private bool isVictory;
    private float entranceProgress = 1f;
    private float idleElapsed;

    public bool IsShowing => showing;
    public bool IsVictory => isVictory;
    public float TotalEntranceDuration => titleFadeDelay + titleFadeDuration;

    private void Awake()
    {
        EnsureInitialized();
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(Retry);
            retryButton.onClick.AddListener(Retry);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(GoToMainMenu);
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
        CancelAndReset();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        if (panelRect != null) panelOrigin = panelRect.anchoredPosition;
        if (titleRect != null) titleOrigin = titleRect.anchoredPosition;
    }

    public void ShowVictory() => Show(victory: true);

    public void ShowLose() => Show(victory: false);

    public void Show(bool victory)
    {
        EnsureInitialized();
        entranceTween.Stop();
        isVictory = victory;
        showing = true;
        idleElapsed = 0f;
        entranceProgress = 0f;

        if (panelImage != null)
            panelImage.sprite = victory ? winPanelSprite : losePanelSprite;
        if (titleImage != null)
            titleImage.sprite = victory ? victoryTitleSprite : loseTitleSprite;
        if (titleRect != null)
            titleRect.sizeDelta = victory ? victoryTitleSize : loseTitleSize;

        if (root != null)
        {
            root.alpha = 1f;
            root.interactable = true;
            root.blocksRaycasts = true;
        }

        ApplyEntrance(0f);
        ApplyIdle(0f);

        float duration = Mathf.Max(0.05f, TotalEntranceDuration);
        entranceTween = Tween.Custom(0f, duration, duration, ApplyEntrance, ease: Ease.Linear, useUnscaledTime: true);
    }

    public void ApplyEntrance(float time)
    {
        EnsureInitialized();
        float panelT = Mathf.Clamp01(time / Mathf.Max(0.01f, panelSlideDuration));
        float panelEased = 1f - Mathf.Pow(1f - panelT, 3f);
        if (panelRect != null)
            panelRect.anchoredPosition = panelOrigin + Vector2.right * ((1f - panelEased) * panelSlideDistance);

        float fadeT = Mathf.Clamp01((time - titleFadeDelay) / Mathf.Max(0.01f, titleFadeDuration));
        float fadeSmooth = Mathf.SmoothStep(0f, 1f, fadeT);
        entranceProgress = fadeSmooth;

        if (titleGroup != null)
            titleGroup.alpha = fadeSmooth;
        if (buttonsGroup != null)
            buttonsGroup.alpha = fadeSmooth;
        if (titleRect != null)
            titleRect.anchoredPosition = titleOrigin;

        ApplyIdle(idleElapsed);
    }

    public void EvaluateIdlePose(float time, out Vector3 scale, out float tiltAngle)
    {
        float phase = time * idleFrequency * Mathf.PI * 2f;
        float primary = Mathf.Max(0f, Mathf.Sin(phase));
        float secondary = Mathf.Max(0f, Mathf.Sin(phase * 2f - 0.6f)) * 0.55f;
        float beat = Mathf.Clamp01(primary * primary + secondary);
        float popMultiplier = Mathf.Lerp(titlePopStartScale, 1f, entranceProgress);
        float s = popMultiplier * (1f + idleScaleAmplitude * beat);
        scale = new Vector3(s, s, 1f);
        tiltAngle = Mathf.Sin(phase * 0.5f) * idleTiltDegrees;
    }

    public void ApplyIdle(float time)
    {
        if (titleRect == null) return;
        EvaluateIdlePose(time, out Vector3 scale, out float tiltAngle);
        titleRect.localScale = scale;
        titleRect.localEulerAngles = new Vector3(0f, 0f, tiltAngle);
    }

    private void Update()
    {
        if (!showing) return;
        idleElapsed += Time.unscaledDeltaTime;
        ApplyIdle(idleElapsed);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        string current = SceneManager.GetActiveScene().name;
        SceneTransition.Load(!string.IsNullOrEmpty(current) ? current : gameScene);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneTransition.Load(menuScene);
    }

    public void CancelAndReset()
    {
        EnsureInitialized();
        entranceTween.Stop();
        showing = false;
        idleElapsed = 0f;
        entranceProgress = 1f;
        if (root != null)
        {
            root.alpha = 0f;
            root.interactable = false;
            root.blocksRaycasts = false;
        }
        if (titleGroup != null) titleGroup.alpha = 0f;
        if (buttonsGroup != null) buttonsGroup.alpha = 0f;
        if (panelRect != null) panelRect.anchoredPosition = panelOrigin;
        if (titleRect != null)
        {
            titleRect.anchoredPosition = titleOrigin;
            titleRect.localScale = Vector3.one;
            titleRect.localEulerAngles = Vector3.zero;
        }
    }

    private void OnDisable() => CancelAndReset();
}
