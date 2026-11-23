using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Pages;
using LouisAgent.Web.Evals.Tests.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.Changes;

/// <summary>
/// F126.4: a run's page that has ended re-grades it, and a re-graded run's page says what it did: which run it
/// re-graded, that no agent was asked, how many judge calls it made and marks it kept, and what changed in the file.
/// </summary>
public class RegradeTests
{
    private const string Eval = "guide";
    private const string GradedId = "20261123-131900-guide";
    private const string RegradedId = "20261123-150400-guide";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 13, 19, 0, TimeSpan.Zero);
    private static readonly Flip Flipped = new(Started.AddHours(1), "It does move on.");

    /// <summary>A run that has ended has Re-grade; one that is going has none.</summary>
    [Test]
    public void AnEndedRun_HasRegrade_AndAGoingOneHasNone()
    {
        using var ctx = new BunitContext();
        using var other = new BunitContext();

        var ended = new EvalPagesHost(Graded()).Render(ctx, Eval, GradedId);
        var going = new EvalPagesHost(Graded() with
        {
            Status = RunStatus.Running,
            EndedAt = null
        })
            .Render(other, Eval, GradedId);

        Assert.That(ended.Find(".run-regrade").TextContent, Is.EqualTo("Re-grade"));
        Assert.That(going.FindAll(".run-regrade"), Is.Empty);
    }

    /// <summary>Re-grade starts the re-grade and goes to the new run's page.</summary>
    [Test]
    public void Regrade_StartsItAndGoesToTheNewRunsPage()
    {
        using var ctx = new BunitContext();
        var host = new EvalPagesHost(Graded());
        var page = host.Render(ctx, Eval, GradedId);

        page.Find(".run-regrade").Click();

        page.WaitForAssertion(() => Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri,
            Does.EndWith(EvalAddress.RunPageOf(Eval, EvalPagesHost.RegradeId))));
        Assert.That(host.Regrades, Is.EqualTo(1));
    }

    /// <summary>
    /// The page a re-grade goes to is the same page with another run, which Blazor keeps rather than makes anew: given
    /// another run, the page reads it, and the re-grade's part shows.
    /// </summary>
    [Test]
    public void ARunsPage_GivenAnotherRun_ReadsIt()
    {
        using var ctx = new BunitContext();
        var graded = Graded();
        var regraded = Regraded();
        var host = new EvalPagesHost(graded, runs: [RunRow.Of(regraded), RunRow.Of(graded)]).Keeps(regraded);
        var page = host.Render(ctx, Eval, GradedId);

        page.Render(parameters => parameters.Add(p => p.Run, RegradedId));

        page.WaitForAssertion(() => Assert.That(page.FindAll(".regrade-changes"), Has.Count.EqualTo(1)));
    }

    /// <summary>A re-grade the API refuses is a line in the API's own words, and the page stays.</summary>
    [Test]
    public void ARefusedRegrade_IsALineInTheApisWords()
    {
        using var ctx = new BunitContext();
        var host = new EvalPagesHost(Graded()) { RefusesRegrades = "A run is already going." };
        var page = host.Render(ctx, Eval, GradedId);

        page.Find(".run-regrade").Click();

        page.WaitForAssertion(() =>
            Assert.That(page.Find(".run-regrade-refused").TextContent, Is.EqualTo("A run is already going.")));
    }

    /// <summary>
    /// A re-graded run's page says it re-graded the run of that time with no agent asked, links that run, and counts
    /// the judge calls and the marks kept.
    /// </summary>
    [Test]
    public void ARegradedRunsPage_SaysWhatItRegraded_AndCountsTheCallsAndTheMarksKept()
    {
        using var ctx = new BunitContext();
        var page = RenderRegraded(ctx);

        var changes = page.Find(".regrade-changes");

        Assert.That(changes.QuerySelector(".regrade-said")!.TextContent, Does.Contain("No agent was asked"));
        Assert.That(changes.QuerySelector("a.regrade-of")!.GetAttribute("href"),
            Is.EqualTo(EvalAddress.RunPageOf(Eval, GradedId)));
        Assert.That(changes.QuerySelector(".regrade-judge-calls")!.TextContent, Is.EqualTo("2"));
        Assert.That(changes.QuerySelector(".regrade-kept")!.TextContent, Is.EqualTo("2 of 4"));
    }

    /// <summary>
    /// What changed: each criterion added with how it was judged, each taken out, the marks kept with your flips, the
    /// facts checked again, and each conversation left out.
    /// </summary>
    [Test]
    public void ARegradedRunsPage_ListsWhatChanged()
    {
        using var ctx = new BunitContext();
        var page = RenderRegraded(ctx);

        var lines = page.FindAll(".regrade-change").Select(line => line.TextContent).ToList();

        Assert.That(lines[0], Does.Contain("guide-walk · turn 1 · a new criterion, \"Names the area\"")
            .And.Contain("Judged in each of 2 repeats: met in 1."));
        Assert.That(lines[1], Does.Contain("guide-walk · turn 1 · \"Lights the area\" was taken out"));
        Assert.That(lines[2], Does.Contain("2 marks kept, and your 2 flips with them"));
        Assert.That(lines[3], Does.Contain("2 facts checked again"));
        Assert.That(lines[4], Does.Contain("guide-bank was left out"));
        Assert.That(page.Find(".regrade-replay").TextContent, Does.Contain("Replay free plays this again"));
    }

    /// <summary>A re-grade that asked the judge nothing says nothing of new answers in the recording.</summary>
    [Test]
    public void ARegradeWithNoJudgeCall_SaysNothingOfNewAnswers()
    {
        using var ctx = new BunitContext();
        var graded = Graded();
        var regraded = Regraded() with { Conversations = [graded.Conversations[0], graded.Conversations[1]] };
        var host = new EvalPagesHost(regraded, runs: [RunRow.Of(regraded), RunRow.Of(graded)]).Keeps(graded);

        var page = host.Render(ctx, Eval, RegradedId);

        Assert.That(page.Find(".regrade-judge-calls").TextContent, Is.EqualTo("0"));
        Assert.That(page.FindAll(".regrade-replay"), Is.Empty);
    }

    /// <summary>The re-graded run's page, with the run it re-graded kept beside it.</summary>
    private static IRenderedComponent<RunPage> RenderRegraded(BunitContext ctx)
    {
        var graded = Graded();
        var regraded = Regraded();
        var host = new EvalPagesHost(regraded, runs: [RunRow.Of(regraded), RunRow.Of(graded)]).Keeps(graded);
        return host.Render(ctx, Eval, RegradedId);
    }

    /// <summary>
    /// The live run: guide-walk's turn with "Moves on" flipped and "Lights the area", in two repeats, and guide-bank.
    /// </summary>
    private static EvalRun Graded() =>
        new(GradedId, Eval, "louis-agent-guide", RunKind.Live, 2, Started,
        [
            Walk([Criterion("Moves on", false, Flipped), Criterion("Lights the area", true)],
                [Criterion("Moves on", false, Flipped), Criterion("Lights the area", true)]),
            new ConversationRun("guide-bank", 2, [Repeat(1, []), Repeat(2, [])])
        ])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddMinutes(6)
        };

    /// <summary>
    /// Its re-grade: "Moves on" kept with its flip, "Lights the area" taken out, "Names the area" added and met in
    /// the first repeat only, and guide-bank left out.
    /// </summary>
    private static EvalRun Regraded() =>
        new(RegradedId, Eval, "louis-agent-guide", RunKind.Regrade, 2, Started.AddMinutes(105),
        [
            Walk([Criterion("Moves on", false, Flipped), Criterion("Names the area", true)],
                [Criterion("Moves on", false, Flipped), Criterion("Names the area", false)])
        ])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddMinutes(105).AddSeconds(9),
            RegradeOf = GradedId,
            ReplayOf = GradedId
        };

    /// <summary>guide-walk, with these criteria in its two repeats' one turn.</summary>
    private static ConversationRun Walk(IReadOnlyList<Mark> first, IReadOnlyList<Mark> second) =>
        new("guide-walk", 2, [Repeat(1, first), Repeat(2, second)]);

    private static Mark Criterion(string text, bool met, Flip? flip = null) =>
        new(text, met, "Why.") { Flip = flip };

    /// <summary>A repeat of one turn with one fact met and these criteria.</summary>
    private static RepeatRun Repeat(int number, IReadOnlyList<Mark> criteria)
    {
        var transcript = new TurnTranscript("Next.", "Next up, packs.", [], TimeSpan.FromSeconds(4));
        return new RepeatRun(number,
            [new TurnRun(transcript, [new Mark("used ShowPage", true, "Called.")], criteria, 0m, "USD")]);
    }
}
