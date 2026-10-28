using LouisAgent.Puls.Admin.Trends;

namespace LouisAgent.Puls.Tests.Admin.Trends;

/// <summary>
/// Where the trend chart puts its runs and values, and how it draws a line: runs spread evenly across the width, a
/// value up from the axis, and a line broken where a run has no value (the unattended run's finding 5).
/// </summary>
public class TrendGeometryTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Three runs, an hour apart.</summary>
    private static readonly TrendRun[] Runs =
        [new("run_1", At), new("run_2", At.AddHours(1)), new("run_3", At.AddHours(2))];

    /// <summary>The first run sits at the left padding, the last at the right, one alone in the middle.</summary>
    [Test]
    public void Runs_SpreadAcrossTheWidth()
    {
        var three = new TrendGeometry(Runs, top: 1);
        var one = new TrendGeometry([Runs[0]], top: 1);

        Assert.That(three.X("run_1"), Is.EqualTo(TrendGeometry.Pad));
        Assert.That(three.X("run_3"), Is.EqualTo(TrendGeometry.Width - TrendGeometry.Pad));
        Assert.That(one.X("run_1"), Is.EqualTo(TrendGeometry.Width / 2));
    }

    /// <summary>Nought sits on the axis and the top at the top padding.</summary>
    [Test]
    public void Values_RiseFromTheAxis()
    {
        var geometry = new TrendGeometry(Runs, top: 2);

        Assert.That(geometry.Y(0), Is.EqualTo(TrendGeometry.Height - TrendGeometry.Pad));
        Assert.That(geometry.Y(2), Is.EqualTo(TrendGeometry.Pad));
    }

    /// <summary>A line with every run is one stroke.</summary>
    [Test]
    public void ALineWithEveryRun_IsOneStroke()
    {
        string path = new TrendGeometry(Runs, top: 1).Path([("run_1", 0), ("run_2", 0.5), ("run_3", 1)]);

        Assert.That(path.Count(c => c == 'M'), Is.EqualTo(1));
        Assert.That(path.Count(c => c == 'L'), Is.EqualTo(2));
    }

    /// <summary>A run with no value breaks the line there, so the gap shows instead of a segment across it.</summary>
    [Test]
    public void ARunWithNoValue_BreaksTheLine()
    {
        var geometry = new TrendGeometry(Runs, top: 1);

        string missing = geometry.Path([("run_1", 0), ("run_3", 1)]);
        string unknown = geometry.Path([("run_1", 0), ("run_2", null), ("run_3", 1)]);

        Assert.That(missing.Count(c => c == 'L'), Is.Zero, "nothing joins run 1 to run 3 across run 2");
        Assert.That(unknown, Is.EqualTo(missing));
    }
}
