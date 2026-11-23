using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Evals.Client;
using LouisAgent.Web.Evals.Pages;

namespace LouisAgent.Web.Evals.Tests.Pages;

/// <summary>
/// A conversation's page shows one repeat at a time, turn by turn: what was asked, the tool calls folded away, the
/// answer, and the marks, a fact in code type and a criterion with the judge's line. A switch picks the repeat.
/// </summary>
public class ConversationPageTests
{
    private const string Eval = "guide";
    private const string RunId = "20261123-061900-guide";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 6, 19, 0, TimeSpan.Zero);

    /// <summary>Three repeats draw a switch of three links, each with its mark, and the open one marked.</summary>
    [Test]
    public void ThreeRepeats_DrawASwitchOfThree_TheOpenOneMarked()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-invoices", repeat: 3);

        var links = page.FindAll(".repeat-switch a");

        Assert.That(links, Has.Count.EqualTo(3));
        Assert.That(links[1].QuerySelector(".mark-not-met"), Is.Not.Null);
        Assert.That(links[2].GetAttribute("aria-current"), Is.EqualTo("page"));
        Assert.That(links[0].GetAttribute("href"),
            Is.EqualTo(EvalAddress.ConversationPageOf(Eval, RunId, "guide-invoices", 1)));
    }

    /// <summary>With no repeat in the address the first failed one opens, or the first when none failed.</summary>
    [Test]
    public void NoRepeatInTheAddress_OpensTheFirstFailed_OrTheFirst()
    {
        using var ctx = new BunitContext();
        using var other = new BunitContext();

        var failed = Render(ctx, "guide-invoices");
        var passed = Render(other, "guide-what-is");

        Assert.That(failed.Find(".repeat-switch [aria-current=page]").TextContent, Does.Contain("Repeat 2"));
        Assert.That(passed.Find(".repeat-switch [aria-current=page]").TextContent, Does.Contain("Repeat 1"));
    }

    /// <summary>A turn shows its ask, its answer, and a fold naming its tool calls with each input and result.</summary>
    [Test]
    public void ATurn_ShowsItsAskAnswer_AndAFoldOfItsCalls()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-invoices", repeat: 2);

        var turn = page.Find(".turn-card");

        Assert.That(turn.QuerySelector(".turn-ask")!.TextContent, Does.Contain("Can it match invoices?"));
        Assert.That(turn.QuerySelector(".turn-answer")!.TextContent, Does.Contain("I can't do that for you today."));
        Assert.That(turn.QuerySelector(".turn-calls summary")!.TextContent.Trim(),
            Is.EqualTo("2 tool calls · ReadDoc, ShowPage"));
        Assert.That(turn.QuerySelector(".turn-calls")!.TextContent, Does.Contain("{\"path\":\"README.md\"}")
            .And.Contain("# louis-agent"));
    }

    /// <summary>A turn that ended in the agent's error shows the error.</summary>
    [Test]
    public void ATurnThatEndedInAnError_ShowsIt()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-pricing");

        Assert.That(page.Find(".turn-error").TextContent, Does.Contain("The model timed out."));
    }

    /// <summary>A fact is drawn in code type, a criterion in words with the judge's line, each with its mark.</summary>
    [Test]
    public void AFact_IsInCodeType_ACriterion_InWordsWithTheJudgesLine()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-invoices", repeat: 2);

        var fact = page.Find(".mark-line-fact");
        var criterion = page.Find(".mark-line-criterion.mark-line-not-met");

        Assert.That(fact.QuerySelector("code")!.TextContent, Is.EqualTo("used ReadDoc"));
        Assert.That(fact.QuerySelector(".mark-met"), Is.Not.Null);
        Assert.That(criterion.TextContent, Does.Contain("Never answers")
            .And.Contain("the answer opens with \"I can't do that\"."));
    }

    /// <summary>A conversation of one repeat has no switch.</summary>
    [Test]
    public void OneRepeat_HasNoSwitch()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-pricing");

        Assert.That(page.FindAll(".repeat-switch"), Is.Empty);
    }

    /// <summary>A conversation the run doesn't hold is one line naming it.</summary>
    [Test]
    public void AConversationTheRunDoesntHold_IsOneLine()
    {
        using var ctx = new BunitContext();
        var page = Render(ctx, "guide-bank");

        Assert.That(page.Find(".conversation-missing").TextContent, Does.Contain("guide-bank"));
        Assert.That(page.FindAll(".turn-card"), Is.Empty);
    }

    /// <summary>The page for one conversation of the run, at this repeat or none, once it has loaded.</summary>
    private static IRenderedComponent<ConversationPage> Render(BunitContext ctx, string conversation,
        int? repeat = null) =>
        new EvalPagesHost(Run()).Render<ConversationPage>(ctx, parameters => parameters.Add(p => p.Eval, Eval)
                .Add(p => p.Run, RunId).Add(p => p.Conversation, conversation).Add(p => p.Repeat, repeat),
            ".conversation-loading");

    /// <summary>
    /// A run of three conversations: guide-what-is passed both its repeats, guide-invoices failed its second of three
    /// on a criterion after two tool calls, and guide-pricing ended in the agent's error.
    /// </summary>
    private static EvalRun Run() =>
        new(RunId, Eval, "louis-agent-guide", RunKind.Live, 3, Started,
        [
            new ConversationRun("guide-what-is", 2,
                [Repeat(1, "What is louis-agent?", met: true), Repeat(2, "What is louis-agent?", met: true)]),
            new ConversationRun("guide-invoices", 3,
            [
                Repeat(1, "Can it match invoices?", met: true), Repeat(2, "Can it match invoices?", met: false),
                Repeat(3, "Can it match invoices?", met: true)
            ]),
            new ConversationRun("guide-pricing", 1,
                [Repeat(1, "What does it cost?", met: true, "The model timed out.")])
        ])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddSeconds(40)
        };

    /// <summary>A repeat of one turn with two tool calls, a fact met and a criterion met or not.</summary>
    private static RepeatRun Repeat(int number, string ask, bool met, string? error = null)
    {
        List<ToolCallTranscript> calls =
        [
            new("ReadDoc", "{\"path\":\"README.md\"}", "# louis-agent"), new("ShowPage", "{\"page\":\"/\"}", "Shown.")
        ];
        var transcript = new TurnTranscript(ask, "I can't do that for you today.", calls, TimeSpan.FromSeconds(9),
            error);
        var criterion = new Mark("Never answers \"I can't do that\"", met,
            "the answer opens with \"I can't do that\".");
        return new RepeatRun(number,
            [new TurnRun(transcript, [new Mark("used ReadDoc", true, "Called.")], [criterion], 0.0008m, "USD")]);
    }
}
