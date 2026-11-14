using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Plays the two authored intro panels. Assign slides in Inspector; their transforms are the final poses.</summary>
[DisallowMultipleComponent]
public sealed class IntroCutsceneController : MonoBehaviour
{
    private const string CompletedKey = "intro.completed";
    public static bool Completed => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

    [Header("Sequence 1")]
    [SerializeField] private GameObject sequence1;
    [SerializeField] private RectTransform sequence1Slide1;
    [SerializeField] private RectTransform sequence1Slide2;
    [SerializeField] private RectTransform sequence1Slide3;
    [SerializeField] private RectTransform sequence1Slide4;
    [SerializeField] private RectTransform sequence1Slide5;

    [Header("Sequence 2")]
    [SerializeField] private GameObject sequence2;
    [SerializeField] private RectTransform sequence2Slide1;
    [SerializeField] private RectTransform sequence2Slide2;
    [SerializeField] private RectTransform sequence2Slide3;
    [SerializeField] private RectTransform sequence2Slide4;
    [SerializeField] private RectTransform sequence2Slide5;
    [SerializeField] private RectTransform sequence2Slide6;

    [Header("Slide 5 Spritesheet")]
    [SerializeField] private Image slide5Image;
    [SerializeField] private Sprite[] slide5Frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;

    [Header("Timing (Unscaled Seconds)")]
    [SerializeField, Min(0.01f)] private float moveDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.3f;
    [SerializeField, Min(0f)] private float sequence1Slide3Delay = 0.2f;
    [SerializeField, Min(0f)] private float sequenceSwitchDelay = 0.5f;
    [SerializeField, Min(0f)] private float sequence1Slide4Delay = 0.2f;
    [SerializeField, Min(0f)] private float sequence1Slide5Delay = 0.2f;
    [SerializeField, Min(0f)] private float sequence2Slide3Delay = 0.2f;
    [SerializeField, Min(0f)] private float sequence2Slide6Delay = 0.2f;
    [SerializeField, Min(0f)] private float finishDelay = 0.5f;
    [SerializeField] private string destinationScene = "GameScene";

