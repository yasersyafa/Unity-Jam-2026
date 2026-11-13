using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class SlotCooldownIconTests
{
    [Test]
    public void GameplayRigSlotDockTarget_HasBottomIconSpriteShaderAndCooldownUi()
    {
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Arts/UI/SlotMachineIcon.png");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Arts/UI/SlotMachineIconFill.mat");

        Assert.That(rig, Is.Not.Null);
        Assert.That(iconSprite, Is.Not.Null);
        Assert.That(material, Is.Not.Null);
        Assert.That(material.shader, Is.Not.Null);
        Assert.That(material.shader.name, Is.EqualTo("Game/UI Combo Rank"));
        Assert.That(material.GetColor("_EmptyColor").a, Is.EqualTo(1f).Within(0.01f));
        Assert.That(material.GetColor("_EmptyColor").r, Is.GreaterThanOrEqualTo(0.35f));
        Assert.That(material.GetFloat("_EmptyDesaturation"), Is.LessThanOrEqualTo(0.25f));

        Transform dockTransform = rig.transform.Find("CanvasHUD/SlotDockTarget");
        Assert.That(dockTransform, Is.Not.Null, "CanvasHUD/SlotDockTarget not found inside GameplayRig.");

        var rect = dockTransform.GetComponent<RectTransform>();
        Assert.That(rect.anchorMin.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(rect.anchorMax.y, Is.EqualTo(0f).Within(0.001f));

        var image = dockTransform.GetComponent<Image>();
        Assert.That(image, Is.Not.Null);
        Assert.That(image.sprite, Is.SameAs(iconSprite));
        Assert.That(image.material, Is.SameAs(material));
        Assert.That(image.preserveAspect, Is.True);

        var iconUi = dockTransform.GetComponent<SlotCooldownIconUI>();
        Assert.That(iconUi, Is.Not.Null);

        var serialized = new SerializedObject(iconUi);
        Assert.That(serialized.FindProperty("player").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("visualRoot").objectReferenceValue, Is.SameAs(rect));
        Assert.That(serialized.FindProperty("iconImage").objectReferenceValue, Is.SameAs(image));
        Assert.That(serialized.FindProperty("iconMaterial").objectReferenceValue, Is.SameAs(material));
    }
}
