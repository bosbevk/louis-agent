using LouisAgent.Puls.Admin.Explorer.Replay;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Replay;

/// <summary>A replay keeps the events' own rhythm, three times faster, with no pause over a quarter second.</summary>
public class ReplayPaceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>A third of the gap, never more than 250 ms and never less than none.</summary>
    [TestCase(300, 100)]
    [TestCase(3000, 250)]
    [TestCase(-300, 0)]
    public void TheWait_IsAThirdOfTheGap(int gapMs, int waitMs) =>
        Assert.That(ReplayPace.WaitMs(T0.AddMilliseconds(gapMs), T0), Is.EqualTo(waitMs));

    /// <summary>With either time unknown, a short beat.</summary>
    [Test]
    public void AnUnknownTime_IsAShortBeat()
    {
        Assert.That(ReplayPace.WaitMs(null, T0), Is.EqualTo(15));
        Assert.That(ReplayPace.WaitMs(T0, null), Is.EqualTo(15));
    }
}
