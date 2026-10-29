using LouisAgent.Puls.Admin.Explorer.Timeline;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Timeline;

/// <summary>A turn's requests laid out on a time axis, Puls's lane then the specialist's.</summary>
public class TimelineLayoutTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The axis runs from the first start to the last end; each bar starts and spans its share, never thinner than
    /// 0.6%, and the specialist's lane is named for the turn's role.
    /// </summary>
    [Test]
    public void EachRequest_IsABarOnTheAxis()
    {
        var turn = new TurnNode { Role = SampleRole.Alpha };
        turn.Rows.Add(Request(T0.AddMilliseconds(1500), 1500, null));
        turn.Rows.Add(Request(T0.AddMilliseconds(4000), 2000, "s1"));
        turn.Rows.Add(Request(T0.AddMilliseconds(2000), 10, null));

        var layout = TimelineLayout.Of(turn);

        Assert.That(layout.SpanMs, Is.EqualTo(4000));
        Assert.That(layout.Lanes.Select(l => (l.Label, l.Specialist)),
            Is.EqualTo(new[] { ("Puls", false), (SampleRole.Alpha, true) }));
        Assert.That(layout.Lanes[0].Bars.Select(b => (b.I, b.Left, b.Width)),
            Is.EqualTo(new[] { (0, "0", "37.5"), (1, "49.75", "0.6") }));
        Assert.That(layout.Lanes[1].Bars.Select(b => (b.I, b.Left, b.Width)), Is.EqualTo(new[] { (0, "50", "50") }));
    }

    /// <summary>A turn no specialist answered still has the specialist's lane, named for none.</summary>
    [Test]
    public void WithNoRole_TheSecondLaneIsTheSpecialist()
    {
        var turn = new TurnNode();
        turn.Rows.Add(Request(T0, 100, null));

        Assert.That(TimelineLayout.Of(turn).Lanes[1].Label, Is.EqualTo("specialist"));
    }

    /// <summary>A request that ended at a time and took so long, a specialist's when it has a parent.</summary>
    private static RequestNode Request(DateTimeOffset at, long ms, string? parent) =>
        new(at, "turn", "s", parent, "m", 1, 0, 0, 1, 0.01m, ms);
}
