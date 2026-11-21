using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Answers;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Answers;

/// <summary>
/// The <c>is-json</c> check: the first answer parses as JSON, either whole or the inside of its one fenced block, and
/// a fail quotes what the JSON reader said.
/// </summary>
public class IsJsonCheckTests
{
    /// <summary>An answer that is JSON, with spaces around it, passes.</summary>
    [Test]
    public async Task AnUnfencedAnswerThatIsJson_Passes()
    {
        var grade = await GradeAsync(new IsJsonCheck(), Trial(Turn(1, [Text("""  {"meal": true, "serves": 4}  """)])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    /// <summary>
    /// The inside of the answer's one fenced block is what's read, with <c>json</c> after the backticks.
    /// </summary>
    [Test]
    public async Task TheInsideOfOneFencedBlock_IsRead()
    {
        var grade = await GradeAsync(new IsJsonCheck(),
            Trial(Turn(1, [Text("Here it is:\n```json\n{\"meal\": true}\n```\nEnjoy.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>An answer that isn't JSON fails, and the reason quotes the JSON reader.</summary>
    [Test]
    public async Task AnAnswerThatIsNotJson_Fails_QuotingTheReader()
    {
        var grade = await GradeAsync(new IsJsonCheck(), Trial(Turn(1, [Text("{\"meal\": true,}")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("isn't JSON").And.Contain("trailing comma"));
    }

    /// <summary>Two fenced blocks are a fail that says so, not a guess at which one is meant.</summary>
    [Test]
    public async Task TwoFencedBlocks_Fail_SayingSo()
    {
        var grade = await GradeAsync(new IsJsonCheck(),
            Trial(Turn(1, [Text("```json\n{\"a\": 1}\n```\nand\n```\n{\"b\": 2}\n```")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("4"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown, never a fail.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(new IsJsonCheck(), Trial(Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, under its name and version, required by default.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheckFromItsSettings()
    {
        var built = Build("{}");

        Assert.That(built, Is.TypeOf<IsJsonCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("is-json", 1, true)));
    }

    /// <summary>A setting it doesn't take is refused, naming the check type and the field.</summary>
    [Test]
    public void ASettingItDoesNotTake_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("""{"schema": "x"}"""));

        Assert.That(refusal!.Message, Does.Contain("is-json").And.Contain("schema"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>Builds the check the way an eval file does: from the general checks' catalogue, by its type.</summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("is-json")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
