using LouisAgent.Web.Components.Staging.Windows;

namespace LouisAgent.Web.Tests.Components.Staging.Windows;

/// <summary>
/// F134.1: the second of the two bodies a window can hold. An agent at work has no website and no visitor, so its
/// window is the work: panes side by side, each a heading over a few lines.
/// </summary>
public class WorkBodyTests
{
    /// <summary>Each pane is drawn with its heading and its lines, in the order given.</summary>
    [Test]
    public void EachPane_IsItsHeading_OverItsLines()
    {
        using var ctx = new BunitContext();
        WorkPane[] panes =
        [
            new("The job", ["Find out why the payments don't match."]),
            new("What it did", ["3 failing", "reads the matcher", "14 passed"]),
        ];

        var body = ctx.Render<WorkBody>(parameters => parameters.Add(work => work.Panes, panes));

        var drawn = body.FindAll(".work-body .work-pane");
        Assert.Multiple(() =>
        {
            Assert.That(drawn.Select(pane => pane.QuerySelector(".pane-heading")!.TextContent),
                Is.EqualTo(new[] { "The job", "What it did" }));
            Assert.That(drawn[1].QuerySelectorAll(".pane-line").Select(line => line.TextContent),
                Is.EqualTo(new[] { "3 failing", "reads the matcher", "14 passed" }));
            Assert.That(body.Find(".work-body").GetAttribute("style"), Is.EqualTo("--panes: 2;"));
        });
    }
}
