using NUnit.Framework;

public sealed class ProfilerExporterTests
{
    [Test] public void Percentile_UsesNearestRank()
    {
        Assert.That(ProfilerCaptureExporter.Percentile(new double[] { 1, 2, 3, 4 }, .5), Is.EqualTo(2));
        Assert.That(ProfilerCaptureExporter.Percentile(new double[] { 1, 2, 3, 4 }, .99), Is.EqualTo(4));
    }
    [Test] public void Csv_EscapesQuotesAndNewlines()
    {
        Assert.That(ProfilerCaptureExporter.Csv("a,\"b\"\nc"), Is.EqualTo("\"a,\"\"b\"\"\nc\""));
    }
    [Test] public void Export_RejectsMissingFile()
    {
        Assert.Throws<System.IO.FileNotFoundException>(() => ProfilerCaptureExporter.Export("Temp/nonexistent-profiler-test.data"));
    }
}
