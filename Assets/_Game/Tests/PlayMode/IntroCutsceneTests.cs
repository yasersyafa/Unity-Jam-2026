using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class IntroCutsceneTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private IntroCutsceneController controller;
    private GameObject first, second;
    private RectTransform[] slides;
    private Texture2D texture;
    private Sprite[] frames;
    private float originalTimeScale;
    private GameObject transitionRoot;
    private object previousTransition;
    private bool hadIntroPreference;
    private int previousIntroPreference;
    private MainMenu menu;

    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private T Get<T>(string name) => (T)typeof(IntroCutsceneController).GetField(name, Private).GetValue(controller);
    private void Apply(float time) => typeof(IntroCutsceneController).GetMethod("Apply", Private).Invoke(controller, new object[] { time });

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalTimeScale = Time.timeScale;
        hadIntroPreference = PlayerPrefs.HasKey("intro.completed");
        previousIntroPreference = PlayerPrefs.GetInt("intro.completed");
        PlayerPrefs.DeleteKey("intro.completed");
        root = new GameObject("Intro test", typeof(RectTransform), typeof(Canvas));
        root.SetActive(false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        first = new GameObject("Sequence 1", typeof(RectTransform));
        second = new GameObject("Sequence 2", typeof(RectTransform));
        first.transform.SetParent(root.transform, false);
        second.transform.SetParent(root.transform, false);
        controller = root.AddComponent<IntroCutsceneController>();
        menu = root.AddComponent<MainMenu>();
        Set(controller, "sequence1", first);
        Set(controller, "sequence2", second);
        slides = new RectTransform[11];
        for (int i = 0; i < slides.Length; i++)
        {
            var item = new GameObject("Slide " + i, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            item.transform.SetParent(i < 5 ? first.transform : second.transform, false);
            slides[i] = (RectTransform)item.transform;
            slides[i].anchoredPosition = new Vector2(25f, 30f);
            slides[i].localRotation = Quaternion.Euler(0f, 0f, 15f);
            item.GetComponent<CanvasGroup>().alpha = 0.8f;
            Set(controller, i < 5 ? "sequence1Slide" + (i + 1) : "sequence2Slide" + (i - 4), slides[i]);
        }
        texture = new Texture2D(2, 1);
        frames = new[] { Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero),
            Sprite.Create(texture, new Rect(1, 0, 1, 1), Vector2.zero) };
        Set(controller, "slide5Image", slides[9].GetComponent<Image>());
        Set(controller, "slide5Frames", frames);
        Set(controller, "framesPerSecond", 20f);
        Set(controller, "moveDuration", 0.1f);
        Set(controller, "fadeDuration", 0.1f);
        Set(controller, "sequence1Slide3Delay", 0.1f);
        Set(controller, "sequenceSwitchDelay", 0.1f);
        Set(controller, "sequence2Slide3Delay", 0.1f);
        Set(controller, "sequence2Slide6Delay", 0.1f);
        Set(controller, "finishDelay", 10f);
        root.SetActive(true);
        yield return null;
    }

    [Test]
    public void Timeline_RespectsSequenceOrderAndAuthoredPoses()
    {
        Assert.That(menu.StartScene, Is.EqualTo("IntroScene"));
        Get<Tween>("timeline").Stop();
        Apply(0f);
        Assert.That(first.activeSelf, Is.True);
        Assert.That(second.activeSelf, Is.False);
        Assert.That(slides[0].gameObject.activeSelf, Is.True);
        Assert.That(slides[1].anchoredPosition.x, Is.GreaterThan(25f));
        Assert.That(slides[2].gameObject.activeSelf, Is.False);
        Apply(Get<float>("firstFadeAt") + 0.05f);
        Assert.That(slides[1].anchoredPosition, Is.EqualTo(new Vector2(25f, 30f)));
        Assert.That(slides[2].GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.4f).Within(0.001f));
        Assert.That(slides[3].gameObject.activeSelf, Is.False);
        Assert.That(slides[4].gameObject.activeSelf, Is.False);
        float fadeEnd = Get<float>("firstFadeAt") + 0.1f;
        Apply(fadeEnd);
        Assert.That(slides[3].gameObject.activeSelf, Is.False);
        Apply(fadeEnd + 0.199f);
        Assert.That(slides[3].gameObject.activeSelf, Is.False);
        float fourthAt = fadeEnd + 0.2f;
        Apply(fourthAt);
        Assert.That(slides[3].gameObject.activeInHierarchy, Is.True);
        Assert.That(slides[4].gameObject.activeSelf, Is.False);
        Apply(fourthAt + 0.199f);
        Assert.That(slides[4].gameObject.activeSelf, Is.False);
        Apply(fourthAt + 0.2f);
        Assert.That(slides[4].gameObject.activeInHierarchy, Is.True);
        Assert.That(first.activeSelf, Is.True);
        for (int i = 3; i <= 4; i++)
        {
            Assert.That(slides[i].anchoredPosition, Is.EqualTo(new Vector2(25f, 30f)));
            Assert.That(slides[i].GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.8f));
            Assert.That(Quaternion.Angle(slides[i].localRotation, Quaternion.Euler(0, 0, 15)), Is.LessThan(0.01f));
        }
        Apply(fourthAt + 0.299f);
        Assert.That(first.activeSelf, Is.True);
        Apply(Get<float>("secondSequenceAt"));
        Assert.That(first.activeSelf, Is.False);
        Assert.That(second.activeSelf, Is.True);
        Assert.That(slides[5].gameObject.activeInHierarchy, Is.True);
        Assert.That(slides[7].gameObject.activeSelf, Is.False);
        Apply(Get<float>("secondMoveAt") + 0.05f);
        Assert.That(slides[7].gameObject.activeSelf, Is.True);
        Assert.That(slides[8].anchoredPosition.y, Is.LessThan(30f));
        Assert.That(slides[9].anchoredPosition.y, Is.LessThan(30f));
        Assert.That(Get<bool>("animatingSprites"), Is.False);
        Apply(Get<float>("spritesAt"));
        Assert.That(slides[8].anchoredPosition, Is.EqualTo(new Vector2(25f, 30f)));
        Assert.That(slides[9].anchoredPosition, Is.EqualTo(new Vector2(25f, 30f)));
        Assert.That(Quaternion.Angle(slides[8].localRotation, Quaternion.Euler(0, 0, 15)), Is.LessThan(0.01f));
        Assert.That(Get<bool>("animatingSprites"), Is.True);
        Assert.That(slides[10].gameObject.activeSelf, Is.False);
        Apply(Get<float>("lastFadeAt") + 0.05f);
        Assert.That(slides[10].GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.4f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator UnscaledPlayback_AnimatesSprites_AndDisableStopsOwnedTweens()
    {
        Time.timeScale = 0f;
        float deadline = Time.realtimeSinceStartup + 3f;
        while (!Get<bool>("animatingSprites") && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(Get<bool>("animatingSprites"), Is.True);
        var image = slides[9].GetComponent<Image>();
        var previous = image.sprite;
        deadline = Time.realtimeSinceStartup + 1f;
        while (image.sprite == previous && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(image.sprite, Is.Not.SameAs(previous));
        controller.enabled = false;
        Assert.That(IntroCutsceneController.Completed, Is.False, "An interrupted intro must play again.");
        Assert.That(menu.StartScene, Is.EqualTo("IntroScene"));
        Assert.That(Get<Tween>("timeline").isAlive, Is.False);
        Assert.That(Get<Tween>("spriteLoop").isAlive, Is.False);
        Assert.That(slides[8].anchoredPosition, Is.EqualTo(new Vector2(25f, 30f)));
    }

    [TestCase(0f)]
    [TestCase(1.05f)]
    [TestCase(1.4f)]
    public void Skip_StopsPlaybackAndIgnoresRepeatedRequests(float time)
    {
        InstallBusyTransition();
        Get<Tween>("timeline").Stop();
        Apply(time);
        controller.Skip();
        controller.Skip();
        Assert.That(IntroCutsceneController.Completed, Is.True);
        Assert.That(menu.StartScene, Is.EqualTo("GameScene"));
        Assert.That(Get<bool>("loading"), Is.True);
        Assert.That(Get<Tween>("timeline").isAlive, Is.False);
        Assert.That(Get<Tween>("spriteLoop").isAlive, Is.False);
    }

    [Test]
    public void TimelineCompletion_RequestsTransitionAndStopsSpriteLoop()
    {
        InstallBusyTransition();
        Get<Tween>("timeline").Complete();
        Assert.That(IntroCutsceneController.Completed, Is.True);
        Assert.That(menu.StartScene, Is.EqualTo("GameScene"));
        Assert.That(Get<bool>("loading"), Is.True);
        Assert.That(Get<Tween>("spriteLoop").isAlive, Is.False);
    }

    private void InstallBusyTransition()
    {
        // A busy transition prevents scene changes in this isolated lifecycle test.
        var instance = typeof(SceneTransition).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        previousTransition = instance.GetValue(null);
        transitionRoot = new GameObject("Test busy transition");
        var transition = transitionRoot.AddComponent<SceneTransition>();
        Set(transition, "busy", true);
        instance.SetValue(null, transition);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = originalTimeScale;
        if (hadIntroPreference) PlayerPrefs.SetInt("intro.completed", previousIntroPreference);
        else PlayerPrefs.DeleteKey("intro.completed");
        PlayerPrefs.Save();
        Object.Destroy(root);
        if (transitionRoot != null)
        {
            typeof(SceneTransition).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousTransition);
            Object.Destroy(transitionRoot);
        }
        foreach (var frame in frames) Object.Destroy(frame);
        Object.Destroy(texture);
        yield return null;
    }
}
