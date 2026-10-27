using LouisAgent.Testing;
using System.Globalization;
using LouisAgent.Puls.Admin.Trends;

namespace LouisAgent.Puls.Tests.Admin.Trends;

// 6.0b: what the trend chart reads out of its runs, out of TrendChart's @code (the rework of 2026-10-26: one run read across the panels, "only update the graph I'm on",
// a value with its change since the run before). Written first: the logic was in the razor page. The bUnit tests of TrendChart stay as they were.
public class TrendReadingTests
{
    private static readonly DateTimeOffset At = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);
    private static readonly TrendRun[] Runs = [new("run_1", At), new("run_2", At.AddDays(1)), new("run_3", At.AddDays(2))];

    private static string Percent(double v) => v.ToString("0%", CultureInfo.InvariantCulture);

    private static TrendSample Sample(int run, double? value) => new(Runs[run].Run, Runs[run].At, value);

    [Test]
    public void TheShownRun_IsTheMarkedOne_ElseTheLast()
    {
        Assert.That(new TrendReading(Runs, "run_2").Shown.Run, Is.EqualTo("run_2"));
        Assert.That(new TrendReading(Runs, null).Shown.Run, Is.EqualTo("run_3"));
        Assert.That(new TrendReading(Runs, "run_gone").Shown.Run, Is.EqualTo("run_3"), "a run the chart doesn't have is no mark");
    }

    [Test]
    public void AHover_ChangesTheRunOfItsOwnPanelOnly()
    {
        var reading = new TrendReading(Runs, "run_3");

        Assert.That(reading.ShownIn("cost", ("cost", "run_1")).Run, Is.EqualTo("run_1"));
        Assert.That(reading.ShownIn("tokens", ("cost", "run_1")).Run, Is.EqualTo("run_3"), "another panel keeps the shown run");
        Assert.That(reading.ShownIn("cost", null).Run, Is.EqualTo("run_3"));
        Assert.That(reading.ShownIn("cost", ("cost", "run_gone")).Run, Is.EqualTo("run_3"), "an unknown hovered run falls back");
    }

    [Test]
    public void HoveredIsTheRunAPanelReads_OnlyWhileItIsHovered()
    {
        var reading = new TrendReading(Runs, "run_3");

        Assert.That(reading.Hovered("cost", ("cost", "run_1"))!.Run, Is.EqualTo("run_1"));
        Assert.That(reading.Hovered("tokens", ("cost", "run_1")), Is.Null);
        Assert.That(reading.Hovered("cost", null), Is.Null);
    }

    [Test]
    public void AValue_ShowsItsChangeSinceTheRunBefore()
    {
        var points = new[] { Sample(0, 0.5), Sample(1, 0.75), Sample(2, 0.25) };

        var up = TrendReading.Value(SampleMode.Alpha, "#3987e5", points, "run_2", Percent);
        Assert.That((up.Text, up.Change, up.Up), Is.EqualTo(("75%", "▲ +25%", true)));
        var down = TrendReading.Value(SampleMode.Alpha, "#3987e5", points, "run_3", Percent);
        Assert.That((down.Text, down.Change, down.Up), Is.EqualTo(("25%", "▼ −50%", false)));
        Assert.That(down.Name, Is.EqualTo(SampleMode.Alpha));
        Assert.That(down.Color, Is.EqualTo("#3987e5"));
    }

    [Test]
    public void TheFirstValue_AndAnUnchangedOne_HaveNoChange()
    {
        var points = new[] { Sample(0, 0.5), Sample(1, 0.5) };

        Assert.That(TrendReading.Value(SampleMode.Alpha, "#fff", points, "run_1", Percent).Change, Is.Null, "nothing before it");
        Assert.That(TrendReading.Value(SampleMode.Alpha, "#fff", points, "run_2", Percent).Change, Is.Null, "no change is no arrow");
    }

    [Test]
    public void AnUnknownValue_IsADash_AndTheChangeSkipsIt()
    {
        var points = new[] { Sample(0, 0.5), Sample(1, null), Sample(2, 0.75) };

        Assert.That(TrendReading.Value(SampleMode.Alpha, "#fff", points, "run_2", Percent).Text, Is.EqualTo("–"), "nobody measured run_2");
        Assert.That(TrendReading.Value(SampleMode.Alpha, "#fff", points, "run_3", Percent).Change, Is.EqualTo("▲ +25%"), "against run_1, the run before that has a value");
        Assert.That(TrendReading.Value(SampleMode.Alpha, "#fff", [], "run_1", Percent).Text, Is.EqualTo("–"), "a mode with no points");
    }
}
