using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class SlotAudioTests
{
    [TestCase(
        "Reel1",
        "Assets/_Game/ScriptableObjects/Audio/SlotStop1.asset",
        "Assets/_Game/Audio/SFX/Slot/Slot1.wav")]
    [TestCase(
        "Reel2",
        "Assets/_Game/ScriptableObjects/Audio/SlotStop2.asset",
        "Assets/_Game/Audio/SFX/Slot/Slot2.wav")]
    [TestCase(
        "Reel3",
        "Assets/_Game/ScriptableObjects/Audio/SlotStop3.asset",
        "Assets/_Game/Audio/SFX/Slot/Slot3.wav")]
    public void GameplayRigReel_HasOrderedSlotStopAudioCue(string reelName, string cuePath, string clipPath)
    {
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(cuePath);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

        Assert.That(rig, Is.Not.Null);
        Assert.That(cue, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);

        SlotAudio slotAudio = rig.GetComponentsInChildren<SlotAudio>(true)
            .FirstOrDefault(component => component.gameObject.name == reelName);
        Assert.That(slotAudio, Is.Not.Null, $"SlotAudio not found on {reelName}");

        var serialized = new SerializedObject(slotAudio);
        Assert.That(serialized.FindProperty("reel").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("stopped").objectReferenceValue, Is.SameAs(cue));

        Assert.That(cue.ui, Is.True);
        Assert.That(cue.cooldown, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(cue.maximumVoices, Is.GreaterThanOrEqualTo(1));
        Assert.That(cue.clips, Contains.Item(clip));
    }

    [Test]
    public void GameplayRigSpinToggle_HasSlotLeverAudioTriggerWiredToButtonClick()
    {
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/SlotLever.asset");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Slot/Slot lever.mp3");

        Assert.That(rig, Is.Not.Null);
        Assert.That(cue, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);

        AudioTrigger trigger = rig.GetComponentsInChildren<AudioTrigger>(true)
            .FirstOrDefault(component => component.gameObject.name == "SpinToggle");
        Assert.That(trigger, Is.Not.Null, "AudioTrigger not found on SpinToggle");

        var serialized = new SerializedObject(trigger);
        Assert.That(serialized.FindProperty("cue").objectReferenceValue, Is.SameAs(cue));

        Assert.That(cue.ui, Is.True);
        Assert.That(cue.cooldown, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(cue.clips, Contains.Item(clip));

        Button button = trigger.GetComponent<Button>();
        Assert.That(button, Is.Not.Null);
        bool callsTriggerPlay = false;
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (ReferenceEquals(button.onClick.GetPersistentTarget(i), trigger)
                && button.onClick.GetPersistentMethodName(i) == nameof(AudioTrigger.Play))
            {
                callsTriggerPlay = true;
                break;
            }
        }
        Assert.That(callsTriggerPlay, Is.True);
    }
}
