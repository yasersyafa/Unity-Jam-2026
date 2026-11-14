using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }
    public AudioMixerGroup musicGroup, sfxGroup, uiGroup;
    private readonly AudioSource[] voices = new AudioSource[40];
    private readonly AudioCue[] cues = new AudioCue[40];
    private readonly System.Collections.Generic.Dictionary<AudioCue, float> times = new();
    private readonly System.Collections.Generic.Dictionary<AudioCue, int> last = new();
    private readonly System.Collections.Generic.HashSet<AudioCue> warned = new();
    private const float LoopCrossfadeSeconds = 0.04f;
    private const float LoopStopFadeSeconds = 0.04f;
    private AudioSource[] music;
    private AudioSource[] loopSources;
    private int active;
    private int loopActive;
    private AudioCue loopCue;
    private AudioClip loopClip;
    private float loopPitch = 1f;
    private float loopStartTime;
    private float loopVolume;
    private float loopCrossfade;
    private double loopHandoffDsp;
    private double loopStopStartDsp;
    private float loopStopStartVol0;
    private float loopStopStartVol1;
    private bool loopScheduledNext;
    private bool loopPlaying;
    private bool loopStopping;
    private float elapsed, duration;
    private float[] starts = new float[2];
    private float target;

    public static AudioService Ensure()
    {
        if (Instance == null)
        {
            var prefab = Resources.Load<AudioService>("GameAudioService");
            if (prefab != null) Instantiate(prefab);
            else new GameObject("Audio Service").AddComponent<AudioService>();
        }
        return Instance;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        music = new[] { AddSource(), AddSource() };
        foreach (var source in music) source.loop = true;
        loopSources = new[] { AddSource(), AddSource() };
        for (int i = 0; i < voices.Length; i++) voices[i] = AddSource();
    }
    private AudioSource AddSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.Stop();
        source.spatialBlend = 0;
        return source;
    }
    private bool HasValidClips(AudioCue cue)
    {
        if (cue == null) return false;
        if (cue.clips == null || cue.clips.Length == 0 || System.Array.TrueForAll(cue.clips, entry => entry == null))
        {
            if (warned.Add(cue)) Debug.LogWarning("Audio cue has no clips: " + cue.name, cue);
            return false;
        }
        return true;
    }
    private bool TryPickClip(AudioCue cue, out AudioClip clip)
    {
        clip = null;
        if (!HasValidClips(cue)) return false;
        int index = Random.Range(0, cue.clips.Length);
        if (cue.clips.Length > 1 && last.TryGetValue(cue, out int previous) && index == previous)
            index = (index + Random.Range(1, cue.clips.Length)) % cue.clips.Length;
        clip = cue.clips[index];
        if (clip == null) return false;
        last[cue] = index;
        return true;
    }
    public void Play(AudioCue cue) => Play(cue, 1f);

    /// <summary>Plays a cue with an additional multiplicative pitch factor.</summary>
    public void Play(AudioCue cue, float pitchMultiplier)
    {
        if (!HasValidClips(cue)) return;
        if (times.TryGetValue(cue, out float time) && Time.unscaledTime - time < cue.cooldown) return;
        int count = 0, free = -1;
        for (int i = 0; i < voices.Length; i++)
            if (voices[i].isPlaying && voices[i].clip != null) { if (cues[i] == cue) count++; }
            else free = i;
        if (free < 0 || count >= cue.maximumVoices) return;
        if (!TryPickClip(cue, out var clip)) return;
        times[cue] = Time.unscaledTime;
        cues[free] = cue;
        var source = voices[free];
        source.outputAudioMixerGroup = cue.ui ? uiGroup : sfxGroup;
        source.clip = clip;
        source.volume = cue.volume;
        float safeMultiplier = pitchMultiplier > 0f ? pitchMultiplier : 1f;
        source.pitch = Mathf.Clamp(Random.Range(cue.pitch.x, cue.pitch.y) * safeMultiplier, 0.1f, 3f);
        source.Play();
    }
    public void StartLoop(AudioCue cue)
    {
        if (loopPlaying && !loopStopping && loopCue == cue) return;
        if (!TryPickClip(cue, out var clip)) return;
        loopCue = cue;
        loopClip = clip;
        loopVolume = cue.volume;
        loopPitch = Mathf.Clamp(Random.Range(cue.pitch.x, cue.pitch.y), 0.1f, 3);
        loopStartTime = cue.loopStartSeconds > 0f && cue.loopStartSeconds < clip.length - 0.05f ? cue.loopStartSeconds : 0f;
        float segmentSeconds = (clip.length - loopStartTime) / loopPitch;
        loopCrossfade = Mathf.Min(LoopCrossfadeSeconds, segmentSeconds * 0.25f);
        loopActive = 0;
        loopScheduledNext = false;
        loopStopping = false;
        loopPlaying = true;
        loopSources[1].Stop();
        var current = loopSources[0];
        current.Stop();
        current.outputAudioMixerGroup = cue.ui ? uiGroup : sfxGroup;
        current.clip = clip;
        current.pitch = loopPitch;
        current.volume = loopVolume;
        current.time = 0f;
        double now = AudioSettings.dspTime;
        current.Play();
        loopHandoffDsp = now + (clip.length / loopPitch) - loopCrossfade;
    }
    public void StopLoop(AudioCue cue = null)
    {
        if (!loopPlaying || loopStopping) return;
        if (cue != null && loopCue != cue) return;
        loopStopping = true;
        loopScheduledNext = false;
        loopStopStartDsp = AudioSettings.dspTime;
        loopStopStartVol0 = loopSources[0].volume;
        loopStopStartVol1 = loopSources[1].volume;
    }
    public void PlayMusic(AudioClip clip, float volume = 1, float fadeSeconds = 1)
    {
        if (clip != null && music[active].clip == clip && music[active].isPlaying) return;
        active = 1 - active;
        music[active].Stop();
        music[active].clip = clip;
        music[active].volume = 0;
        music[active].outputAudioMixerGroup = musicGroup;
        if (clip != null) music[active].Play();
        BeginFade(clip == null ? 0 : volume, fadeSeconds);
    }
    public void StopMusic(float fadeSeconds = 1) { BeginFade(0, fadeSeconds); }
    private void BeginFade(float volume, float seconds)
    {
        for (int i = 0; i < 2; i++) starts[i] = music[i].volume;
        target = Mathf.Clamp01(volume);
        elapsed = 0;
        duration = Mathf.Max(0.001f, seconds);
    }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.001f));
        for (int i = 0; i < 2; i++)
        {
            music[i].volume = Mathf.Lerp(starts[i], i == active ? target : 0, t);
            if (t == 1 && music[i].volume == 0) music[i].Stop();
        }
        UpdateLoop();
    }
    private void UpdateLoop()
    {
        if (!loopPlaying) return;
        double now = AudioSettings.dspTime;
        if (loopStopping)
        {
            float fadeOut = Mathf.Clamp01((float)((now - loopStopStartDsp) / LoopStopFadeSeconds));
            loopSources[0].volume = Mathf.Lerp(loopStopStartVol0, 0f, fadeOut);
            loopSources[1].volume = Mathf.Lerp(loopStopStartVol1, 0f, fadeOut);
            if (fadeOut >= 1f)
            {
                loopSources[0].Stop();
                loopSources[1].Stop();
                loopPlaying = false;
                loopStopping = false;
                loopCue = null;
                loopClip = null;
            }
            return;
        }
        if (loopClip == null || loopCue == null) { StopLoop(); return; }
        if (!loopScheduledNext && now >= loopHandoffDsp - 0.1)
        {
            var incoming = loopSources[1 - loopActive];
            incoming.Stop();
            incoming.outputAudioMixerGroup = loopCue.ui ? uiGroup : sfxGroup;
            incoming.clip = loopClip;
            incoming.pitch = loopPitch;
            incoming.volume = 0f;
            int startSample = loopStartTime > 0f
                ? Mathf.Clamp(Mathf.RoundToInt(loopStartTime * loopClip.frequency), 0, loopClip.samples - 1)
                : 0;
            incoming.timeSamples = startSample;
            if (loopHandoffDsp <= now) loopHandoffDsp = now + 0.005;
            incoming.PlayScheduled(loopHandoffDsp);
            loopScheduledNext = true;
        }
        if (loopScheduledNext && now >= loopHandoffDsp)
        {
            float u = loopCrossfade > 0.0001f ? Mathf.Clamp01((float)((now - loopHandoffDsp) / loopCrossfade)) : 1f;
            var outgoing = loopSources[loopActive];
            var incoming = loopSources[1 - loopActive];
            outgoing.volume = Mathf.Lerp(loopVolume, 0f, u);
            incoming.volume = Mathf.Lerp(0f, loopVolume, u);
            if (u >= 1f)
            {
                outgoing.Stop();
                incoming.volume = loopVolume;
                loopActive = 1 - loopActive;
                loopScheduledNext = false;
                double nextDuration = (loopClip.length - loopStartTime) / loopPitch;
                loopHandoffDsp += System.Math.Max(0.02, nextDuration - loopCrossfade);
            }
        }
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
