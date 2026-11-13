using NUnit.Framework;

public sealed class EnemyFlightVisualTests
{
    private const float Threshold = 0.4f, Interval = 0.2f;

    [TestCase(false, 1f, true)]    // moving right fast enough turns right
    [TestCase(true, -1f, false)]   // moving left fast enough turns left
    [TestCase(false, 0.2f, false)] // too slow: keep facing
    [TestCase(true, -0.39f, true)] // just under the threshold: keep facing
    [TestCase(true, 2f, true)]     // already facing that way
    public void ResolveFacingRight_TurnsOnlyAboveSpeedThreshold(bool current, float speed, bool expected)
    {
        Assert.That(EnemyFlightVisual.ResolveFacingRight(current, speed, Threshold, 1f, Interval), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveFacingRight_WaitsForMinimumIntervalBetweenTurns()
    {
        Assert.That(EnemyFlightVisual.ResolveFacingRight(false, 5f, Threshold, 0.1f, Interval), Is.False);
        Assert.That(EnemyFlightVisual.ResolveFacingRight(false, 5f, Threshold, 0.2f, Interval), Is.True);
    }
}
