using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[InitializeOnLoad]
public static class ProfilerCaptureExporter
{
    public sealed class Job
    {
        public string id, state, input, output, error;
        public int processed, total;
    }
    private static Job job;
    private static readonly Dictionary<string, ProfilerAnalysis.Marker> markers = new();
    private static readonly List<ProfilerAnalysis.Frame> frameRows = new();
    private static int next, end, first;
    private static double started;
    private const string StatusPath = "Temp/profiler_export_status.json";
    static ProfilerCaptureExporter()
    {
        AssemblyReloadEvents.beforeAssemblyReload += () => Finish("interrupted", "Domain reload interrupted export.");
        EditorApplication.quitting += () => Finish("interrupted", "Editor closed during export.");
    }
    public static Job GetStatus()
    {
        return job ?? (File.Exists(StatusPath) ? JsonConvert.DeserializeObject<Job>(File.ReadAllText(StatusPath)) : new Job { state = "idle" });
    }
    public static Job Cancel()
    {
        Finish("interrupted", "Export cancelled.");
        return GetStatus();
    }
    public static void ValidateRequest(string input, bool exporting, bool recording)
    {
        if (exporting) throw new InvalidOperationException("Export already running.");
        if (recording) throw new InvalidOperationException("Stop Profiler recording before loading a capture.");
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Capture path is required.", nameof(input));
        if (!File.Exists(input)) throw new FileNotFoundException("Capture not found.", input);
        if (new FileInfo(input).Length == 0) throw new InvalidDataException("Capture file is empty.");
    }
    private static bool RecordingActive()
    {
        if (ProfilerDriver.enabled) return true;
        const string path = "Temp/pipeline_profiler_record_status.json";
        if (!File.Exists(path)) return false;
        var status = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
        string state = (string)status["status"];
        return state == "queued" || state == "recording" || state == "pending";
    }
    public static Job Export(string inputPath, string outputDirectory = null)
    {
        ValidateRequest(inputPath, job != null && job.state == "running", RecordingActive());
        string input = Path.GetFullPath(inputPath);
        string output = Path.GetFullPath(outputDirectory ?? Path.Combine("Logs/ProfilerExports", Path.GetFileNameWithoutExtension(input) + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any()) throw new IOException("Output directory must be empty.");
        job = new Job { id = Guid.NewGuid().ToString("N"), state = "running", input = input, output = output };
        started = EditorApplication.timeSinceStartup;
        try
        {
            Directory.CreateDirectory(output);
            markers.Clear(); frameRows.Clear();
            if (!ProfilerDriver.LoadProfile(input, false)) throw new InvalidDataException("Unable to load capture.");
            first = next = ProfilerDriver.firstFrameIndex;
            end = ProfilerDriver.lastFrameIndex;
            if (first < 0 || end < first) throw new InvalidDataException("Capture contains no frames.");
            job.total = end - first + 1;
            EditorApplication.update += Tick;
            SaveStatus();
        }
        catch (Exception exception) { Finish("failed", exception.ToString()); }
        return job;
    }
    private static void Tick()
    {
        try
        {
            if (RecordingActive()) throw new InvalidOperationException("Recording started during export.");
            if (ProfilerDriver.firstFrameIndex != first || ProfilerDriver.lastFrameIndex != end)
                throw new InvalidOperationException("Profiler history changed during export.");
            if (EditorApplication.timeSinceStartup - started > 600) throw new TimeoutException("Export exceeded 600 seconds.");
            double deadline = EditorApplication.timeSinceStartup + 0.015;
            do { frameRows.Add(ReadFrame(next++)); job.processed++; }
            while (next <= end && EditorApplication.timeSinceStartup < deadline);
            if (next > end) WriteResults();
            else SaveStatus();
        }
        catch (Exception exception) { Finish("failed", exception.ToString()); }
    }
    private static ProfilerAnalysis.Frame ReadFrame(int index)
    {
        var frame = new ProfilerAnalysis.Frame { index = index, missingMainThreadReason = "Main Thread data unavailable.", missingGcReason = "GC.Alloc metadata unavailable." };
        long bytes = 0;
        bool foundGcMarker = false, missingMetadata = false;
        for (int threadIndex = 0; ; threadIndex++)
        using (var view = ProfilerDriver.GetRawFrameDataView(index, threadIndex))
        {
            if (!view.valid) break;
            if (view.threadName == "Main Thread") { frame.mainThreadMs = view.frameTimeMs; frame.missingMainThreadReason = null; }
            string thread = view.threadGroupName + "/" + view.threadName + "/" + view.threadId;
            int gcMarker = view.GetMarkerId("GC.Alloc");
            foundGcMarker |= gcMarker >= 0;
            var samples = new ProfilerAnalysis.Sample[view.sampleCount];
            bool incomplete = false;
            for (int i = 0; i < samples.Length; i++)
            {
                bool allocation = gcMarker >= 0 && view.GetSampleMarkerId(i) == gcMarker;
                long? allocated = null;
                if (allocation)
                {
                    if (view.GetSampleMetadataCount(i) > 0) { allocated = view.GetSampleMetadataAsLong(i, 0); bytes += allocated.Value; }
                    else { incomplete = true; missingMetadata = true; }
                }
                samples[i] = new ProfilerAnalysis.Sample { name = view.GetSampleName(i), descendants = view.GetSampleChildrenCountRecursive(i), milliseconds = view.GetSampleTimeMs(i), allocation = allocation, allocationBytes = allocated };
            }
            ProfilerAnalysis.Accumulate(thread, samples, markers, gcMarker >= 0 && !incomplete);
        }
        if (foundGcMarker && !missingMetadata) { frame.gcAllocBytes = bytes; frame.missingGcReason = null; }
        return frame;
    }
    public static double? Percentile(double[] sorted, double percentile) => ProfilerAnalysis.Percentile(sorted, percentile);
    public static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    private static string Number(double? value) => value?.ToString("R", CultureInfo.InvariantCulture) ?? "";
    private static string Integer(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
    private static void WriteResults()
    {
        double[] sorted = frameRows.Where(f => f.mainThreadMs.HasValue).Select(f => f.mainThreadMs.Value).OrderBy(x => x).ToArray();
        var ranked = markers.Values.OrderByDescending(x => x.selfMs).ThenBy(x => x.thread).ThenBy(x => x.name).ToArray();
        var summary = new
        {
            schemaVersion = 1, input = job.input, inputBytes = new FileInfo(job.input).Length,
            editorVersion = Application.unityVersion, exportedUtc = DateTime.UtcNow.ToString("O"),
            firstFrame = first, lastFrame = end, totalFrames = frameRows.Count, validFrames = sorted.Length,
            missingDurationReason = sorted.Length == 0 ? "Main Thread data unavailable." : null,
            averageMs = sorted.Length > 0 ? (double?)sorted.Average() : null,
            p50Ms = Percentile(sorted, .5), p95Ms = Percentile(sorted, .95), p99Ms = Percentile(sorted, .99),
            maximumMs = sorted.Length > 0 ? (double?)sorted[sorted.Length - 1] : null,
            heaviestFrames = ProfilerAnalysis.Heaviest(frameRows), markers = ranked,
            limitations = new[] { "CPU samples only; GPU and memory snapshots are not exported.", "Inclusive timings and allocations overlap; never sum nested markers or parallel thread durations as frame cost.", "Wait/VSync markers remain visible and are not compute cost.", "GC bytes are observed GC.Alloc sample metadata, not retained memory. Null means unavailable; marker allocation totals include only threads/frames with usable metadata.", "Capture target and Deep Profile state cannot be reliably inferred here; retain recording status alongside the input. Reader version is not capture version." }
        };
        using (var writer = new StreamWriter(Path.Combine(job.output, "frames.csv")))
        {
            writer.WriteLine("frame,mainThreadMs,gcAllocBytes,missingMainThreadReason,missingGcReason");
            foreach (var f in frameRows) writer.WriteLine(string.Join(",", f.index.ToString(CultureInfo.InvariantCulture), Number(f.mainThreadMs), Integer(f.gcAllocBytes), Csv(f.missingMainThreadReason), Csv(f.missingGcReason)));
        }
        using (var writer = new StreamWriter(Path.Combine(job.output, "markers.csv")))
        {
            writer.WriteLine("thread,marker,calls,inclusiveMs,selfMs,maximumMs,inclusiveGcBytes,selfGcBytes");
            foreach (var m in ranked) writer.WriteLine(string.Join(",", Csv(m.thread), Csv(m.name), m.calls.ToString(CultureInfo.InvariantCulture), Number(m.inclusiveMs), Number(m.selfMs), Number(m.maximumMs), Integer(m.inclusiveGcBytes), Integer(m.selfGcBytes)));
        }
        File.WriteAllText(Path.Combine(job.output, "summary.json"), JsonConvert.SerializeObject(summary, Formatting.Indented));
        Finish("completed", null);
    }
    private static void Finish(string state, string error)
    {
        if (job == null || job.state != "running") return;
        EditorApplication.update -= Tick;
        job.state = state; job.error = error; SaveStatus();
    }
    private static void SaveStatus()
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(StatusPath, JsonConvert.SerializeObject(job, Formatting.Indented));
    }
}
