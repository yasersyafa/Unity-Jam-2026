using System;
using NUnit.Framework;

public sealed class SlotOutcomeGeneratorTests
{
    [TestCase(7, 7, 7, SlotTier.Jackpot)]
    [TestCase(2, 2, 2, SlotTier.Epic)]
    [TestCase(1, 2, 3, SlotTier.Rare)]
    [TestCase(9, 8, 7, SlotTier.Rare)]
    [TestCase(1, 2, 1, SlotTier.Uncommon)]
    [TestCase(1, 4, 9, SlotTier.Common)]
    public void Classify_RecognizesWinningPatterns(int a, int b, int c, SlotTier expected)
    {
        Assert.That(SlotOutcomeGenerator.Classify(a, b, c), Is.EqualTo(expected));
    }

    [TestCase(0, 1, 1)]
    [TestCase(1, 10, 1)]
    [TestCase(1, 1, -1)]
    public void Classify_RejectsInvalidDigits(int a, int b, int c)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SlotOutcomeGenerator.Classify(a, b, c));
    }

    [Test]
    public void Spin_ProducesMatchingDigitsAndHonorsMinimumTier()
    {
        var generator = new SlotOutcomeGenerator(new Random(123));
        for (int spin = 1; spin <= 1000; spin++)
        {
            var result = generator.Spin();
            Assert.That(result.SpinNumber, Is.EqualTo(spin));
            Assert.That(result.Tier, Is.EqualTo(SlotOutcomeGenerator.Classify(result.Left, result.Middle, result.Right)));
            Assert.That((int)result.Tier, Is.GreaterThanOrEqualTo((int)result.MinimumTier));
        }
    }

    private sealed class LowestRoll : Random
    {
        public override int Next(int maximum) => 0;
    }

    [TestCase(5, SlotTier.Rare)]
    [TestCase(15, SlotTier.Epic)]
    [TestCase(25, SlotTier.Jackpot)]
    public void Spin_GuaranteesPityTierAtThreshold(int spins, SlotTier expected)
    {
        var generator = new SlotOutcomeGenerator(new LowestRoll());
        SlotResult result = default;
        for (int i = 0; i < spins; i++) result = generator.Spin();
        Assert.That(result.MinimumTier, Is.EqualTo(expected));
        Assert.That(result.Tier, Is.EqualTo(expected));
    }

    [Test]
    public void TierWeights_MatchConfiguredDropPercentages()
    {
        Assert.That(SlotOutcomeGenerator.TierWeights, Is.EqualTo(new[] { 45, 26, 15, 11, 3 }));
    }
}
