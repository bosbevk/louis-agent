using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Pages;

namespace LouisAgent.Web.Evals.Tests.Pages;

/// <summary>
/// From a conversation's page a person walks the run's failed conversations in the run page's order, steps back to
/// the one before, and opens the same conversation in the run before this one.
/// </summary>
public class ConversationWalkTests
{
    private const string Eval = "guide";
    private const string RunId = "20261123-061900-guide";
    private const string EarlierId = "20261122-090000-guide";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 19, 0, TimeSpan.Zero);

    /// <summary>Next failed opens the next failed one in the page's order, and is not drawn after the last.</summary>
    [Test]
    public void NextFailed_OpensTheNextFailed_AndIsNotDrawnAfterTheLast()
    {
        using var ctx = new BunitContext();
        using var other = new BunitContext();

        var first = Render(ctx, "guide-b");
        var last = Render(other, "guide-c");

        Assert.That(first.Find("a.conversation-next").GetAttribute("href"),
            Is.EqualTo(EvalAddress.ConversationPageOf(Eval, RunId, "guide-c")));
        Assert.That(last.FindAll("a.conversation-next"), Is.Empty);
    }

    /// <summary>Previous opens the conversation before, in the page's order, and is not drawn on the first.</summary>
    [Test]
    public void Previous_OpensTheOneBefore_AndIsNotDrawnOnTheFirst()
    {
        using var ctx = new BunitContext();
        using var other = new BunitContext();

        var passed = Render(ctx, "guide-a");
        var first = Render(other, "guide-b");

        Assert.That(passed.Find("a.conversation-previous").GetAttribute("href"),
            Is.EqualTo(EvalAddress.ConversationPageOf(Eval, RunId, "guide-c")));
        Assert.That(first.FindAll("a.conversation-previous"), Is.Empty);
    }

    /// <summary>One link opens the same conversation in the newest run of this eval started before this one.</summary>
    [Test]
    public void OneLink_OpensTheSameConversation_InTheRunBefore()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-b");

        Assert.That(page.Find("a.conversation-before").GetAttribute("href"),
            Is.EqualTo(EvalAddress.ConversationPageOf(Eval, EarlierId, "guide-b")));
    }

    /// <summary>On the eval's first run there is no run before, so no link is drawn.</summary>
    [Test]
    public void OnTheEvalsFirstRun_NoLinkToTheRunBefore()
    {
        using var ctx = new BunitContext();
        var page = new EvalPagesHost(Run(), runs: [RunRow.Of(Run())]).Render<ConversationPage>(ctx,
            parameters => parameters.Add(p => p.Eval, Eval).Add(p => p.Run, RunId).Add(p => p.Conversation, "guide-b"),
            ".conversation-loading");

        Assert.That(page.FindAll("a.conversation-before"), Is.Empty);
    }

    /// <summary>The page for one conversation, on a host with this run, a later one and an earlier one.</summary>
    private static IRenderedComponent<ConversationPage> Render(BunitContext ctx, string conversation)
    {
        var later = RunRow.Of(Run()) with
        {
            Id = "20261124-090000-guide",
            StartedAt = Started.AddDays(1)
        };
        var earlier = RunRow.Of(Run()) with
        {
            Id = EarlierId,
            StartedAt = Started.AddDays(-1)
        };
        var host = new EvalPagesHost(Run(), runs: [later, RunRow.Of(Run()), earlier]);
        return host.Render<ConversationPage>(ctx, parameters => parameters.Add(p => p.Eval, Eval)
            .Add(p => p.Run, RunId).Add(p => p.Conversation, conversation), ".conversation-loading");
    }

    /// <summary>A run where guide-a passed and guide-b and guide-c failed, so the order is b, c, a.</summary>
    private static EvalRun Run() =>
        new(RunId, Eval, "louis-agent-guide", RunKind.Live, 1, Started,
        [
            new ConversationRun("guide-a", 1, [Repeat(met: true)]),
            new ConversationRun("guide-b", 1, [Repeat(met: false)]),
            new ConversationRun("guide-c", 1, [Repeat(met: false)])
        ])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddSeconds(20)
        };

    /// <summary>A repeat of one turn whose one fact is met or not.</summary>
    private static RepeatRun Repeat(bool met)
    {
        var transcript = new TurnTranscript("Hi?", "Hello.", [], TimeSpan.FromSeconds(3));
        return new RepeatRun(1, [new TurnRun(transcript, [new Mark("no-error", met, "Why.")], [], null, null)]);
    }
}
