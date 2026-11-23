using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Pages;

namespace LouisAgent.Web.Evals.Tests.Pages;

/// <summary>
/// A run's page gives the number first, then what the run ran with, then its conversations, the failed ones first,
/// each saying which repeat failed and on what. Every figure is the run's own answer.
/// </summary>
public class RunPageTests
{
    private const string Eval = "guide";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 19, 0, TimeSpan.Zero);

    /// <summary>A finished run shows passed of conversations, its kind, repeats, time, cost and marks met.</summary>
    [Test]
    public void AFinishedRun_ShowsPassedOfConversations_AndWhatItRanWith()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Run());

        string figures = page.Find(".run-this").TextContent;

        Assert.That(page.Find(".run-passed").TextContent.Trim(), Is.EqualTo("1"));
        Assert.That(figures, Does.Contain("of 3 passed").And.Contain("live").And.Contain("6 min 12 s")
            .And.Contain("$0.003").And.Contain("Facts met").And.Contain("Criteria met").And.Contain("7 of 8"));
    }

    /// <summary>
    /// F126.3.4: a running run's figures say how long it has gone so far by the page's clock, and what it has cost
    /// the agent and the judge so far.
    /// </summary>
    [Test]
    public void ARunningRunsFigures_SaySoFar_AndBothCostsSoFar()
    {
        using var ctx = new BunitContext();
        var host = new EvalPagesHost(null);
        var going = Run() with
        {
            Status = RunStatus.Running,
            EndedAt = null,
            StartedAt = host.Clock.GetUtcNow().AddSeconds(-158)
        };
        host.Answers(Judged(going));
        var page = host.Render(ctx, Eval, going.Id);

        string figures = page.Find(".run-this").TextContent;

        Assert.That(figures, Does.Contain("So far").And.Contain("2 min 38 s").And.Contain("The agent's cost")
            .And.Contain("$0.003").And.Contain("The judge's cost"));
        Assert.That(page.Find(".run-judge-cost").TextContent.Trim(), Is.EqualTo("$0.003"));
        Assert.That(figures, Does.Not.Contain("Took"));
    }

    /// <summary>A finished run shows how long it took and both its costs; a cost not known reads unknown.</summary>
    [Test]
    public void AFinishedRun_ShowsTook_AndBothCosts_AnUnknownOneAsUnknown()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Run());

        string figures = page.Find(".run-this").TextContent;

        Assert.That(figures, Does.Contain("Took").And.Contain("The judge's cost"));
        Assert.That(page.Find(".run-judge-cost").TextContent.Trim(), Is.EqualTo("unknown"));
    }

    /// <summary>A run whose every priced turn cost the judge what it cost the agent.</summary>
    private static EvalRun Judged(EvalRun run) => run with
    {
        Conversations =
        [
            .. run.Conversations.Select(c => c with
            {
                Repeats =
                [
                    .. c.Repeats.Select(r =>
                        r with { Turns = [.. r.Turns.Select(t => t with { JudgeCost = t.Cost })] })
                ]
            })
        ]
    };

    /// <summary>A run whose turns have no price reads its cost as unknown, never as nothing spent.</summary>
    [Test]
    public void ACostWithNoPrice_ReadsUnknown()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Run(cost: null));

        Assert.That(page.Find(".run-cost").TextContent.Trim(), Is.EqualTo("unknown"));
    }

    /// <summary>The conversations come failed first, as the run orders them.</summary>
    [Test]
    public void TheConversations_ComeFailedFirst()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Run());

        Assert.That(page.FindAll(".conversation-row-id").Select(id => id.TextContent.Trim()),
            Is.EqualTo(new[] { "guide-invoices", "guide-pricing", "guide-what-is" }));
    }

    /// <summary>
    /// A failed row says which repeat failed and on what, in the first miss's words, and how many repeats passed.
    /// </summary>
    [Test]
    public void AFailedRow_SaysWhichRepeatFailedAndOnWhat()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Run());

        var rows = page.FindAll(".conversation-row");

        Assert.That(rows[0].QuerySelector(".conversation-row-miss")!.TextContent.Trim(),
            Is.EqualTo("Repeat 2, not met: Never answers \"I can't do that\""));
        Assert.That(rows[0].QuerySelector(".conversation-row-repeats")!.TextContent.Trim(),
            Is.EqualTo("2 of 3 repeats"));
        Assert.That(rows[1].QuerySelector(".conversation-row-miss")!.TextContent.Trim(),
            Is.EqualTo("Repeat 1, fact not met: used ShowPage"));
    }

    /// <summary>A passed row says no miss, and ends with every repeat passed.</summary>
    [Test]
    public void APassedRow_HasNoMiss_AndEndsWithEveryRepeat()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, Run());

        var passed = page.FindAll(".conversation-row")[2];

        Assert.That(passed.QuerySelectorAll(".conversation-row-miss"), Is.Empty);
        Assert.That(passed.QuerySelector(".conversation-row-repeats")!.TextContent.Trim(),
            Is.EqualTo("3 of 3 repeats"));
        Assert.That(passed.QuerySelector(".conversation-row-ask")!.TextContent, Does.Contain("What is louis-agent?"));
    }

    /// <summary>A row's link is the conversation's own address in this run.</summary>
    [Test]
    public void ARowsLink_IsTheConversationsAddress()
    {
        using var ctx = new BunitContext();
        var run = Run();
        var page = Render(ctx, run);

        Assert.That(page.Find("a.conversation-row").GetAttribute("href"),
            Is.EqualTo(EvalAddress.ConversationPageOf(Eval, run.Id, "guide-invoices")));
    }

    /// <summary>The line under the figures says whether the agent was asked with the visitor's page.</summary>
    [Test]
    public void TheLineUnderTheFigures_SaysWhetherThePageWasSent()
    {
        using var ctx = new BunitContext();
        using var other = new BunitContext();

        string without = Render(ctx, Run()).Find(".run-asked").TextContent;
        string with = Render(other, Run() with { PageSent = true }).Find(".run-asked").TextContent;

        Assert.That(without, Does.Contain("louis-agent-guide").And.Contain("without the page"));
        Assert.That(with, Does.Contain("with the page").And.Not.Contain("without"));
    }

    /// <summary>
    /// A run of three repeats: guide-what-is passed all three, guide-invoices failed its second on a criterion, and
    /// guide-pricing, asked once, failed on a fact. It took 6 min 12 s.
    /// </summary>
    private static EvalRun Run(decimal? cost = 0.001m) =>
        new("20261123-061900-guide", Eval, "louis-agent-guide", RunKind.Live, 3, Started,
        [
            new ConversationRun("guide-what-is", 3,
            [
                Repeat(1, "What is louis-agent?", cost), Repeat(2, "What is louis-agent?", null),
                Repeat(3, "What is louis-agent?", null)
            ]),
            new ConversationRun("guide-invoices", 3,
            [
                Repeat(1, "Can it match invoices?", cost),
                Repeat(2, "Can it match invoices?", cost, criterion: "Never answers \"I can't do that\""),
                Repeat(3, "Can it match invoices?", null)
            ]),
            new ConversationRun("guide-pricing", 1,
                [Repeat(1, "Where can I see what it costs?", null, fact: "used ShowPage")])
        ])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddMinutes(6).AddSeconds(12)
        };

    /// <summary>A repeat of one turn, with a fact or a criterion not met when one is named.</summary>
    private static RepeatRun Repeat(int number, string ask, decimal? cost, string? fact = null,
        string? criterion = null)
    {
        var transcript = new TurnTranscript(ask, "An answer.", [], TimeSpan.FromSeconds(4));
        List<Mark> facts = [new("no-error", true, "No error.")];
        if (fact is not null) facts.Add(new Mark(fact, false, "It didn't."));
        List<Mark> criteria = [new("Answers the ask", true, "It does.")];
        if (criterion is not null) criteria.Add(new Mark(criterion, false, "It said it can't."));
        return new RepeatRun(number, [new TurnRun(transcript, facts, criteria, cost, cost is null ? null : "USD")]);
    }

    /// <summary>The run's page over a stub of the API that has this run, once it has loaded.</summary>
    private static IRenderedComponent<RunPage> Render(BunitContext ctx, EvalRun run) =>
        new EvalPagesHost(run).Render(ctx, Eval, run.Id);
}
