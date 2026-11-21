using LouisAgent.Web.Evals.Admin.Verdict;
using LouisAgent.Web.Evals.Components.RunPage;

namespace LouisAgent.Web.Evals.Tests.Components.RunPage;

/// <summary>
/// Pins the verdict card on top of the run page (F53.1 step 1): a line per mode with passed of total and the change
/// against the run before, coloured by its tone, then the cost, the requests and the answers waiting for a grade
/// (golden: <c>RunVerdict.html</c>).
/// </summary>
public class RunVerdictTests
{
    /// <summary>Two modes, one up on the run before and one on its first run, with two answers to grade.</summary>
    private static readonly RunVerdictView View = new(
        [new ModeVerdict(SampleMode.Alpha, 3, 4, 2, false), new ModeVerdict(SampleMode.Beta, 1, 4, null, true)],
        2, 1.25m, 4);

    /// <summary>The card as it is drawn.</summary>
    [Test]
    public void TheVerdict_IsDrawnAsBefore()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RunVerdict>(p => p.Add(x => x.RunId, "run1").Add(x => x.View, View));

        Snapshots.Match("Components/RunPage/RunVerdict.html", cut.Markup);
    }

    /// <summary>With nothing to grade the card doesn't say so.</summary>
    [Test]
    public void WithNothingToGrade_TheCardDoesntSaySo()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<RunVerdict>(p => p.Add(x => x.RunId, "run1").Add(x => x.View, View with { ToGrade = 0 }));

        Assert.That(cut.FindAll(".verdict-grade"), Is.Empty);
    }
}
