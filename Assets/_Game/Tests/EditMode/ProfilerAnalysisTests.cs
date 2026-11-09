using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

public sealed class ProfilerAnalysisTests
{
    [Test] public void NestedSamples_ExcludeChildrenFromSelfTimeAndAttributeAllocations()
    {
        var markers = new Dictionary<string, ProfilerAnalysis.Marker>();
        ProfilerAnalysis.Accumulate("main", new[]
        {
            new ProfilerAnalysis.Sample { name = "Root", descendants = 3, milliseconds = 10 },
            new ProfilerAnalysis.Sample { name = "Nested", descendants = 1, milliseconds = 4 },
            new ProfilerAnalysis.Sample { name = "GC.Alloc", allocation = true, allocationBytes = 32 },
            new ProfilerAnalysis.Sample { name = "GC.Alloc", allocation = true, allocationBytes = 16 }
        }, markers, true);
        var root = markers.Values.Single(m => m.name == "Root");
        var nested = markers.Values.Single(m => m.name == "Nested");
        Assert.That(root.selfMs, Is.EqualTo(6));
        Assert.That(root.inclusiveGcBytes, Is.EqualTo(48));
        Assert.That(root.selfGcBytes, Is.EqualTo(16));
        Assert.That(nested.selfGcBytes, Is.EqualTo(32));
        Assert.That(markers.Values.Single(m => m.name == "GC.Alloc").calls, Is.EqualTo(2));
    }
    [Test] public void SameMarkerOnDifferentThreads_RemainsSeparate()
    {
        var markers = new Dictionary<string, ProfilerAnalysis.Marker>();
        var samples = new[] { new ProfilerAnalysis.Sample { name = "Update", milliseconds = 3 } };
        ProfilerAnalysis.Accumulate("main", samples, markers, false);
        ProfilerAnalysis.Accumulate("worker", samples, markers, false);
        Assert.That(markers.Count, Is.EqualTo(2));
        Assert.That(markers.Values.All(m => m.inclusiveGcBytes == null), Is.True);
    }
    [Test] public void MissingMetadata_RemainsUnknownRatherThanZero()
    {
        var markers = new Dictionary<string, ProfilerAnalysis.Marker>();
        ProfilerAnalysis.Accumulate("main", new[] { new ProfilerAnalysis.Sample { allocation = true } }, markers, false);
        Assert.That(markers.Values.Single().selfGcBytes, Is.Null);
        Assert.That(markers.Values.Single().name, Is.EqualTo("<unnamed>"));
    }
    [Test] public void InvalidHierarchy_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => ProfilerAnalysis.Accumulate("main", new[]
        { new ProfilerAnalysis.Sample { descendants = 2 } }, new Dictionary<string, ProfilerAnalysis.Marker>(), false));
    }
    [Test] public void EmptyDurations_HaveNoPercentile()
    {
        Assert.That(ProfilerAnalysis.Percentile(Array.Empty<double>(), .95), Is.Null);
    }
    [Test] public void Percentile_RejectsInvalidRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProfilerAnalysis.Percentile(new[] { 1d }, 1.1));
    }
    [Test] public void HeaviestFrames_ExcludeUnknownAndKeepWorstTen()
    {
        var frames = Enumerable.Range(0, 20).Select(i => new ProfilerAnalysis.Frame { index = i, mainThreadMs = i }).ToList();
        frames.Add(new ProfilerAnalysis.Frame { index = 30 });
        var result = ProfilerAnalysis.Heaviest(frames);
        Assert.That(result.Length, Is.EqualTo(10));
        Assert.That(result[0].index, Is.EqualTo(19));
        Assert.That(result[9].index, Is.EqualTo(10));
    }
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void BusyRequests_AreRejectedBeforeLoading(bool exporting, bool recording)
    {
        Assert.Throws<InvalidOperationException>(() => ProfilerCaptureExporter.ValidateRequest("missing.data", exporting, recording));
    }
    [Test] public void EmptyCapture_IsRejectedBeforeLoading()
    {
        string path = Path.GetTempFileName();
        try { Assert.Throws<InvalidDataException>(() => ProfilerCaptureExporter.ValidateRequest(path, false, false)); }
        finally { File.Delete(path); }
    }
    [Test] public void Csv_HandlesMissingValues()
    {
        Assert.That(ProfilerCaptureExporter.Csv(null), Is.EqualTo("\"\""));
    }
}
