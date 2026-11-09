using System;
using System.Collections.Generic;
using System.Linq;

public static class ProfilerAnalysis
{
    public sealed class Sample
    {
        public string name;
        public int descendants;
        public double milliseconds;
        public bool allocation;
        public long? allocationBytes;
    }
    public sealed class Marker
    {
        public string thread, name;
        public int calls;
        public double inclusiveMs, selfMs, maximumMs;
        public long? inclusiveGcBytes, selfGcBytes;
    }
    public sealed class Frame
    {
        public int index;
        public double? mainThreadMs;
        public long? gcAllocBytes;
        public string missingMainThreadReason, missingGcReason;
    }
    public static void Accumulate(string thread, Sample[] samples, Dictionary<string, Marker> markers, bool allocationsAvailable)
    {
        var prefix = new long[samples.Length + 1];
        for (int i = 0; i < samples.Length; i++) prefix[i + 1] = prefix[i] + (samples[i].allocationBytes ?? 0);
        for (int i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            int end = i + sample.descendants + 1;
            if (sample.descendants < 0 || end > samples.Length) throw new ArgumentException("Invalid sample hierarchy.");
            double childMs = 0;
            for (int child = i + 1; child < end; child += 1 + samples[child].descendants)
            {
                if (samples[child].descendants < 0 || child + samples[child].descendants + 1 > end)
                    throw new ArgumentException("Invalid child hierarchy.");
                childMs += samples[child].milliseconds;
            }
            string name = sample.name ?? "<unnamed>";
            string key = thread.Length + ":" + thread + name;
            if (!markers.TryGetValue(key, out var marker)) markers[key] = marker = new Marker { thread = thread, name = name };
            marker.calls++;
            marker.inclusiveMs += sample.milliseconds;
            marker.selfMs += Math.Max(0, sample.milliseconds - childMs);
            marker.maximumMs = Math.Max(marker.maximumMs, sample.milliseconds);
            if (allocationsAvailable)
            {
                marker.inclusiveGcBytes = (marker.inclusiveGcBytes ?? 0) + prefix[end] - prefix[i];
                // Attribute allocation cost to its immediate parent, excluding nested calls.
                long own = sample.allocationBytes ?? 0;
                for (int child = i + 1; child < end; child += 1 + samples[child].descendants)
                    if (samples[child].allocation) own += samples[child].allocationBytes ?? 0;
                marker.selfGcBytes = (marker.selfGcBytes ?? 0) + own;
            }
        }
    }
    public static double? Percentile(double[] sorted, double percentile)
    {
        if (percentile < 0 || percentile > 1 || double.IsNaN(percentile)) throw new ArgumentOutOfRangeException(nameof(percentile));
        if (sorted.Length == 0) return null;
        return sorted[Math.Max(0, (int)Math.Ceiling(percentile * sorted.Length) - 1)];
    }
    public static Frame[] Heaviest(IEnumerable<Frame> frames) => frames.Where(f => f.mainThreadMs.HasValue)
        .OrderByDescending(f => f.mainThreadMs).ThenBy(f => f.index).Take(10).ToArray();
}
