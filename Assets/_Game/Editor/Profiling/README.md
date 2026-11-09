# Profiler capture export

Stop recording before exporting. Loading a capture replaces the displayed Profiler history.

Record with the existing CLI:

```sh
unity command diagnostics profiler_record --frames 300
unity command diagnostics profiler_record_status
```

When recording completes, use the returned capture path:

```sh
unity command script eval 'return ProfilerCaptureExporter.Export("Logs/capture.data");'
unity command script eval 'return ProfilerCaptureExporter.GetStatus();'
```

Poll until completed or failed. Exports go to Logs/ProfilerExports by default: summary.json, frames.csv, markers.csv. Domain reload interrupts the job; restart the export afterwards. Avoid starting recording or loading another capture during export.

Markers are ranked by self CPU time, separated by thread. Inclusive time includes children and must not be summed across nesting. Wait/VSync markers remain visible; they do not imply compute bottlenecks. Empty CSV values mean unavailable. Only recorded CPU samples and GC.Alloc metadata are inspected; GPU and memory snapshots are outside this tool. Deep Profile is optional and changes measurement overhead.

## Export fields and lifecycle

`summary.json` includes source size, reader version, frame range, nearest-rank percentiles, the ten heaviest frames, and all markers ranked by self time. Unknown timings are JSON null. CSV uses invariant numeric formatting and quoted text.

`markers.csv` includes inclusiveGcBytes (allocation samples in the marker subtree) and selfGcBytes (the marker's immediate GC.Alloc children). GC.Alloc rows also expose their own allocation metadata. These values overlap: do not sum parent and child rows. Missing allocation metadata is not treated as zero. Marker allocation totals cover only usable frame/thread observations.

`frames.csv` gives explicit reasons when main-thread timing or GC metadata is absent. No GC.Alloc marker means unavailable instrumentation, not proof of zero allocations.

The output directory must be empty. A completed summary is written only after the CSV files. Treat files from failed/interrupted jobs as partial. Read GetStatus() before using output. Only one export runs at once; queued/active CLI recordings are rejected. Starting recording or changing the history range during export fails the job. Do not load another capture concurrently, including one with the same frame range.

Cancel through:

```sh
unity command script eval 'return ProfilerCaptureExporter.Cancel();'
```

Domain reload and Editor shutdown interrupt the job and persist its status in Temp/profiler_export_status.json. Restart the export to a new directory. Each job times out after 600 seconds. A single exceptionally large frame can still occupy the main thread while that frame is read.
