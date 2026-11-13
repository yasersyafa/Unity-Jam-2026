using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Comic-panel tutorial UI, built at runtime on its own overlay canvas. The canvas sorts just below the gameplay
/// HUD so the slot machine overlay always covers it. All motion uses unscaled time because the slot pauses the game.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialPresenter : MonoBehaviour
{
    private static readonly Color Ink = new(0.08f, 0.06f, 0.06f, 1f);
    private static readonly Color Paper = new(1f, 0.97f, 0.88f, 1f);
    private static readonly Color Yellow = new(1f, 0.86f, 0.12f, 1f);
    private static readonly Color Red = new(0.91f, 0.19f, 0.16f, 1f);

    [Tooltip("Comic font; TMP default font when empty.")]
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("Below the gameplay HUD canvas (20) so the slot machine stays on top.")]
    [SerializeField] private int sortingOrder = 19;
    [SerializeField, Min(0.5f)] private float coachDuration = 2.8f;

    private Canvas canvas;
    private CanvasGroup root;
    private RectTransform card;
    private TextMeshProUGUI title, instruction, hint, coach, pop, skipLabel;
    private RectTransform coachBubble, skipFill;
    private CanvasGroup coachGroup, hintGroup, popGroup, skipGroup;
    private Image[] pips = System.Array.Empty<Image>();
    private Sequence cardAnimation, coachAnimation, popAnimation;
    private Tween hintBlink, pipPulse;

    public void Show(int stepCount)
    {
        Build();
        canvas.gameObject.SetActive(true);
        root.alpha = 1f;
        BuildPips(stepCount);
        hintGroup.alpha = coachGroup.alpha = popGroup.alpha = 0f;
        SetSkipProgress(0f);
    }

    public void ShowStep(int index, string heading, string body)
    {
        Build();
        title.text = heading;
        instruction.text = body;
        HideHint();
        for (int i = 0; i < pips.Length; i++)
        {
            pips[i].color = i < index ? Ink : i == index ? Yellow : Paper;
            pips[i].rectTransform.localScale = Vector3.one;
        }
        pipPulse.Stop();
        if (index < pips.Length)
            pipPulse = Tween.Scale(pips[index].rectTransform, Vector3.one * 1.25f, 0.3f, Ease.InOutSine,
                cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);

        // Stamp: slam in oversized and tilted, then settle.
        cardAnimation.Stop();
        float tilt = Random.Range(-4f, 4f);
        card.localScale = Vector3.one * 1.6f;
        card.localEulerAngles = new Vector3(0f, 0f, tilt * 3f);
        cardAnimation = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(card, Vector3.one, 0.22f, Ease.OutBack))
            .Group(Tween.LocalEulerAngles(card, card.localEulerAngles, new Vector3(0f, 0f, tilt), 0.22f, Ease.OutBack))
            .Chain(Tween.PunchLocalPosition(card, new Vector3(0f, -12f, 0f), 0.18f));
    }

    public void ShowHint(string text)
    {
        Build();
        hint.text = text;
        hintBlink.Stop();
        hintGroup.alpha = 1f;
        hintBlink = Tween.Alpha(hintGroup, 1f, 0.35f, 0.45f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
    }

    public void HideHint()
    {
        hintBlink.Stop();
        if (hintGroup != null) hintGroup.alpha = 0f;
    }

    public void ShowCoach(string line)
    {
        Build();
        coach.text = line;
        coachAnimation.Stop();
        coachBubble.localScale = Vector3.one * 0.3f;
        coachGroup.alpha = 0f;
        coachAnimation = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(coachBubble, Vector3.one, 0.25f, Ease.OutBack))
            .Group(Tween.Alpha(coachGroup, 1f, 0.12f))
            .ChainDelay(coachDuration)
            .Chain(Tween.Alpha(coachGroup, 0f, 0.2f));
    }

    /// <summary>Onomatopoeia burst in the middle of the screen.</summary>
    public void Pop(string word)
    {
        Build();
        pop.text = word;
        popAnimation.Stop();
        var rect = pop.rectTransform;
        rect.localScale = Vector3.one * 2.2f;
        rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(-14f, 14f));
        popGroup.alpha = 1f;
        popAnimation = Sequence.Create(useUnscaledTime: true)
            .Chain(Tween.Scale(rect, Vector3.one, 0.18f, Ease.OutBack))
            .ChainDelay(0.35f)
            .Chain(Tween.Alpha(popGroup, 0f, 0.2f))
            .Group(Tween.Scale(rect, Vector3.one * 1.3f, 0.2f, Ease.InQuad));
    }

    public void SetSkipProgress(float progress)
    {
        if (skipFill == null) return;
        progress = Mathf.Clamp01(progress);
        skipFill.localScale = new Vector3(progress, 1f, 1f);
        skipGroup.alpha = progress > 0f ? 1f : 0.6f;
    }

    public void Hide()
    {
        cardAnimation.Stop();
        coachAnimation.Stop();
        popAnimation.Stop();
        hintBlink.Stop();
        pipPulse.Stop();
        if (canvas != null) canvas.gameObject.SetActive(false);
    }

    private void OnDestroy() => Hide();

    // ---- Construction ----

    private void Build()
    {
        if (canvas != null) return;
        if (font == null) font = TMP_Settings.defaultFontAsset;
        var go = new GameObject("Tutorial Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root = go.GetComponent<CanvasGroup>();
        root.interactable = root.blocksRaycasts = false;
        var parent = (RectTransform)go.transform;

        // Instruction card: ink border, paper panel, red title.
        card = Panel(parent, "Card", new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 170f), Ink);
        var paper = Panel(card, "Paper", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -16f), Paper, stretch: true);
        title = Label(paper, "Title", 64f, Red, TextAlignmentOptions.MidlineLeft);
        Place(title.rectTransform, new Vector2(0f, 0f), new Vector2(0.32f, 1f), new Vector2(28f, 0f), Vector2.zero);
        Outline(title, 0.25f);
        instruction = Label(paper, "Instruction", 34f, Ink, TextAlignmentOptions.MidlineLeft);
        Place(instruction.rectTransform, new Vector2(0.32f, 0f), new Vector2(1f, 1f), new Vector2(8f, 8f), new Vector2(-24f, -8f));

        // Step progress pips above the card.
        var strip = new GameObject("Progress", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        strip.SetParent(parent, false);
        Anchor(strip, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 36f));
        var layout = strip.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        hint = Label(parent, "Hint", 34f, Yellow, TextAlignmentOptions.Center);
        Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -275f), new Vector2(1200f, 60f));
        Outline(hint, 0.3f);
        hintGroup = hint.gameObject.AddComponent<CanvasGroup>();

        // Coach speech bubble, bottom left above the HUD bars.
        coachBubble = Panel(parent, "Coach", new Vector2(0f, 0f), new Vector2(60f, 300f), new Vector2(620f, 140f), Ink);
        var bubble = Panel(coachBubble, "Bubble", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, -12f), Color.white, stretch: true);
        coach = Label(bubble, "Line", 32f, Ink, TextAlignmentOptions.Center);
        Place(coach.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 10f), new Vector2(-18f, -10f));
        coachGroup = coachBubble.gameObject.AddComponent<CanvasGroup>();
        coachBubble.localEulerAngles = new Vector3(0f, 0f, 3f);

        pop = Label(parent, "Pop", 150f, Yellow, TextAlignmentOptions.Center);
        Anchor(pop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1000f, 220f));
        Outline(pop, 0.35f);
        popGroup = pop.gameObject.AddComponent<CanvasGroup>();

        // Skip: hold Escape fills the bar.
        var skip = Panel(parent, "Skip", new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(320f, 46f), Ink);
        skipGroup = skip.gameObject.AddComponent<CanvasGroup>();
        skipFill = Panel(skip, "Fill", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 0f), Red, stretch: true);
        skipFill.pivot = new Vector2(0f, 0.5f);
        skipLabel = Label(skip, "Label", 24f, Color.white, TextAlignmentOptions.Center);
        Place(skipLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        skipLabel.text = "HOLD ESC: SKIP";
    }

    private void BuildPips(int count)
    {
        var strip = canvas.transform.Find("Progress");
        foreach (var pip in pips) if (pip != null) Destroy(pip.transform.parent.gameObject);
        pips = new Image[count];
        for (int i = 0; i < count; i++)
        {
            var frame = Panel((RectTransform)strip, $"Pip {i + 1}", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f), Ink);
            frame.localEulerAngles = new Vector3(0f, 0f, i % 2 == 0 ? -6f : 6f);
            pips[i] = Panel(frame, "Fill", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-8f, -8f), Paper, stretch: true).GetComponent<Image>();
        }
    }

    private static RectTransform Panel(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color, bool stretch = false)
    {
        var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        var image = rect.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (stretch) Place(rect, Vector2.zero, Vector2.one, -size * 0.5f, size * 0.5f);
        else Anchor(rect, anchor, position, size);
        return rect;
    }

    private TextMeshProUGUI Label(RectTransform parent, string name, float size, Color color, TextAlignmentOptions alignment)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(parent, false);
        if (font != null) label.font = font;
        label.fontSize = size;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = alignment;
        label.enableAutoSizing = true;
        label.fontSizeMin = size * 0.4f;
        label.fontSizeMax = size;
        label.raycastTarget = false;
        return label;
    }

    private static void Outline(TextMeshProUGUI label, float width)
    {
        label.outlineColor = Ink;
        label.outlineWidth = width;
    }

    private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
