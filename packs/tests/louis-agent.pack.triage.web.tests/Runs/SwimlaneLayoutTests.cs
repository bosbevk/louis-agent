using LouisAgent.Packs.Triage.Web.Runs;

namespace LouisAgent.Packs.Triage.Web.Tests.Runs;

/// <summary>
/// The triage team as swimlanes (F122, Louis's pick of three, "lanes with steps"): a lane per agent in the stage's
/// order, each step a dot in its agent's lane, left to right in the run's order, joined by one line.
/// </summary>
public class SwimlaneLayoutTests
{
    /// <summary>Each step's dot sits in its agent's lane, one column further right than the step before.</summary>
    [Test]
    public void EachStep_SitsInItsAgentsLane_InOrder()
    {
        var dots = SwimlaneLayout.Dots(["code", "sorter", "manager", "investigator", "manager"]);

        Assert.Multiple(() =>
        {
            Assert.That(dots.Select(dot => dot.Lane), Is.EqualTo(new[] { 0, 1, 2, 3, 2 }));
            Assert.That(dots.Select(dot => dot.Step), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(dots.Select(dot => dot.X), Is.Ordered.Ascending);
            Assert.That(dots[2].Y, Is.EqualTo(dots[4].Y), "the manager's two steps share its lane");
        });
    }

    /// <summary>The line runs through every dot, in order; no steps, no line.</summary>
    [Test]
    public void TheLine_RunsThroughEveryDot()
    {
        var dots = SwimlaneLayout.Dots(["code", "sorter"]);

        Assert.Multiple(() =>
        {
            Assert.That(SwimlaneLayout.Line(dots),
                Is.EqualTo($"M{dots[0].X} {dots[0].Y} L{dots[1].X} {dots[1].Y}"));
            Assert.That(SwimlaneLayout.Line([]), Is.Empty);
        });
    }

    /// <summary>An agent the stage doesn't know runs in the workflow's lane, so a new kind of step still shows.</summary>
    [Test]
    public void AnUnknownAgent_RunsInTheFirstLane()
    {
        Assert.That(SwimlaneLayout.Dots(["someone-new"])[0].Lane, Is.EqualTo(0));
    }

    /// <summary>The picture is as wide as the whole run, so the dots don't move as more steps arrive.</summary>
    [Test]
    public void TheWidth_IsTheWholeRuns()
    {
        Assert.That(SwimlaneLayout.Width(9), Is.EqualTo(SwimlaneLayout.LabelWidth + 9 * SwimlaneLayout.ColumnWidth));
    }
}
