using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Web.Evals.Components.Runs;

namespace LouisAgent.Web.Evals.Tests.Components.Runs;

/// <summary>
/// F126.3.4: how a run that didn't finish ended, said first on its page: a person's stop in the page's own colours,
/// and the harness failing as the error it is, with the run's own reason.
/// </summary>
public class RunEndingTests
{
    private static readonly DateTimeOffset Started = new(2026, 11, 23, 14, 10, 0, TimeSpan.Zero);

    /// <summary>A stopped run's box says a person stopped it, what is kept and how many did not run.</summary>
    [Test]
    public void AStoppedRun_SaysAPersonStoppedIt()
    {
        using var ctx = new BunitContext();

        var ending = ctx.Render<RunEnding>(parameters => parameters.Add(p => p.Run, Ended(RunStatus.Stopped))
            .Add(p => p.NotRun, "4 conversations"));

        Assert.That(ending.Find(".run-stopped-by-person").TextContent, Does.Contain("A person stopped it.")
            .And.Contain("4 conversations did not run."));
    }

    /// <summary>A run that could not finish gives its own reason, as the error it is.</summary>
    [Test]
    public void ARunThatCouldNotFinish_GivesItsReason()
    {
        using var ctx = new BunitContext();
        var run = Ended(RunStatus.CouldNotFinish) with { StoppedBecause = "The judge could not mark." };

        var ending = ctx.Render<RunEnding>(parameters => parameters.Add(p => p.Run, run).Add(p => p.NotRun, "2"));

        Assert.That(ending.Find(".run-stopped").TextContent, Does.Contain("The judge could not mark."));
    }

    /// <summary>A finished run, or one still going, has no ending to tell.</summary>
    [TestCase(RunStatus.Finished)]
    [TestCase(RunStatus.Running)]
    public void AFinishedOrGoingRun_HasNoEndingToTell(RunStatus status)
    {
        using var ctx = new BunitContext();

        var ending = ctx.Render<RunEnding>(parameters => parameters.Add(p => p.Run, Ended(status))
            .Add(p => p.NotRun, "0 conversations"));

        Assert.That(ending.Markup.Trim(), Is.Empty);
    }

    /// <summary>A run of the guide's eval that ended this way.</summary>
    private static EvalRun Ended(RunStatus status) =>
        new("20261123-141000-guide", "guide", "louis-agent-guide", RunKind.Live, 3, Started, [])
        {
            Status = status,
            EndedAt = Started.AddSeconds(158)
        };
}