    private sealed class Slide
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 position;
        public Quaternion rotation;
        public float alpha;
        public Vector2 entry;
    }

    private Slide[] slides;
    private Tween timeline;
    private Tween spriteLoop;
    private Sprite initialSprite;
    private bool loading;
    private bool animatingSprites;
    private bool started;
    private float firstFadeAt, firstSlide4At, firstSlide5At, secondSequenceAt, secondMoveAt, spritesAt, lastFadeAt, endAt;

    private void Start()
    {
        started = true;
        if (!ValidateReferences()) return;
        var rects = new[] { sequence1Slide1, sequence1Slide2, sequence1Slide3, sequence1Slide4, sequence1Slide5,
            sequence2Slide1, sequence2Slide2, sequence2Slide3, sequence2Slide4, sequence2Slide5, sequence2Slide6 };
        slides = new Slide[rects.Length];
        Canvas.ForceUpdateCanvases();
        for (int i = 0; i < rects.Length; i++)
        {
            var rect = rects[i];
            var group = rect.GetComponent<CanvasGroup>() ?? rect.gameObject.AddComponent<CanvasGroup>();
            slides[i] = new Slide { rect = rect, group = group, position = rect.anchoredPosition,
                rotation = rect.localRotation, alpha = group.alpha };
        }
        slides[1].entry = EntryOffset(slides[1].rect, true);
        slides[8].entry = EntryOffset(slides[8].rect, false);
        slides[9].entry = EntryOffset(slides[9].rect, false);
        initialSprite = slide5Image.sprite;
        firstFadeAt = moveDuration + sequence1Slide3Delay;
        firstSlide4At = firstFadeAt + fadeDuration + sequence1Slide4Delay;
        firstSlide5At = firstSlide4At + sequence1Slide5Delay;
        secondSequenceAt = firstSlide5At + sequenceSwitchDelay;
        secondMoveAt = secondSequenceAt + fadeDuration + sequence2Slide3Delay;
        spritesAt = secondMoveAt + moveDuration;
        lastFadeAt = spritesAt + sequence2Slide6Delay;
        endAt = lastFadeAt + fadeDuration + finishDelay;
        Apply(0f);
        timeline = Tween.Custom(0f, endAt, endAt, Apply, Ease.Linear, useUnscaledTime: true)
            .OnComplete(Skip);
    }

    private bool ValidateReferences()
    {
        if (sequence1 == null || sequence2 == null || sequence1 == sequence2 ||
            transform.IsChildOf(sequence1.transform) || transform.IsChildOf(sequence2.transform) ||
            sequence1.transform.IsChildOf(sequence2.transform) || sequence2.transform.IsChildOf(sequence1.transform))
            return Fail("Assign two separate sequence roots and place the controller outside both roots.");
        var rects = new[] { sequence1Slide1, sequence1Slide2, sequence1Slide3, sequence1Slide4, sequence1Slide5,
            sequence2Slide1, sequence2Slide2, sequence2Slide3, sequence2Slide4, sequence2Slide5, sequence2Slide6 };
        for (int i = 0; i < rects.Length; i++)
        {
            var root = i < 5 ? sequence1.transform : sequence2.transform;
            if (rects[i] == null || rects[i] == root || !rects[i].IsChildOf(root) || rects[i].GetComponentInParent<Canvas>(true) == null)
                return Fail($"Assign slide {i + 1} under its sequence root and a Canvas.");
            for (int j = 0; j < i; j++)
                if (rects[i] == rects[j]) return Fail("Each slide must have a unique reference.");
        }
        if (slide5Image == null || !slide5Image.transform.IsChildOf(sequence2Slide5) ||
            slide5Frames == null || slide5Frames.Length == 0 || Array.Exists(slide5Frames, frame => frame == null))
            return Fail("Assign the slide 5 Image and a nonempty list of sliced sprites.");
        if (string.IsNullOrWhiteSpace(destinationScene)) return Fail("Assign the destination scene name.");
        return true;
    }

    private bool Fail(string message)
    {
        Debug.LogError($"IntroCutsceneController: {message}", this);
        enabled = false;
        return false;
    }

    // Convert Canvas edge distances to the slide parent's coordinates, preserving the authored anchors.
    private static Vector2 EntryOffset(RectTransform rect, bool fromRight)
    {
        var canvas = (RectTransform)rect.GetComponentInParent<Canvas>(true).rootCanvas.transform;
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        float minX = float.PositiveInfinity, maxY = float.NegativeInfinity;
        foreach (var corner in corners)
        {
            var local = canvas.InverseTransformPoint(corner);
            minX = Mathf.Min(minX, local.x);
            maxY = Mathf.Max(maxY, local.y);
        }
        Vector3 offset = fromRight
            ? Vector3.right * Mathf.Max(0f, canvas.rect.xMax - minX + 1f)
            : Vector3.down * Mathf.Max(0f, maxY - canvas.rect.yMin + 1f);
        return rect.parent.InverseTransformVector(canvas.TransformVector(offset));
    }

    private void Apply(float time)
    {
        if (loading || slides == null) return;
        SetActive(sequence1, time < secondSequenceAt);
        SetActive(sequence2, time >= secondSequenceAt);
        Show(0, true);
        Show(1, true);
        Move(1, time / moveDuration);
        Fade(2, time, firstFadeAt);
        Show(3, time >= firstSlide4At);
        Show(4, time >= firstSlide5At);
        Show(5, true);
        Fade(6, time, secondSequenceAt);
        Show(7, time >= secondMoveAt);
        float movement = Mathf.Clamp01((time - secondMoveAt) / moveDuration);
        Show(8, time >= secondMoveAt);
        Show(9, time >= secondMoveAt);
        Move(8, movement);
        Move(9, movement);
        slides[8].rect.localRotation = slides[8].rotation * Quaternion.Euler(0f, 0f, 360f * (1f - movement));
        if (time >= spritesAt && !animatingSprites) StartSprites();
        Fade(10, time, lastFadeAt);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target.activeSelf != active) target.SetActive(active);
    }

    private void Show(int index, bool visible) => SetActive(slides[index].rect.gameObject, visible);

    private void Move(int index, float progress)
    {
        float eased = Easing.Evaluate(Mathf.Clamp01(progress), Ease.OutCubic);
        slides[index].rect.anchoredPosition = slides[index].position + slides[index].entry * (1f - eased);
    }

    private void Fade(int index, float time, float start)
    {
        Show(index, time >= start);
        slides[index].group.alpha = slides[index].alpha * Mathf.Clamp01((time - start) / fadeDuration);
    }

    private void StartSprites()
    {
        animatingSprites = true;
        slide5Image.sprite = slide5Frames[0];
        spriteLoop = Tween.Custom(0f, (float)slide5Frames.Length, slide5Frames.Length / framesPerSecond,
            frame => slide5Image.sprite = slide5Frames[Mathf.Min(Mathf.FloorToInt(frame), slide5Frames.Length - 1)],
            Ease.Linear, cycles: -1, cycleMode: CycleMode.Restart, useUnscaledTime: true);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Skip();
    }

    /// <summary>Connect a UI button's OnClick to this method. Escape uses the same path.</summary>
    public void Skip()
    {
        if (!isActiveAndEnabled || slides == null || loading) return;
        loading = true;
        StopAnimations();
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
        SceneTransition.Load(destinationScene);
    }

    private void StopAnimations()
    {
        timeline.Stop();
        spriteLoop.Stop();
    }

    private void OnDisable()
    {
        StopAnimations();
        if (slides == null) return;
        foreach (var slide in slides)
        {
            if (slide.rect == null) continue;
            slide.rect.anchoredPosition = slide.position;
            slide.rect.localRotation = slide.rotation;
            slide.group.alpha = slide.alpha;
        }
        if (slide5Image != null) slide5Image.sprite = initialSprite;
    }

    private void OnEnable()
    {
        if (!started || loading) return;
        animatingSprites = false;
        Start();
    }

    private void OnDestroy() => StopAnimations();

    private void OnValidate()
    {
        moveDuration = Mathf.Max(0.01f, moveDuration);
        fadeDuration = Mathf.Max(0.01f, fadeDuration);
        framesPerSecond = Mathf.Max(1f, framesPerSecond);
        sequence1Slide3Delay = Mathf.Max(0f, sequence1Slide3Delay);
        sequenceSwitchDelay = Mathf.Max(0f, sequenceSwitchDelay);
        sequence1Slide4Delay = Mathf.Max(0f, sequence1Slide4Delay);
        sequence1Slide5Delay = Mathf.Max(0f, sequence1Slide5Delay);
        sequence2Slide3Delay = Mathf.Max(0f, sequence2Slide3Delay);
        sequence2Slide6Delay = Mathf.Max(0f, sequence2Slide6Delay);
        finishDelay = Mathf.Max(0f, finishDelay);
    }
}
