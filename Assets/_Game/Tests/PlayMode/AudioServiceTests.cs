using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AudioServiceTests
{
    private AudioService service;
    private AudioClip clip;
    private AudioCue cue;
    private float previousTimeScale;
    private bool previousRunInBackground;

    [UnitySetUp] public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        if (AudioService.Instance != null) Object.Destroy(AudioService.Instance.gameObject);
        yield return null;
        service = AudioService.Ensure();
        clip = AudioClip.Create("Test clip", 441000, 1, 44100, false);
        cue = ScriptableObject.CreateInstance<AudioCue>();
        cue.clips = new[] { clip };
        // Let Unity finish initializing newly created AudioSources before observing playback.
        yield return null;
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        Application.runInBackground = previousRunInBackground;
        if (service != null) Object.Destroy(service.gameObject);
        Object.Destroy(cue);
        Object.Destroy(clip);
        yield return null;
    }
    private AudioSource[] Effects => service.GetComponents<AudioSource>().Where(s => !s.loop && s.isPlaying).ToArray();
    private AudioSource[] Music => service.GetComponents<AudioSource>().Where(s => s.loop).ToArray();

    [UnityTest] public IEnumerator Ensure_ReusesServiceAndRejectsDuplicate()
    {
        Assert.That(AudioService.Ensure(), Is.SameAs(service));
        var duplicate = new GameObject("Duplicate audio");
        duplicate.AddComponent<AudioService>();
        yield return null;
        Assert.That(duplicate == null, Is.True);
        Assert.That(AudioService.Instance, Is.SameAs(service));
    }
    [Test] public void Play_EnforcesCueLimitAndRoutesEffects()
    {
        cue.maximumVoices = 2;
        for (int i = 0; i < 40; i++) service.Play(cue);
        Assert.That(Effects.Length, Is.EqualTo(2));
        Assert.That(service.sfxGroup, Is.Not.Null);
        Assert.That(Effects.All(s => s.outputAudioMixerGroup == service.sfxGroup), Is.True);
    }
    [Test] public void Play_RoutesUiToUiMixer()
    {
        cue.ui = true;
        service.Play(cue);
        Assert.That(Effects.Length, Is.EqualTo(1));
        Assert.That(service.uiGroup, Is.Not.Null);
        Assert.That(Effects[0].outputAudioMixerGroup, Is.EqualTo(service.uiGroup));
    }
    [Test] public void EmptyCue_WarnsOnceWithoutStartingVoice()
    {
        cue.clips = new AudioClip[0];
        cue.name = "Empty test cue";
        LogAssert.Expect(LogType.Warning, "Audio cue has no clips: Empty test cue");
        service.Play(cue);
        service.Play(cue);
        service.Play(null);
        Assert.That(Effects, Is.Empty);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator Cooldown_ExpiresWhileGameplayPaused()
    {
        Time.timeScale = 0;
        cue.cooldown = 0.05f;
        service.Play(cue);
        service.Play(cue);
        Assert.That(Effects.Length, Is.EqualTo(1));
        yield return new WaitForSecondsRealtime(0.1f);
        service.Play(cue);
        Assert.That(Effects.Length, Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator Music_FadesAndStopsWhileGameplayPaused()
    {
        Time.timeScale = 0;
        service.PlayMusic(clip, 0.6f, 0.05f);
        yield return new WaitForSecondsRealtime(0.15f);
        var playing = Music.Single(s => s.isPlaying);
        Assert.That(playing.volume, Is.EqualTo(0.6f).Within(0.01f));
        Assert.That(playing.outputAudioMixerGroup, Is.EqualTo(service.musicGroup));
        service.PlayMusic(clip, 0.6f, 0.05f);
        Assert.That(Music.Count(s => s.isPlaying), Is.EqualTo(1));
        service.StopMusic(0.05f);
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.That(Music.Any(s => s.isPlaying), Is.False);
    }
}
