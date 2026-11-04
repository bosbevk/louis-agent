using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Tools.Results;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Tools.Results;

/// <summary>
/// The <c>tool-rows-marked</c> check: it lists the rows of a tool's tables whose last cell is the mark. It measures and
/// never judges, so it only ever passes, or is Unknown when a turn broke.
/// </summary>
public class ToolRowsMarkedCheckTests
{
    private const string TwoMissed = """
                                     Estimated prices for the Western Cape.

                                     | Item | Priced from | Estimate |
                                     |---|---|---|
                                     | beef mince 500 g | Beef mince, 500 Gram at R62.00 | ± R62 |
                                     | rotis 5 | no price for this product | ? |
                                     | flaked almonds 100 g | no price for this product | ? |
                                     | **Total** | 1 of 3 items | **± R62 + 2 items unpriced** |

                                     No estimate for: rotis 5, flaked almonds 100 g.
                                     """;

    private const string AllPriced = """
                                     | Item | Priced from | Estimate |
                                     |---|---|---|
                                     | beef mince 500 g | Beef mince, 500 Gram at R62.00 | ± R62 |
                                     | **Total** | 1 of 1 items | **± R62** |
                                     """;

    /// <summary>The call and its result, under one id, as the stream carries them.</summary>
    private static SessionEvent[] Called(string tool, string result) => [ToolCall(tool), ToolResult(result)];

    /// <summary>Grades a trial with the check that looks for a question mark in the prices tool.</summary>
    private static async Task<Grade> GradeAsync(Trial trial) =>
        await new ToolRowsMarkedCheck("EstimatePrices", "?").GradeAsync(trial, CancellationToken.None);

    /// <summary>The marked rows are counted and named by their first cell; the others and the total aren't.</summary>
    [Test]
    public async Task MarkedRows_AreCounted_AndNamed()
    {
        var grade = await GradeAsync(Trial(Turn(1, Called("EstimatePrices", TwoMissed))));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("2 marked ? by EstimatePrices: rotis 5, flaked almonds 100 g"));
        Assert.That(grade.Reason, Does.Not.Contain("beef mince").And.Not.Contain("Total"));
    }

    /// <summary>Nothing marked still passes, and says so.</summary>
    [Test]
    public async Task NoMarkedRow_Passes_SayingSo()
    {
        var grade = await GradeAsync(Trial(Turn(1, Called("EstimatePrices", AllPriced))));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("none marked ? by EstimatePrices"));
    }

    /// <summary>A tool nobody called is no miss: it passes, saying the tool was never called.</summary>
    [Test]
    public async Task NeverCalled_Passes_SayingSo()
    {
        var grade = await GradeAsync(Trial(Turn(1)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain("EstimatePrices was never called"));
    }

    /// <summary>The same row in two turns counts once: the list says what missed, not how often it was asked.</summary>
    [Test]
    public async Task TheSameRowTwice_CountsOnce()
    {
        var grade = await GradeAsync(Trial(
            Turn(1, Called("EstimatePrices", TwoMissed)), Turn(2, Called("EstimatePrices", TwoMissed))));

        Assert.That(grade.Reason, Does.Contain("2 marked ?"));
    }

    /// <summary>A specialist's results count, since the cook is the one who prices a list.</summary>
    [Test]
    public async Task ASpecialistsResults_Count()
    {
        var grade = await GradeAsync(Trial(Turn(1,
        [
            SubToolCall("EstimatePrices"), SubToolResult(TwoMissed)
        ])));

        Assert.That(grade.Reason, Does.Contain("2 marked ?"));
    }

    /// <summary>Another tool's marked rows aren't this tool's.</summary>
    [Test]
    public async Task AnotherToolsRows_AreLeftOut()
    {
        var grade = await GradeAsync(Trial(Turn(1, Called("FindShops", TwoMissed))));

        Assert.That(grade.Reason, Does.Contain("EstimatePrices was never called"));
    }

    /// <summary>A broken turn is Unknown: a list from half a conversation isn't the run's list.</summary>
    [Test]
    public async Task ABrokenTurn_IsUnknown()
    {
        var grade = await GradeAsync(Trial(Turn(1, Called("EstimatePrices", TwoMissed), failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>It can't move a run's pass rate: only a required check counts towards a trial's outcome.</summary>
    [Test]
    public void ItIsNeverRequired() =>
        Assert.That(new ToolRowsMarkedCheck("EstimatePrices", "?").Required, Is.False);

    /// <summary>An eval file gets the check by its type, from the tool and the mark it names.</summary>
    [Test]
    public void TheEntry_BuildsTheCheck()
    {
        var built = Build("""{"tool": "EstimatePrices", "mark": "?"}""");

        Assert.That(built, Is.TypeOf<ToolRowsMarkedCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("tool-rows-marked", 1, false)));
    }

    /// <summary>A blank or missing setting is refused, naming the check type and the field.</summary>
    [TestCase("""{"tool": " ", "mark": "?"}""", "tool")]
    [TestCase("""{"tool": "EstimatePrices"}""", "mark")]
    public void ABlankOrMissingSetting_IsRefused(string settings, string field)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(settings));

        Assert.That(refusal!.Message, Does.Contain("tool-rows-marked").And.Contain(field));
    }

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("tool-rows-marked")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
