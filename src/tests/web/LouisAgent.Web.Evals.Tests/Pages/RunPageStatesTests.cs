using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Web.Evals.Tests.Pages;

/// <summary>
/// A run's page says plainly what it could not show: a run that stopped gives its reason first, one still running
/// shows what is graded so far, and a run the host doesn't have is one line. The eval's other runs sit at its foot.
/// </summary>
public class RunPageStatesTests
{
    private const string Eval = "kitchen";

    private static readonly DateTimeOffset Started = new(2026, 11, 22, 14, 40, 0, TimeSpan.Zero);

    /// <summary>
    /// A run that could not finish shows its reason first, then what was graded, and how many of the eval's
    /// conversations did not run.
    /// </summary>
    [Test]
    public void ARunThatCouldNotFinish_ShowsItsReasonFirst_ThenWhatWasGraded()
    {
        using var ctx = new BunitContext();
        var run = Run() with
        {
            Status = RunStatus.CouldNotFinish,
            EndedAt = Started.AddSeconds(108),
            StoppedBecause = "The judge could not mark \"Offers a table\" in kitchen-booking."
        };
        var page = Host(run).Render(ctx, Eval, run.Id);

        var stopped = page.Find(".run-stopped");

        Assert.That(page.Markup.IndexOf("run-stopped", StringComparison.Ordinal),
            Is.LessThan(page.Markup.IndexOf("run-this", StringComparison.Ordinal)), "the reason comes first");
        Assert.That(stopped.TextContent, Does.Contain("could not finish").And.Contain("Offers a table")
            .And.Contain("2 conversations did not run"));
        Assert.That(page.Find(".run-of").TextContent, Does.Contain("of 2 graded passed"));
    }

    /// <summary>A run still running says so, and shows the conversations graded so far.</summary>
    [Test]
    public void ARunStillRunning_SaysSo_AndShowsWhatIsGradedSoFar()
    {
        using var ctx = new BunitContext();
        var page = Host(Run()).Render(ctx, Eval, Run().Id);

        Assert.That(page.Find(".run-running").TextContent, Does.Contain("still going"));
        Assert.That(page.FindAll(".conversation-row"), Has.Count.EqualTo(2));
    }

    /// <summary>Reload reads the run again.</summary>
    [Test]
    public void Reload_ReadsTheRunAgain()
    {
        using var ctx = new BunitContext();
        var host = Host(Run());
        var page = host.Render(ctx, Eval, Run().Id);

        page.Find("button.run-reload").Click();

        page.WaitForAssertion(() => Assert.That(host.RunReads, Is.EqualTo(2)));
    }

    /// <summary>A run the host doesn't have is one line that names the id, and nothing else.</summary>
    [Test]
    public void ARunTheHostDoesntHave_IsOneLine_NamingItsId()
    {
        using var ctx = new BunitContext();
        var page = new RunPageHost(null).Render(ctx, Eval, "20261122-164000-kitchen");

        Assert.That(page.Find(".run-missing").TextContent, Does.Contain("20261122-164000-kitchen"));
        Assert.That(page.FindAll(".run-this"), Is.Empty);
    }

    /// <summary>The eval's runs are listed at the foot, as the API sends them, with this run marked.</summary>
    [Test]
    public void TheEvalsRuns_AreAtTheFoot_WithThisRunMarked()
    {
        using var ctx = new BunitContext();
        var run = Run();
        var page = Host(run).Render(ctx, Eval, run.Id);

        var rows = page.FindAll(".run-row");

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[1].ClassList, Does.Contain("run-row-open"));
        Assert.That(rows[0].ClassList, Does.Not.Contain("run-row-open"));
    }

    /// <summary>A host with this run, the kitchen's eval of four conversations, and two runs of it.</summary>
    private static RunPageHost Host(EvalRun run)
    {
        var newer = RunRow.Of(run) with
        {
            Id = "20261123-090000-kitchen",
            StartedAt = Started.AddDays(1)
        };
        return new RunPageHost(run, new EvalsReply([new EvalRow(Eval, "wheatfield-kitchen", 4, newer)]),
            [newer, RunRow.Of(run)]);
    }

    /// <summary>A run of the kitchen's eval, still running, with two of its four conversations graded.</summary>
    private static EvalRun Run() =>
        new("20261122-144000-kitchen", Eval, "wheatfield-kitchen", RunKind.Live, 1, Started,
        [
            new ConversationRun("kitchen-hours", 1, [Repeat("Are you open on Sunday?", met: true)]),
            new ConversationRun("kitchen-menu", 1, [Repeat("Do you have anything without gluten?", met: false)])
        ]);

    /// <summary>A repeat of one turn whose one criterion is met or not.</summary>
    private static RepeatRun Repeat(string ask, bool met)
    {
        var transcript = new TurnTranscript(ask, "An answer.", [], TimeSpan.FromSeconds(4));
        var criterion = new Mark("Names a dish from the menu", met, "Why.");
        return new RepeatRun(1, [new TurnRun(transcript, [], [criterion], 0.002m, "USD")]);
    }
}
