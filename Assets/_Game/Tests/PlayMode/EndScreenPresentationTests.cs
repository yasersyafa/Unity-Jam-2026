using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class EndScreenPresentationTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static T GetField<T>(object target, string name) =>
        (T)target.GetType().GetField(name, Private).GetValue(target);

    [Test]
    public void Prefab_HasWinLoseSpritesAndButtonsWired_AndSlideAndFadeAndIdleAnimateCorrectly()
    {
        var prefab = Resources.Load<EndScreenPresentation>("EndScreenPresentation");
        Assert.That(prefab, Is.Not.Null, "Missing Resources/EndScreenPresentation.prefab");

        var instance = Object.Instantiate(prefab);
        try
        {
            var rootGroup = GetField<CanvasGroup>(instance, "root");
            var panelRect = GetField<RectTransform>(instance, "panelRect");
            var panelImage = GetField<Image>(instance, "panelImage");
            var titleRect = GetField<RectTransform>(instance, "titleRect");
            var titleImage = GetField<Image>(instance, "titleImage");
            var titleGroup = GetField<CanvasGroup>(instance, "titleGroup");
            var retryBtn = GetField<Button>(instance, "retryButton");
            var mainMenuBtn = GetField<Button>(instance, "mainMenuButton");
            var winPanel = GetField<Sprite>(instance, "winPanelSprite");
            var losePanel = GetField<Sprite>(instance, "losePanelSprite");
            var victoryTitle = GetField<Sprite>(instance, "victoryTitleSprite");
            var loseTitle = GetField<Sprite>(instance, "loseTitleSprite");

            Assert.That(winPanel, Is.Not.Null);
            Assert.That(losePanel, Is.Not.Null);
            Assert.That(victoryTitle, Is.Not.Null);
            Assert.That(loseTitle, Is.Not.Null);
            Assert.That(retryBtn, Is.Not.Null);
            Assert.That(mainMenuBtn, Is.Not.Null);

            var retryComic = retryBtn.GetComponent<ComicButton>();
            var mainMenuComic = mainMenuBtn.GetComponent<ComicButton>();
            Assert.That(retryComic, Is.Not.Null);
            Assert.That(mainMenuComic, Is.Not.Null);
            Assert.That(GetField<float>(retryComic, "hoverTilt"), Is.EqualTo(10f).Within(0.001f));
            Assert.That(GetField<float>(mainMenuComic, "hoverTilt"), Is.EqualTo(-10f).Within(0.001f));

            // Victory sequence: panel starts off to the right, slides to center, then title fades in
            instance.ShowVictory();
            Assert.That(instance.IsShowing, Is.True);
            Assert.That(instance.IsVictory, Is.True);
            Assert.That(rootGroup.alpha, Is.EqualTo(1f).Within(0.001f));
            Assert.That(panelImage.sprite, Is.SameAs(winPanel));
            Assert.That(titleImage.sprite, Is.SameAs(victoryTitle));

            instance.ApplyEntrance(0f);
            Assert.That(panelRect.anchoredPosition.x, Is.GreaterThan(1500f));
            Assert.That(titleGroup.alpha, Is.EqualTo(0f).Within(0.001f));

            instance.ApplyEntrance(0.22f);
            Assert.That(panelRect.anchoredPosition.x, Is.EqualTo(0f).Within(1f));

            instance.ApplyEntrance(instance.TotalEntranceDuration);
            Assert.That(titleGroup.alpha, Is.EqualTo(1f).Within(0.001f));

            // Lose sequence swaps to LosePanel + Lose title
            instance.ShowLose();
            Assert.That(instance.IsVictory, Is.False);
            Assert.That(panelImage.sprite, Is.SameAs(losePanel));
            Assert.That(titleImage.sprite, Is.SameAs(loseTitle));

            // Idle animation ("kedat-kedut") varies scale and tilt over time
            instance.ApplyEntrance(instance.TotalEntranceDuration);
            instance.EvaluateIdlePose(0f, out Vector3 scale0, out float tilt0);
            instance.EvaluateIdlePose(0.08f, out Vector3 scalePeak, out float tiltPeak);
            Assert.That(scalePeak.x, Is.GreaterThan(scale0.x));
            Assert.That(Mathf.Abs(tiltPeak - tilt0), Is.GreaterThan(0.1f));

            instance.CancelAndReset();
            Assert.That(rootGroup.alpha, Is.Zero);
            Assert.That(rootGroup.blocksRaycasts, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(instance.gameObject);
        }
    }

    [UnityTest]
    public IEnumerator RetryAndMainMenuButtons_Rotate10AndMinus10DegreesOnPointerHover()
    {
        var prefab = Resources.Load<EndScreenPresentation>("EndScreenPresentation");
        var instance = Object.Instantiate(prefab);
        try
        {
            instance.ShowVictory();
            instance.ApplyEntrance(instance.TotalEntranceDuration);

            var retryBtn = GetField<Button>(instance, "retryButton");
            var mainMenuBtn = GetField<Button>(instance, "mainMenuButton");
            var retryComic = retryBtn.GetComponent<ComicButton>();
            var mainMenuComic = mainMenuBtn.GetComponent<ComicButton>();

            var pointer = new PointerEventData(EventSystem.current);
            retryComic.OnPointerEnter(pointer);
            mainMenuComic.OnPointerEnter(pointer);

            yield return new WaitForSecondsRealtime(0.22f);

            float retryZ = Mathf.DeltaAngle(0f, retryBtn.transform.localEulerAngles.z);
            float mainMenuZ = Mathf.DeltaAngle(0f, mainMenuBtn.transform.localEulerAngles.z);
            Assert.That(retryZ, Is.EqualTo(10f).Within(0.5f));
            Assert.That(mainMenuZ, Is.EqualTo(-10f).Within(0.5f));

            retryComic.OnPointerExit(pointer);
            mainMenuComic.OnPointerExit(pointer);
        }
        finally
        {
            Object.Destroy(instance.gameObject);
        }
        yield return null;
    }
}
