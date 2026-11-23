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

        Assert.That(page.Find(".run-running").TextContent, Does.Contain("The run is going.")
            .And.Contain("2 of 4 conversations are graded so far"));
        Assert.That(page.FindAll(".conversation-row"), Has.Count.EqualTo(2));
    }

    /// <summary>Reload reads an ended run again.</summary>
    [Test]
    public void Reload_ReadsTheRunAgain()
    {
        using var ctx = new BunitContext();
        var host = Host(Finished());
        var page = host.Render(ctx, Eval, Run().Id);

        page.Find("button.run-reload").Click();

        page.WaitForAssertion(() => Assert.That(host.RunReads, Is.EqualTo(2)));
    }

    /// <summary>A run the host doesn't have is one line that names the id, and nothing else.</summary>
    [Test]
    public void ARunTheHostDoesntHave_IsOneLine_NamingItsId()
    {
        using var ctx = new BunitContext();
        var page = new EvalPagesHost(null).Render(ctx, Eval, "20261122-164000-kitchen");

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

    /// <summary>F126.3.4: a running run's page reads it again every three seconds, by the page's clock.</summary>
    [Test]
    public void ARunningRunsPage_ReadsItAgain_EveryThreeSeconds()
    {
        using var ctx = new BunitContext();
        var host = Host(Run());
        var page = host.Render(ctx, Eval, Run().Id);

        host.Clock.Advance(TimeSpan.FromSeconds(3));
        page.WaitForAssertion(() => Assert.That(host.RunReads, Is.EqualTo(2)));
        host.Clock.Advance(TimeSpan.FromSeconds(3));
        page.WaitForAssertion(() => Assert.That(host.RunReads, Is.EqualTo(3)));
    }

    /// <summary>When a read answers a run that has ended, the page shows it and reads no more.</summary>
    [Test]
    public void WhenTheRunEnds_ThePageShowsIt_AndReadsNoMore()
    {
        using var ctx = new BunitContext();
        var host = Host(Run());
        var page = host.Render(ctx, Eval, Run().Id);

        host.Answers(Finished());
        host.Clock.Advance(TimeSpan.FromSeconds(3));
        page.WaitForAssertion(() => Assert.That(page.FindAll(".run-running"), Is.Empty));
        host.Clock.Advance(TimeSpan.FromSeconds(9));

        Assert.That(host.RunReads, Is.EqualTo(2));
    }

    /// <summary>A page that is gone reads no more, and a finished run's page never reads again by itself.</summary>
    [Test]
    public async Task AGonePage_AndAFinishedRunsPage_ReadNoMore()
    {
        using var ctx = new BunitContext();
        var going = Host(Run());
        going.Render(ctx, Eval, Run().Id);
        await ctx.DisposeComponentsAsync();
        going.Clock.Advance(TimeSpan.FromSeconds(9));
        using var other = new BunitContext();
        var finished = Host(Finished());
        finished.Render(other, Eval, Run().Id);
        finished.Clock.Advance(TimeSpan.FromSeconds(9));

        Assert.That((going.RunReads, finished.RunReads), Is.EqualTo((1, 1)));
    }

    /// <summary>While the run goes, Stop is there and Reload is not; Stop stops it and reads it again.</summary>
    [Test]
    public void WhileTheRunGoes_StopStopsIt_AndReadsItAgain()
    {
        using var ctx = new BunitContext();
        var host = Host(Run());
        var page = host.Render(ctx, Eval, Run().Id);

        Assert.That(page.FindAll("button.run-reload"), Is.Empty);
        page.Find("button.run-stop").Click();

        page.WaitForAssertion(() => Assert.That((host.Stops, host.RunReads), Is.EqualTo((1, 2))));
    }

    /// <summary>
    /// A run a person stopped shows its own box: that a person stopped it, that what was graded before is kept, and
    /// how many conversations did not run; it is not shown as an error.
    /// </summary>
    [Test]
    public void AStoppedRun_ShowsItsBox_NotAsAnError()
    {
        using var ctx = new BunitContext();
        var run = Run() with
        {
            Status = RunStatus.Stopped,
            EndedAt = Started.AddSeconds(158)
        };
        var page = Host(run).Render(ctx, Eval, run.Id);

        Assert.That(page.Find(".run-stopped-by-person").TextContent, Does.Contain("The run was stopped")
            .And.Contain("A person stopped it. What was graded before is kept.").And
            .Contain("2 conversations did not run."));
        Assert.That(page.FindAll(".run-stopped"), Is.Empty, "not the could-not-finish box");
        Assert.That(page.Find(".page-head").TextContent,
            Does.Contain("2 of 4 conversations were graded, then the run was stopped."));
    }

    /// <summary>The kitchen's run as it ended, finished.</summary>
    private static EvalRun Finished() => Run() with
    {
        Status = RunStatus.Finished,
        EndedAt = Started.AddSeconds(30)
    };

    /// <summary>A host with this run, the kitchen's eval of four conversations, and two runs of it.</summary>
    private static EvalPagesHost Host(EvalRun run)
    {
        var newer = RunRow.Of(run) with
        {
            Id = "20261123-090000-kitchen",
            StartedAt = Started.AddDays(1)
        };
        return new EvalPagesHost(run, new EvalsReply([new EvalRow(Eval, "wheatfield-kitchen", 4, newer)]),
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
