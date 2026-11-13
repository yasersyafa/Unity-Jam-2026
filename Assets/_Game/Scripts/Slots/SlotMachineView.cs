using TMPro;
using UnityEngine;

public sealed class SlotMachineView : MonoBehaviour
{
    [SerializeField] private float firstStopDuration = 2f;
    [SerializeField] private float stopInterval = 0.4f;
    [Header("Snappy Stop")]
    [SerializeField, Min(0.01f)] private float snapDuration = 0.22f;
    [SerializeField, Range(0f, 0.4f)] private float snapOvershoot = 0.12f;
    [Header("Reel Stop Feedback")]
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.22f;
    [SerializeField, Min(0f)] private float shakeStrength = 9f;
    [SerializeField, Min(0.01f)] private float bangDuration = 0.55f;
    private readonly SlotOutcomeGenerator outcomes = new SlotOutcomeGenerator();
    private SlotResult pendingResult;
    [Header("Scene UI References")]
    [SerializeField] private TextMeshProUGUI status;
    [SerializeField] private SlotReelView[] reels;
    [SerializeField] private UnityEngine.UI.Button spinButton;
    [SerializeField] private UnityEngine.UI.Button skipButton;
    [SerializeField] private GameObject slotCanvas;
    [SerializeField] private SlotStopFeedback[] stopFeedback;
    [Header("Comic Presentation")]
    [SerializeField] private SlotPanelPresentation panelPresentation;
    [SerializeField] private SlotRewardPresentation rewardPresentation;
    public bool IsTransitioning => panelPresentation != null && panelPresentation.IsPlaying;
    private Coroutine pendingReward;
    private bool initialized;
    private bool visibilityRequested;
    private bool spinAllowed = true;
    private bool panelVisible = true;
    public bool IsPresenting
    {
        get
        {
            if (pendingReward != null || IsSpinning || (rewardPresentation != null && rewardPresentation.IsPlaying)) return true;
            if (stopFeedback != null) foreach (var feedback in stopFeedback)
                if (feedback != null && feedback.IsPlaying) return true;
            return false;
        }
    }
    public bool IsSpinning { get; private set; }
    public event System.Action<SlotResult> SpinCompleted;

    public void SetSpinAllowed(bool allowed)
    {
        spinAllowed = allowed;
        if (spinButton != null) spinButton.interactable = allowed && !IsSpinning;
    }

    public void SetPanelVisible(bool visible)
    {
        if (visibilityRequested && panelVisible == visible) return;
        visibilityRequested = true;
        panelVisible = visible;
        if (visible && rewardPresentation != null) rewardPresentation.Cancel();
        if (visible && status != null) status.enabled = true;
        if (panelPresentation != null) panelPresentation.Show(visible);
        else if (slotCanvas != null) slotCanvas.SetActive(visible);
    }

    public void HidePanelImmediate()
    {
        if (pendingReward != null) StopCoroutine(pendingReward);
        pendingReward = null;
        IsSpinning = false;
        if (initialized) foreach (var reel in reels) if (reel != null) reel.CancelSpin();
        panelVisible = false;
        visibilityRequested = true;
        if (rewardPresentation != null) rewardPresentation.Cancel();
        if (panelPresentation != null) panelPresentation.HideImmediate();
        else if (slotCanvas != null) slotCanvas.SetActive(false);
    }

    public void PresentReward(WeaponData weapon)
    {
        if (rewardPresentation != null)
        {
            if (status != null) status.enabled = false;
            if (pendingReward != null) StopCoroutine(pendingReward);
            pendingReward = StartCoroutine(PresentAfterStop(weapon));
        }
    }

    private System.Collections.IEnumerator PresentAfterStop(WeaponData weapon)
    {
        // Always yield once so the coroutine handle is assigned before clearing it.
        yield return null;
        bool waiting;
        do
        {
            waiting = false;
            if (stopFeedback != null)
                foreach (var feedback in stopFeedback)
                    if (feedback != null && feedback.IsPlaying) waiting = true;
            if (waiting) yield return null;
        } while (waiting);
        rewardPresentation.Present(weapon);
        pendingReward = null;
    }

    private bool Initialize()
    {
        if (initialized) return true;
        if (slotCanvas == null || status == null || spinButton == null || skipButton == null
            || reels == null || reels.Length != 3 || stopFeedback == null || stopFeedback.Length != 3
            || System.Array.Exists(reels, reel => reel == null) || System.Array.Exists(stopFeedback, feedback => feedback == null))
        {
            Debug.LogError("SlotMachineView requires its authored slot UI references.", this);
            enabled = false;
            return false;
        }
        for (int i = 0; i < reels.Length; i++)
        {
            reels[i].Initialize();
            reels[i].ConfigureSnap(snapDuration, snapOvershoot);
            stopFeedback[i].Initialize((RectTransform)reels[i].transform, shakeDuration, shakeStrength, bangDuration);
            reels[i].Stopped += stopFeedback[i].Play;
        }
        skipButton.interactable = false;
        if (panelPresentation != null) panelPresentation.Prewarm();
        SetSpinAllowed(spinAllowed);
        initialized = true;
        return true;
    }

    private void Start() { Initialize(); }

    public void Spin()
    {
        if (!Initialize() || IsTransitioning || !spinAllowed || IsPresenting || !panelVisible) return;
        IsSpinning = true;
        spinButton.interactable = false;
        skipButton.interactable = true;
        pendingResult = outcomes.Spin();
        status.text = "Spinning...";
        int[] targets = { pendingResult.Left, pendingResult.Middle, pendingResult.Right };
        for (int i = 0; i < reels.Length; i++) reels[i].Spin(firstStopDuration + i * stopInterval, targets[i]);
    }

    public void Skip()
    {
        if (IsTransitioning || reels == null || !skipButton.interactable) return;
        skipButton.interactable = false;
        foreach (var reel in reels) reel.Skip();
    }

    private void Update()
    {
        if (reels == null || !IsSpinning) return;
        foreach (var reel in reels) if (reel.IsSpinning) return;
        CompleteSpin();
    }

    private void CompleteSpin()
    {
        if (!IsSpinning) return;
        IsSpinning = false;
        skipButton.interactable = false;
        status.text = $"Spin {pendingResult.SpinNumber}: {pendingResult.Left}{pendingResult.Middle}{pendingResult.Right} — {pendingResult.Tier}"
            + $"\nRare {outcomes.RareCounter}/5 | Epic {outcomes.EpicCounter}/15 | Jackpot {outcomes.JackpotCounter}/25";
        spinButton.interactable = spinAllowed;
        SpinCompleted?.Invoke(pendingResult);
    }

    private void OnDisable() { HidePanelImmediate(); }

    private void OnDestroy()
    {
        if (!initialized || reels == null || stopFeedback == null) return;
        for (int i = 0; i < Mathf.Min(reels.Length, stopFeedback.Length); i++)
            if (reels[i] != null && stopFeedback[i] != null) reels[i].Stopped -= stopFeedback[i].Play;
    }
}
