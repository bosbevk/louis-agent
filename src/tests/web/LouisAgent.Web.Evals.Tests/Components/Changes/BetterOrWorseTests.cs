using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Api;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Components.Evals;
using LouisAgent.Web.Evals.Pages;
using LouisAgent.Web.Evals.Tests.Pages;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Evals.Tests.Components.Changes;

/// <summary>
/// F126.4: better or worse, where a person looks first. A run's head says how many more or fewer passed than the run
/// before and names it, and tags what now passes or now fails; the last eight runs draw a bar each with the criteria
/// missed most; the evals' card says the change in a line; and a conversation's page says how often a criterion was
/// flipped lately, and links the run before by the comparison's rule.
/// </summary>
public class BetterOrWorseTests
{
    private const string Eval = "guide";
    private const string NowId = "20261123-131900-guide";
    private const string BeforeId = "20261122-101500-guide";
    private const string StoppedId = "20261123-090000-guide";

    private static readonly DateTimeOffset Now = new(2026, 11, 23, 13, 19, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Before = new(2026, 11, 22, 10, 15, 0, TimeSpan.Zero);

    /// <summary>
    /// The head says one more passed than the run before and names that run; the conversation that now passes is
    /// tagged so, and one that passed both times isn't.
    /// </summary>
    [Test]
    public void TheHead_SaysOneMoreThanTheRunBefore_AndTheRowThatNowPassesIsTagged()
    {
        using var ctx = new BunitContext();
        var page = RenderRun(ctx);

        Assert.That(page.Find(".run-change").TextContent, Does.Contain("▲ 1 more than the run before")
            .And.Contain("guide-bank now passes"));
        Assert.That(page.Find(".run-change a").GetAttribute("href"), Is.EqualTo(EvalAddress.RunPageOf(Eval, BeforeId)));
        Assert.That(page.FindAll(".conversation-row-now-passes").Select(tag => tag.TextContent),
            Is.EqualTo(new[] { "now passes" }));
    }

    /// <summary>A run with no run before says nothing of one.</summary>
    [Test]
    public void ARunWithNoRunBefore_SaysNothingOfOne()
    {
        using var ctx = new BunitContext();
        var page = new EvalPagesHost(NowRun(), runs: [RunRow.Of(NowRun())]).Render(ctx, Eval, NowId);

        Assert.That(page.FindAll(".run-change"), Is.Empty);
    }

    /// <summary>
    /// The last 8 runs draws a bar for each finished run, this one marked, and lists the criteria missed most with how
    /// often each was flipped.
    /// </summary>
    [Test]
    public void TheLastEightRuns_DrawABarAFinishedRun_AndTheCriteriaMissedMost()
    {
        using var ctx = new BunitContext();
        var page = RenderRun(ctx);

        var bars = page.FindAll(".run-trend-bar");
        var criteria = page.FindAll(".run-trend-criterion").Select(row => row.TextContent).ToList();

        Assert.That(bars, Has.Count.EqualTo(2), "the stopped run isn't drawn");
        Assert.That(bars[1].ClassList, Does.Contain("is-this"));
        Assert.That(criteria[0], Does.Contain("Links the pricing page").And.Contain("missed in 2 of 2"));
        Assert.That(criteria.Single(row => row.Contains("Lights the area")), Does.Contain("you flipped it in 1 of 2"));
    }

    /// <summary>The evals' card says the change against the run before in one line.</summary>
    [TestCase(6, "▲ 1 more than the run before")]
    [TestCase(4, "▼ 1 fewer than the run before")]
    [TestCase(5, "the same as the run before")]
    public void TheEvalsCard_SaysTheChange(int passed, string said)
    {
        using var ctx = new BunitContext();
        var last = RunRow.Of(NowRun()) with { Passed = passed };
        var row = new EvalRow(Eval, "louis-agent-guide", 7, last)
        {
            RunBefore = RunRow.Of(BeforeRun()) with { Passed = 5 }
        };
        var api = new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage()))
            {
                BaseAddress = new Uri("http://localhost/")
            }, new ChatSettings());
        ctx.Services.AddSingleton(new EvalsApi(api));

        var card = ctx.Render<EvalCard>(parameters => parameters.Add(p => p.Row, row));

        Assert.That(card.Find(".eval-card-change").TextContent, Is.EqualTo(said));
    }

    /// <summary>
    /// A conversation's page says how often its flipped criterion was flipped in the last finished runs, and links the
    /// run before by the comparison's rule: the newest earlier run that finished, past a stopped one.
    /// </summary>
    [Test]
    public void AConversationsPage_SaysHowOftenAFlippedCriterionWasFlipped_AndLinksTheRunBefore()
    {
        using var ctx = new BunitContext();
        var host = new EvalPagesHost(NowRun(), runs: Rows()).Keeps(BeforeRun());

        var page = host.Render<ConversationPage>(ctx, parameters => parameters.Add(p => p.Eval, Eval)
            .Add(p => p.Run, NowId).Add(p => p.Conversation, "guide-walk"), ".conversation-loading");

        Assert.That(page.Find(".mark-line-lately").TextContent,
            Does.Contain("You flipped this criterion in 1 of the last 2 runs."));
        Assert.That(page.Find("a.conversation-before").GetAttribute("href"),
            Is.EqualTo(EvalAddress.ConversationPageOf(Eval, BeforeId, "guide-walk")));
    }

    /// <summary>The run's page of the run now, with the run before kept and the eval's three rows.</summary>
    private static IRenderedComponent<RunPage> RenderRun(BunitContext ctx) =>
        new EvalPagesHost(NowRun(), runs: Rows()).Keeps(BeforeRun()).Render(ctx, Eval, NowId);

    /// <summary>The eval's runs: this one, a stopped one this morning, and the run before, the newest first.</summary>
    private static IReadOnlyList<RunRow> Rows()
    {
        var stopped = RunRow.Of(BeforeRun()) with
        {
            Id = StoppedId,
            Status = RunStatus.Stopped,
            StartedAt = Now.AddHours(-4)
        };
        return [RunRow.Of(NowRun()), stopped, RunRow.Of(BeforeRun())];
    }

    /// <summary>The run before: guide-bank failed, guide-walk passed, guide-pricing missed its pricing link.</summary>
    private static EvalRun BeforeRun() =>
        Run(BeforeId, Before, Conversation("guide-bank", "Answers about banks", met: false),
            Conversation("guide-walk", "Lights the area", met: true),
            Conversation("guide-pricing", "Links the pricing page", met: false));

    /// <summary>The run now: guide-bank passes, guide-walk passed after a flip, guide-pricing still fails.</summary>
    private static EvalRun NowRun() =>
        Run(NowId, Now, Conversation("guide-bank", "Answers about banks", met: true),
            Conversation("guide-walk", "Lights the area", met: false, flipped: true),
            Conversation("guide-pricing", "Links the pricing page", met: false));

    private static EvalRun Run(string id, DateTimeOffset started, params ConversationRun[] conversations) =>
        new(id, Eval, "louis-agent-guide", RunKind.Live, 1, started, conversations)
        {
            Status = RunStatus.Finished,
            EndedAt = started.AddMinutes(6)
        };

    /// <summary>A conversation of one repeat of one turn with one criterion, met or not, flipped if asked.</summary>
    private static ConversationRun Conversation(string id, string criterion, bool met, bool flipped = false)
    {
        var transcript = new TurnTranscript("Hello?", "Hello.", [], TimeSpan.FromSeconds(3));
        var mark = new Mark(criterion, met, "Why.") { Flip = flipped ? new Flip(Now.AddHours(1), "It does.") : null };
        return new ConversationRun(id, 1, [new RepeatRun(1, [new TurnRun(transcript, [], [mark], null, null)])]);
    }
}
