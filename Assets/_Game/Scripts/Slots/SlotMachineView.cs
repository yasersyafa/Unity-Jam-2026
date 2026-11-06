using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class SlotMachineView : MonoBehaviour
{
    [SerializeField] private float firstStopDuration = 2f;
    [SerializeField] private float stopInterval = 0.4f;
    [SerializeField] private TMP_FontAsset font;
    [Header("Snappy Stop")]
    [SerializeField, Min(0.01f)] private float snapDuration = 0.22f;
    [SerializeField, Range(0f, 0.4f)] private float snapOvershoot = 0.12f;
    [Header("Reel Stop Feedback")]
    [SerializeField] private Sprite bangSprite;
    [Tooltip("Optional UI prefab with RectTransform. Overrides Bang Sprite.")]
    [SerializeField] private RectTransform bangUiPrefab;
    [SerializeField, Min(0.01f)] private float shakeDuration = 0.22f;
    [SerializeField, Min(0f)] private float shakeStrength = 9f;
    [SerializeField, Min(0.01f)] private float bangDuration = 0.55f;
    [SerializeField] private Vector2 bangOffset = new Vector2(0, 110);
    [SerializeField] private Vector2 bangSize = new Vector2(190, 100);
    private readonly SlotOutcomeGenerator outcomes = new SlotOutcomeGenerator();
    private SlotResult pendingResult;
    private TextMeshProUGUI status;
    private SlotReelView[] reels;
    private UnityEngine.UI.Button spinButton;

    private void Start()
    {
        var canvasObject = new GameObject("SlotCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).transform.SetParent(transform);
        if (font == null) font = TMP_Settings.defaultFontAsset;
        var panel = Box("Machine", canvasObject.transform, new Vector2(680, 440), Vector2.zero, new Color(0.07f, 0.09f, 0.15f));
        reels = new SlotReelView[3];
        for (int i = 0; i < 3; i++)
        {
            var window = Box("Reel" + (i + 1), panel, new Vector2(190, 280), new Vector2((i - 1) * 208, 38), new Color(0.94f, 0.92f, 0.84f));
            window.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            reels[i] = window.gameObject.AddComponent<SlotReelView>();
            reels[i].Initialize(font);
            reels[i].ConfigureSnap(snapDuration, snapOvershoot);
            for (int band = 0; band < 12; band++)
            {
                float alpha = 0.38f * Mathf.Pow(1 - band / 12f, 2);
                foreach (int side in new[] { -1, 1 })
                    Box("Shade", window, new Vector2(190, 9), new Vector2(0, side * (136 - band * 9)), new Color(0.03f, 0.04f, 0.08f, alpha));
            }
        }
        var overlay = new GameObject("StopFeedback", typeof(RectTransform)).GetComponent<RectTransform>();
        overlay.SetParent(panel, false);
        overlay.sizeDelta = Vector2.zero;
        foreach (var reel in reels)
        {
            var feedback = reel.gameObject.AddComponent<SlotStopFeedback>();
            feedback.Initialize((RectTransform)reel.transform, overlay, font, bangSprite, bangUiPrefab,
                shakeDuration, shakeStrength, bangDuration, bangOffset, bangSize);
            reel.Stopped += feedback.Play;
        }
        status = new GameObject("Result", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        status.transform.SetParent(canvasObject.transform, false);
        status.rectTransform.sizeDelta = new Vector2(900, 100);
        status.rectTransform.anchoredPosition = new Vector2(0, -280);
        status.font = font;
        status.fontSize = 24;
        status.alignment = TextAlignmentOptions.Center;
        status.raycastTarget = false;
        status.text = "Spin 0 | Rare 0/5 | Epic 0/15 | Jackpot 0/25";
        var button = Box("Spin", panel, new Vector2(240, 62), new Vector2(0, -155), new Color(0.95f, 0.65f, 0.18f));
        button.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        spinButton = button.gameObject.AddComponent<UnityEngine.UI.Button>();
        spinButton.onClick.AddListener(Spin);
        var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(button, false);
        label.rectTransform.sizeDelta = new Vector2(240, 62);
        label.font = font;
        label.text = "SPIN";
        label.fontSize = 30;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        label.raycastTarget = false;
    }

    public void Spin()
    {
        if (reels == null || !spinButton.interactable) return;
        spinButton.interactable = false;
        pendingResult = outcomes.Spin();
        status.text = "Spinning...";
        int[] targets = { pendingResult.Left, pendingResult.Middle, pendingResult.Right };
        for (int i = 0; i < reels.Length; i++) reels[i].Spin(firstStopDuration + i * stopInterval, targets[i]);
    }

    private void Update()
    {
        if (reels == null || spinButton.interactable) return;
        foreach (var reel in reels) if (reel.IsSpinning) return;
        status.text = $"Spin {pendingResult.SpinNumber}: {pendingResult.Left}{pendingResult.Middle}{pendingResult.Right} — {pendingResult.Tier}"
            + $"\nRare {outcomes.RareCounter}/5 | Epic {outcomes.EpicCounter}/15 | Jackpot {outcomes.JackpotCounter}/25";
        spinButton.interactable = true;
    }

    private static RectTransform Box(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }
}
