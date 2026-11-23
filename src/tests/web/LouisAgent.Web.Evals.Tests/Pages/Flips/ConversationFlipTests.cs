using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Web.Evals.Pages;

namespace LouisAgent.Web.Evals.Tests.Pages.Flips;

/// <summary>
/// F126.4: on a conversation's page a person flips a criterion's mark. Flip opens a box under the mark for a note;
/// the flip is sent to the mark's address and the run read again, and a flipped mark says what it was flipped to,
/// what the judge said, the note, and how to undo it. A fact is code's, so it has no Flip.
/// </summary>
public class ConversationFlipTests
{
    private const string Eval = "guide";
    private const string RunId = "20261123-131900-guide";
    private const string Conversation = "guide-walkthrough";

    private static readonly DateTimeOffset Started = new(2026, 11, 23, 13, 19, 0, TimeSpan.Zero);

    /// <summary>Each criterion has Flip, and a fact has none.</summary>
    [Test]
    public void EachCriterion_HasFlip_AndAFactHasNone()
    {
        using var ctx = new BunitContext();
        var (page, _) = Render(ctx, Run());

        Assert.That(page.FindAll(".mark-line-criterion .mark-flip"), Has.Count.EqualTo(2));
        Assert.That(page.FindAll(".mark-line-fact .mark-flip"), Is.Empty);
    }

    /// <summary>
    /// Flip opens the box under the mark, its question and its two buttons named for the other answer; "Flip to met"
    /// sends the note to the mark's address and reads the run again, which now shows the mark flipped.
    /// </summary>
    [Test]
    public void FlipToMet_SendsTheNote_AndReadsTheRunAgain()
    {
        using var ctx = new BunitContext();
        var (page, host) = Render(ctx, Run());
        int reads = host.RunReads;

        page.FindAll(".mark-flip")[1].Click();
        var box = page.Find(".flip-box");
        Assert.That(box.TextContent, Does.Contain("Flip to met?").And.Contain("Don't flip"));
        page.Find(".flip-box input").Input("ShowPage lit the band; the judge read the words.");
        page.Find(".flip-box-go").Click();

        page.WaitForAssertion(() => Assert.That(page.FindAll(".mark-line-flipped"), Has.Count.EqualTo(1)));
        Assert.That(host.Flips,
            Is.EqualTo(new[] { (HttpMethod.Put, (string?)"ShowPage lit the band; the judge read the words.") }));
        Assert.That(host.RunReads, Is.GreaterThan(reads));
        Assert.That(page.FindAll(".flip-box"), Is.Empty);
    }

    /// <summary>A met criterion's box flips the other way, to not met.</summary>
    [Test]
    public void AMetCriterion_FlipsToNotMet()
    {
        using var ctx = new BunitContext();
        var (page, _) = Render(ctx, Run());

        page.FindAll(".mark-flip")[0].Click();

        Assert.That(page.Find(".flip-box-go").TextContent, Does.Contain("Flip to not met"));
    }

    /// <summary>"Don't flip" closes the box and sends nothing.</summary>
    [Test]
    public void DontFlip_ClosesTheBox_AndSendsNothing()
    {
        using var ctx = new BunitContext();
        var (page, host) = Render(ctx, Run());

        page.FindAll(".mark-flip")[1].Click();
        page.Find(".flip-box-cancel").Click();

        Assert.That(page.FindAll(".flip-box"), Is.Empty);
        Assert.That(host.Flips, Is.Empty);
    }

    /// <summary>
    /// A flipped mark says what it was flipped to, the judge's own answer and reason, and the note, in the flipped
    /// look; Undo the flip sends the DELETE and reads the run again.
    /// </summary>
    [Test]
    public void AFlippedMark_SaysWhatTheJudgeSaid_AndUndoPutsItBack()
    {
        using var ctx = new BunitContext();
        var (page, host) = Render(ctx, Flipped());

        var mark = page.Find(".mark-line-flipped");
        Assert.That(mark.TextContent, Does.Contain("You flipped this to met.")
            .And.Contain("The judge said not met: \"The answer does not say the area is lit.\"")
            .And.Contain("Your note: The judge read the words."));

        page.Find(".mark-undo").Click();

        page.WaitForAssertion(() => Assert.That(page.FindAll(".mark-line-flipped"), Is.Empty));
        Assert.That(host.Flips.Single().Method, Is.EqualTo(HttpMethod.Delete));
    }

    /// <summary>The head says "Passed after your flip", and the repeat's figures count the flips.</summary>
    [Test]
    public void TheHead_SaysPassedAfterYourFlip_AndTheFiguresCountTheFlips()
    {
        using var ctx = new BunitContext();
        var (page, _) = Render(ctx, Flipped());

        Assert.That(page.Find(".page-head p").TextContent,
            Is.EqualTo("Passed after your flip. 1 of 1 repeat passed. This is repeat 1."));
        Assert.That(page.Find(".conversation-flips").TextContent, Is.EqualTo("1"));
    }

    /// <summary>A flip the API refuses is a line in the API's own words, and the mark stays as it was.</summary>
    [Test]
    public void ARefusedFlip_IsALineInTheApisWords()
    {
        using var ctx = new BunitContext();
        var (page, host) = Render(ctx, Run());
        host.RefusesFlips = "The run is going.";

        page.FindAll(".mark-flip")[1].Click();
        page.Find(".flip-box-go").Click();

        page.WaitForAssertion(() =>
            Assert.That(page.Find(".conversation-refused").TextContent, Does.Contain("The run is going.")));
        Assert.That(page.FindAll(".mark-line-flipped"), Is.Empty);
    }

    /// <summary>The page for the run's one conversation, once it has loaded, and its host.</summary>
    private static (IRenderedComponent<ConversationPage> Page, EvalPagesHost Host) Render(BunitContext ctx,
        EvalRun run)
    {
        var host = new EvalPagesHost(run);
        var page = host.Render<ConversationPage>(ctx, parameters => parameters.Add(p => p.Eval, Eval)
            .Add(p => p.Run, RunId).Add(p => p.Conversation, Conversation), ".conversation-loading");
        return (page, host);
    }

    /// <summary>The run with its one missed criterion flipped to met, with a note.</summary>
    internal static EvalRun Flipped() =>
        Run().WithFlip(Conversation, 1, 1, 2, new Flip(Started.AddHours(1), "The judge read the words."))!;

    /// <summary>
    /// A finished run of one conversation of one repeat: one turn with a fact met, a criterion met and one missed.
    /// </summary>
    internal static EvalRun Run()
    {
        var transcript = new TurnTranscript("Next.", "Next up, packs, glowing below.", [], TimeSpan.FromSeconds(9));
        var turn = new TurnRun(transcript, [new Mark("used ShowPage", true, "Called.")],
        [
            new Mark("Moves to the next area", true, "From levels to packs."),
            new Mark("Lights the area it talks about", false, "The answer does not say the area is lit.")
        ], 0.0009m, "USD");
        return new EvalRun(RunId, Eval, "louis-agent-guide", RunKind.Live, 1, Started,
            [new ConversationRun(Conversation, 1, [new RepeatRun(1, [turn])])])
        {
            Status = RunStatus.Finished,
            EndedAt = Started.AddSeconds(28)
        };
    }
}
