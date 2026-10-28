using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Components.Admin.Grades;

namespace LouisAgent.Puls.Tests.Components.Admin.Grades;

/// <summary>
/// The run's eval and graders, pinned to <c>GraderList.graders.html</c> and, for a run from before step 2a,
/// <c>GraderList.none.html</c> (one line: the run recorded no graders), F18 step 4.5c.
/// </summary>
public class GraderListTests
{
    /// <summary>A run's row with the eval fields given, everything else plain.</summary>
    private static StoredRunRow Run(string? eval, int? version, string? commit, IReadOnlyList<RunGraderRow>? graders) =>
        new("run_1", "check-x", 1, "done", 0, DateTimeOffset.UnixEpoch, null, null, null, null, eval, version, commit,
            null, graders);

    /// <summary>A run with two graders lists them as chips, under its eval, version and commit.</summary>
    [Test]
    public void WithGraders_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();
        var run = Run("stub", 1, "abc1234",
        [
            new RunGraderRow("no-error", 1, "FixedRule", null),
            new RunGraderRow("within-time", 1, "FixedRule", null),
        ]);

        var cut = ctx.Render<GraderList>(p => p.Add(c => c.Run, run));

        Assert.That(cut.FindAll(".chip").Select(c => c.TextContent),
            Is.EqualTo(new[] { "no-error v1 · FixedRule", "within-time v1 · FixedRule" }));
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Grades/GraderList.graders.html", cut.Markup);
    }

    /// <summary>A run from before graders were recorded says so in one line.</summary>
    [Test]
    public void FromBeforeStep2a_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<GraderList>(p => p.Add(c => c.Run, Run(null, null, null, null)));

        Assert.That(cut.FindAll(".chip"), Is.Empty);
        Assert.That(cut.Find("p").TextContent, Is.EqualTo("This run recorded no graders."));
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Grades/GraderList.none.html", cut.Markup);
    }
}
