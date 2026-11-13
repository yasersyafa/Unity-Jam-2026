using NUnit.Framework;
using UnityEngine;

public sealed class BossIntroTests
{
    [Test]
    public void WheelPlaysTwoCyclesThenHoldsFinalSpriteAndResets()
    {
        var instance = Object.Instantiate(Resources.Load<BossIntroPresentation>("BossIntroPresentation"));
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var type = typeof(BossIntroPresentation);
        var image = ((RectTransform)type.GetField("portrait", flags).GetValue(instance)).GetComponent<UnityEngine.UI.Image>();
        var original = image.sprite;
        var texture = new Texture2D(3, 1);
        var first = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
        var second = Sprite.Create(texture, new Rect(1, 0, 1, 1), Vector2.zero);
        var final = Sprite.Create(texture, new Rect(2, 0, 1, 1), Vector2.zero);
        try
        {
            type.GetField("wheelFrames", flags).SetValue(instance, new[] { first, second });
            type.GetField("wheelRepeatCount", flags).SetValue(instance, 2);
            type.GetField("wheelFinalSprite", flags).SetValue(instance, final);
            var apply = type.GetMethod("ApplyWheel", flags);
            apply.Invoke(instance, new object[] { 0f }); Assert.That(image.sprite, Is.SameAs(first));
            apply.Invoke(instance, new object[] { 1.1f / 12 }); Assert.That(image.sprite, Is.SameAs(second));
            apply.Invoke(instance, new object[] { 2.1f / 12 }); Assert.That(image.sprite, Is.SameAs(first));
            apply.Invoke(instance, new object[] { 3.1f / 12 }); Assert.That(image.sprite, Is.SameAs(second));
            apply.Invoke(instance, new object[] { 4.1f / 12 }); Assert.That(image.sprite, Is.SameAs(final));
            apply.Invoke(instance, new object[] { 5f }); Assert.That(image.sprite, Is.SameAs(final));
            instance.CancelAndReset(); Assert.That(image.sprite, Is.SameAs(original));
        }
        finally
        {
            Object.DestroyImmediate(instance.gameObject);
            Object.DestroyImmediate(first); Object.DestroyImmediate(second); Object.DestroyImmediate(final);
            Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void PrefabIsReadyAndCancellationHidesOverlay()
    {
        var prefab = Resources.Load<BossIntroPresentation>("BossIntroPresentation");
        Assert.That(prefab, Is.Not.Null);
        var instance = Object.Instantiate(prefab);
        try
        {
            instance.CancelAndReset();
            instance.CancelAndReset();
            Assert.That(instance.GetComponent<CanvasGroup>().alpha, Is.Zero);
            Assert.That(instance.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
        }
        finally { Object.DestroyImmediate(instance.gameObject); }
    }
}
