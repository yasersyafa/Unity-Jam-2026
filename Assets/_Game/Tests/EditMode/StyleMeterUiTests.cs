using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class StyleMeterUiTests
{
    [Test]
    public void GameplayRig_HasRightAnchoredStyleRankHudWithAllSixSpritesAndShader()
    {
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Arts/UI/ComboRankFill.mat");
        var backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Arts/UI/ComboBackgroung.png");

        Assert.That(rig, Is.Not.Null);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("Game/UI Combo Rank"));
        Assert.That(material.GetColor("_EmptyColor").a, Is.EqualTo(1f).Within(0.01f));
        Assert.That(material.GetColor("_EmptyColor").r, Is.GreaterThanOrEqualTo(0.35f));
        Assert.That(material.GetFloat("_EmptyDesaturation"), Is.LessThanOrEqualTo(0.25f));
        Assert.That(backgroundSprite, Is.Not.Null);

        StyleMeterUI styleUi = rig.GetComponentsInChildren<StyleMeterUI>(true).SingleOrDefault();
        Assert.That(styleUi, Is.Not.Null, "StyleMeterUI should exist inside GameplayRig.");
        Assert.That(styleUi.gameObject.name, Is.EqualTo("StyleRankHUD"));

        Canvas canvas = styleUi.GetComponentInParent<Canvas>(true);
        Assert.That(canvas, Is.Not.Null);
        Assert.That(canvas.gameObject.name, Is.EqualTo("CanvasHUD"));

        var rect = styleUi.GetComponent<RectTransform>();
        Assert.That(rect.anchorMin.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(rect.anchorMax.x, Is.EqualTo(1f).Within(0.001f));
        Assert.That(rect.anchoredPosition.x, Is.LessThan(0f));

        var serialized = new SerializedObject(styleUi);
        Assert.That(serialized.FindProperty("group").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("panelRoot").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("rankVisual").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("rankMaterial").objectReferenceValue, Is.SameAs(material));
        Assert.That(serialized.FindProperty("player").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("showTagline").boolValue, Is.False);
        Assert.That(serialized.FindProperty("taglineLabel").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("hitCountLabel").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("taglineLabel").objectReferenceValue.GetType().Name, Is.EqualTo("TextMeshProUGUI"));
        Assert.That(serialized.FindProperty("hitCountLabel").objectReferenceValue.GetType().Name, Is.EqualTo("TextMeshProUGUI"));

        var rankImage = serialized.FindProperty("rankImage").objectReferenceValue as Image;
        Assert.That(rankImage, Is.Not.Null);
        Assert.That(rankImage.material, Is.SameAs(material));

        Transform bgTransform = styleUi.transform.Find("ComboBackground");
        Assert.That(bgTransform, Is.Not.Null);
        Assert.That(bgTransform.gameObject.activeSelf, Is.False);
        Assert.That(bgTransform.GetComponent<Image>().sprite, Is.SameAs(backgroundSprite));

        string[] expectedSpriteNames =
        {
            "ComboRankSheets_1", // C
            "ComboRankSheets_2", // B
            "ComboRankSheets_3", // A
            "ComboRankSheets_0", // S
            "ComboRankSheets_4", // SS
            "ComboRankSheets_5", // SSS
        };

        SerializedProperty visuals = serialized.FindProperty("rankVisuals");
        Assert.That(visuals.arraySize, Is.EqualTo(expectedSpriteNames.Length));
        for (int i = 0; i < expectedSpriteNames.Length; i++)
        {
            SerializedProperty entry = visuals.GetArrayElementAtIndex(i);
            var sprite = entry.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
            Assert.That(sprite, Is.Not.Null, $"Missing sprite at rank index {i}");
            Assert.That(sprite.name, Is.EqualTo(expectedSpriteNames[i]));
            float bottom = entry.FindPropertyRelative("uvBottom").floatValue;
            float top = entry.FindPropertyRelative("uvTop").floatValue;
            Assert.That(top, Is.GreaterThan(bottom));
        }
    }
}
