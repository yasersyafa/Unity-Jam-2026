using System;
using System.Collections.Generic;

public enum SlotTier { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Jackpot = 4 }

public readonly struct SlotResult
{
    public int Left { get; }
    public int Middle { get; }
    public int Right { get; }
    public SlotTier Tier { get; }
    public SlotTier MinimumTier { get; }
    public int SpinNumber { get; }

    public SlotResult(int left, int middle, int right, SlotTier tier, SlotTier minimumTier, int spinNumber)
    {
        Left = left; Middle = middle; Right = right;
        Tier = tier; MinimumTier = minimumTier; SpinNumber = spinNumber;
    }
}

public sealed class SlotOutcomeGenerator
{
    private static readonly int[] Weights = { 45, 26, 15, 11, 3 };
    public static IReadOnlyList<int> TierWeights => Weights;
    private static readonly List<int[]>[] Combinations = BuildCombinations();
    private readonly Random random;
    public int SpinNumber { get; private set; }
    public int RareCounter { get; private set; }
    public int EpicCounter { get; private set; }
    public int JackpotCounter { get; private set; }

    public SlotOutcomeGenerator(Random random = null) { this.random = random ?? new Random(); }

    public SlotResult Spin()
    {
        SpinNumber++;
        RareCounter = (RareCounter + 1) % 6;
        EpicCounter = (EpicCounter + 1) % 16;
        JackpotCounter = (JackpotCounter + 1) % 26;
        SlotTier minimum = JackpotCounter == 25 ? SlotTier.Jackpot
            : EpicCounter == 15 ? SlotTier.Epic : RareCounter == 5 ? SlotTier.Rare : SlotTier.Common;
        int total = 0;
        for (int i = (int)minimum; i < Weights.Length; i++) total += Weights[i];
        int roll = random.Next(total);
        int tier = (int)minimum;
        while (roll >= Weights[tier]) { roll -= Weights[tier]; tier++; }
        var pool = Combinations[tier];
        var digits = pool[random.Next(pool.Count)];
        if ((SlotTier)tier == SlotTier.Rare) RareCounter = 0;
        else if ((SlotTier)tier == SlotTier.Epic) EpicCounter = 0;
        else if ((SlotTier)tier == SlotTier.Jackpot) JackpotCounter = 0;
        return new SlotResult(digits[0], digits[1], digits[2], (SlotTier)tier, minimum, SpinNumber);
    }

    public static SlotTier Classify(int a, int b, int c)
    {
        if (a < 1 || a > 9 || b < 1 || b > 9 || c < 1 || c > 9)
            throw new ArgumentOutOfRangeException(nameof(a), "Slot digits must be 1–9.");
        if (a == b && b == c) return a == 7 ? SlotTier.Jackpot : SlotTier.Epic;
        if (a == b || b == c || a == c) return SlotTier.Uncommon;
        if ((b - a == 1 && c - b == 1) || (b - a == -1 && c - b == -1)) return SlotTier.Rare;
        return SlotTier.Common;
    }

    private static List<int[]>[] BuildCombinations()
    {
        var pools = new List<int[]>[5];
        for (int i = 0; i < pools.Length; i++) pools[i] = new List<int[]>();
        for (int a = 1; a <= 9; a++)
            for (int b = 1; b <= 9; b++)
                for (int c = 1; c <= 9; c++) pools[(int)Classify(a, b, c)].Add(new[] { a, b, c });
        return pools;
    }
}
