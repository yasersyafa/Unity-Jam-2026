using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SlotAudioPlayModeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject root;
    private AudioService service;
    private readonly List<Object> createdAssets = new List<Object>();
    private float previousTimeScale;
    private bool previousRunInBackground;

    private static void Set(object target, string field, object value)
    {
        var info = target.GetType().GetField(field, Private);
        Assert.That(info, Is.Not.Null, $"Field not found: {field} on {target.GetType().Name}");
        info.SetValue(target, value);
    }

    private AudioClip CreateClip(string name)
    {
        var clip = AudioClip.Create(name, 44100, 1, 44100, false);
        createdAssets.Add(clip);
        return clip;
    }

    private AudioCue CreateUiCue(string name, AudioClip clip)
    {
        var cue = ScriptableObject.CreateInstance<AudioCue>();
        cue.name = name;
        cue.clips = new[] { clip };
        cue.volume = 1f;
        cue.pitch = Vector2.one;
        cue.cooldown = 0f;
        cue.maximumVoices = 4;
        cue.ui = true;
        createdAssets.Add(cue);
        return cue;
    }

    private (SlotReelView reel, SlotAudio audio) CreateReel(string name, AudioCue stopCue)
    {
        var reelObject = new GameObject(name, typeof(RectTransform));
        reelObject.transform.SetParent(root.transform, false);
        reelObject.SetActive(false);

        var digits = new TextMeshProUGUI[7];
        for (int i = 0; i < digits.Length; i++)
        {
            var digitObject = new GameObject($"Digit_{i}", typeof(RectTransform));
            digitObject.transform.SetParent(reelObject.transform, false);
            var text = digitObject.AddComponent<TextMeshProUGUI>();
            if (text.fontSharedMaterial == null)
            {
                var shader = Shader.Find("TextMeshPro/Mobile/Distance Field") ?? Shader.Find("Hidden/Internal-Colored");
                var fallback = new Material(shader);
                createdAssets.Add(fallback);
                text.fontSharedMaterial = fallback;
            }
            digits[i] = text;
        }

        var reel = reelObject.AddComponent<SlotReelView>();
        Set(reel, "digits", digits);
        reel.ConfigureSnap(0.04f, 0.1f);
        reel.Initialize();

        var audio = reelObject.AddComponent<SlotAudio>();
        Set(audio, "reel", reel);
        Set(audio, "stopped", stopCue);

        reelObject.SetActive(true);
        return (reel, audio);
    }

    private AudioSource[] ActiveUiVoices()
    {
        return service.GetComponents<AudioSource>()
            .Where(source => !source.loop && source.isPlaying)
            .ToArray();
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        previousRunInBackground = Application.runInBackground;
        Time.timeScale = 1f;
        Application.runInBackground = true;

        if (AudioService.Instance != null)
            Object.Destroy(AudioService.Instance.gameObject);
        yield return null;

        service = AudioService.Ensure();
        root = new GameObject("SlotAudioTestRoot", typeof(RectTransform), typeof(Canvas));
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        Application.runInBackground = previousRunInBackground;
        if (root != null) Object.Destroy(root);
        if (service != null) Object.Destroy(service.gameObject);
        foreach (var asset in createdAssets)
        {
            if (asset != null) Object.Destroy(asset);
        }
        createdAssets.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SequentialReelStops_PlaySlot1Slot2Slot3InOrderOnSnap()
    {
        AudioClip clip1 = CreateClip("Slot1Clip");
        AudioClip clip2 = CreateClip("Slot2Clip");
        AudioClip clip3 = CreateClip("Slot3Clip");

        var (reel1, _) = CreateReel("Reel1", CreateUiCue("SlotStop1Cue", clip1));
        var (reel2, _) = CreateReel("Reel2", CreateUiCue("SlotStop2Cue", clip2));
        var (reel3, _) = CreateReel("Reel3", CreateUiCue("SlotStop3Cue", clip3));

        var stopOrder = new List<AudioClip>();
        reel1.Stopped += _ => stopOrder.Add(ActiveUiVoices().Select(v => v.clip).FirstOrDefault(c => c == clip1));
        reel2.Stopped += _ => stopOrder.Add(ActiveUiVoices().Select(v => v.clip).FirstOrDefault(c => c == clip2));
        reel3.Stopped += _ => stopOrder.Add(ActiveUiVoices().Select(v => v.clip).FirstOrDefault(c => c == clip3));

        reel1.Spin(0.08f, 3);
        reel2.Spin(0.18f, 5);
        reel3.Spin(0.28f, 7);

        Assert.That(ActiveUiVoices(), Is.Empty);

        float timeout = Time.realtimeSinceStartup + 2f;
        while ((reel1.IsSpinning || reel2.IsSpinning || reel3.IsSpinning) && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(reel1.IsSpinning || reel2.IsSpinning || reel3.IsSpinning, Is.False, "Reels timed out.");
        Assert.That(stopOrder, Is.EqualTo(new[] { clip1, clip2, clip3 }));

        AudioSource[] playing = ActiveUiVoices();
        Assert.That(playing.Length, Is.EqualTo(3));
        Assert.That(playing.All(source => source.outputAudioMixerGroup == service.uiGroup), Is.True);
    }

    [UnityTest]
    public IEnumerator SkipAfterFirstReelStopped_DoesNotReplayFirstAndPlaysRemainingOnSnap()
    {
        AudioClip clip1 = CreateClip("Slot1Clip");
        AudioClip clip2 = CreateClip("Slot2Clip");
        AudioClip clip3 = CreateClip("Slot3Clip");

        var (reel1, _) = CreateReel("Reel1", CreateUiCue("SlotStop1Cue", clip1));
        var (reel2, _) = CreateReel("Reel2", CreateUiCue("SlotStop2Cue", clip2));
        var (reel3, _) = CreateReel("Reel3", CreateUiCue("SlotStop3Cue", clip3));

        int reel1Stops = 0;
        int reel2Stops = 0;
        int reel3Stops = 0;
        reel1.Stopped += _ => reel1Stops++;
        reel2.Stopped += _ => reel2Stops++;
        reel3.Stopped += _ => reel3Stops++;

        reel1.Spin(0.06f, 2);
        reel2.Spin(0.60f, 4);
        reel3.Spin(0.70f, 6);

        float timeout = Time.realtimeSinceStartup + 2f;
        while (reel1.IsSpinning && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(reel1.IsSpinning, Is.False);
        Assert.That(reel2.IsSpinning, Is.True);
        Assert.That(reel3.IsSpinning, Is.True);
        Assert.That(reel1Stops, Is.EqualTo(1));
        Assert.That(ActiveUiVoices().Select(v => v.clip).ToArray(), Is.EqualTo(new[] { clip1 }));

        reel1.Skip();
        reel2.Skip();
        reel3.Skip();

        timeout = Time.realtimeSinceStartup + 2f;
        while ((reel2.IsSpinning || reel3.IsSpinning) && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(reel2.IsSpinning || reel3.IsSpinning, Is.False, "Skipped reels timed out.");
        Assert.That(reel1Stops, Is.EqualTo(1));
        Assert.That(reel2Stops, Is.EqualTo(1));
        Assert.That(reel3Stops, Is.EqualTo(1));

        AudioClip[] playingClips = ActiveUiVoices().Select(v => v.clip).ToArray();
        Assert.That(playingClips.Count(c => c == clip1), Is.EqualTo(1));
        Assert.That(playingClips.Count(c => c == clip2), Is.EqualTo(1));
        Assert.That(playingClips.Count(c => c == clip3), Is.EqualTo(1));
    }

    [Test]
    public void SpinButtonPress_PlaysSlotLeverCueOnUiMixer()
    {
        AudioClip leverClip = CreateClip("SlotLeverClip");
        AudioCue leverCue = CreateUiCue("SlotLeverCue", leverClip);

        var buttonObject = new GameObject("SpinToggle", typeof(RectTransform), typeof(Button), typeof(AudioTrigger));
        buttonObject.transform.SetParent(root.transform, false);

        var trigger = buttonObject.GetComponent<AudioTrigger>();
        Set(trigger, "cue", leverCue);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(trigger.Play);

        button.onClick.Invoke();

        AudioSource[] playing = ActiveUiVoices();
        Assert.That(playing.Length, Is.EqualTo(1));
        Assert.That(playing[0].clip, Is.SameAs(leverClip));
        Assert.That(playing[0].outputAudioMixerGroup, Is.EqualTo(service.uiGroup));
    }
}
