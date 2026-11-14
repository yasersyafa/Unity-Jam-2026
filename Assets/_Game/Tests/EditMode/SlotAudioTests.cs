using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class SlotAudioTests
{
    [TestCase("Reel1", "Assets/_Game/ScriptableObjects/Audio/SlotStop1.asset", 0)]
    [TestCase("Reel2", "Assets/_Game/ScriptableObjects/Audio/SlotStop2.asset", 1)]
    [TestCase("Reel3", "Assets/_Game/ScriptableObjects/Audio/SlotStop3.asset", 2)]
    public void GameplayRigReel_HasSlot1ChordAudioCueAndIndex(string reelName, string cuePath, int expectedIndex)
    {
        var rig = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(cuePath);
        var slot1Clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Slot/Slot1.wav");
        var chord = AssetDatabase.LoadAssetAtPath<SlotChordSettings>("Assets/_Game/ScriptableObjects/Audio/SlotChord.asset");

        Assert.That(rig, Is.Not.Null);
        Assert.That(cue, Is.Not.Null);
        Assert.That(slot1Clip, Is.Not.Null);
        Assert.That(chord, Is.Not.Null);

        SlotAudio slotAudio = rig.GetComponentsInChildren<SlotAudio>(true)
            .FirstOrDefault(component => component.gameObject.name == reelName);
        Assert.That(slotAudio, Is.Not.Null, $"SlotAudio not found on {reelName}");

        var serialized = new SerializedObject(slotAudio);
        Assert.That(serialized.FindProperty("reel").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("stopped").objectReferenceValue, Is.SameAs(cue));
        Assert.That(serialized.FindProperty("stopChord").objectReferenceValue, Is.SameAs(chord));
        Assert.That(serialized.FindProperty("chordNoteIndex").intValue, Is.EqualTo(expectedIndex));

        Assert.That(cue.ui, Is.True);
        Assert.That(cue.cooldown, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(cue.maximumVoices, Is.GreaterThanOrEqualTo(1));
        Assert.That(cue.pitch, Is.EqualTo(Vector2.one));
        Assert.That(cue.clips, Contains.Item(slot1Clip));
    }

    [Test]
    public void SlotChordSettings_CMajorFromC_ComputesExpectedTriadPitches()
    {
        var settings = ScriptableObject.CreateInstance<SlotChordSettings>();
        try
        {
            settings.Configure(SlotNoteRoot.C, SlotChordQuality.Major);

            Assert.That(settings.GetSemitoneOffset(0), Is.EqualTo(0));
            Assert.That(settings.GetSemitoneOffset(1), Is.EqualTo(4));
            Assert.That(settings.GetSemitoneOffset(2), Is.EqualTo(7));

            Assert.That(settings.GetPitchMultiplier(0), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(settings.GetPitchMultiplier(1), Is.EqualTo(Mathf.Pow(2f, 4f / 12f)).Within(0.0001f));
            Assert.That(settings.GetPitchMultiplier(2), Is.EqualTo(Mathf.Pow(2f, 7f / 12f)).Within(0.0001f));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    [Test]
    public void SlotChordSettings_GMajorAndMinorAndCustom_ShiftsReelOneAndIntervalNotes()
    {
        var settings = ScriptableObject.CreateInstance<SlotChordSettings>();
        try
        {
            settings.Configure(SlotNoteRoot.G, SlotChordQuality.Major);
            Assert.That(settings.GetSemitoneOffset(0), Is.EqualTo(7));
            Assert.That(settings.GetSemitoneOffset(1), Is.EqualTo(11));
            Assert.That(settings.GetSemitoneOffset(2), Is.EqualTo(14));

            settings.Configure(SlotNoteRoot.G, SlotChordQuality.Major, octaveOffset: -1);
            Assert.That(settings.GetSemitoneOffset(0), Is.EqualTo(-5));
            Assert.That(settings.GetSemitoneOffset(1), Is.EqualTo(-1));
            Assert.That(settings.GetSemitoneOffset(2), Is.EqualTo(2));

            settings.Configure(SlotNoteRoot.A, SlotChordQuality.Minor);
            Assert.That(settings.GetSemitoneOffset(0), Is.EqualTo(9));
            Assert.That(settings.GetSemitoneOffset(1), Is.EqualTo(12));
            Assert.That(settings.GetSemitoneOffset(2), Is.EqualTo(16));

            settings.ConfigureCustom(SlotNoteRoot.D, new Vector3Int(0, 5, 12));
            Assert.That(settings.GetSemitoneOffset(0), Is.EqualTo(2));
            Assert.That(settings.GetSemitoneOffset(1), Is.EqualTo(7));
            Assert.That(settings.GetSemitoneOffset(2), Is.EqualTo(14));
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
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
