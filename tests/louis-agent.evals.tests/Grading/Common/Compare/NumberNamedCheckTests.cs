using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Compare;

/// <summary>
/// The <c>number-named</c> check: each turn's answer names the number the scenario expects under a key, as the named
/// reader reads it: one number for the first turn, or one per turn. A scenario that asks for none passes.
/// </summary>
public class NumberNamedCheckTests
{
    /// <summary>The check over the plain digits reader, on the <c>serves</c> key.</summary>
    private static readonly NumberNamedCheck Check = new(new Digits(), "serves");

    /// <summary>One number, named in the first turn, passes.</summary>
    [Test]
    public async Task OneNumber_NamedInTheFirstTurn_Passes()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"serves": 4}""", Turn(1, [Text("Bobotie vir 4 mense.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
    }

    /// <summary>One number per turn: each turn must name its own.</summary>
    [Test]
    public async Task OnePerTurn_EachTurnNamesItsOwn_Passes()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"serves": [4, 6]}""",
            Turn(1, [Text("Vir 4 mense.")]), Turn(2, [Text("Nou vir 6.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>A turn that names another number fails, naming the turn, what it named and what was asked.</summary>
    [Test]
    public async Task ATurnNamingAnotherNumber_Fails()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"serves": [4, 6]}""",
            Turn(1, [Text("Vir 4 mense.")]), Turn(2, [Text("Nou vir 5.")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("turn 2").And.Contain("5").And.Contain("6"));
    }

    /// <summary>A turn that names no number at all fails, saying so.</summary>
    [Test]
    public async Task ATurnNamingNoNumber_Fails()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"serves": 4}""", Turn(1, [Text("Vir die hele gesin.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("no number"));
    }

    /// <summary>A scenario that asks for no number passes, as the cook's serving check does.</summary>
    [Test]
    public async Task NoNumberAsked_Passes()
    {
        var grade = await GradeAsync(Check, Trial(Mode, "{}", Turn(1, [Text("Vir die hele gesin.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>
    /// A number asked of a turn that never came, or had no answer, is Unknown: there's nothing to read.
    /// </summary>
    [Test]
    public async Task ATurnWithNothingToRead_IsUnknown()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"serves": [4, 6]}""", Turn(1, [Text("Vir 4 mense.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("turn 2"));
    }

    /// <summary>
    /// A number that isn't a whole number of at least one is refused, naming the scenario and the key.
    /// </summary>
    [TestCase("""{"serves": 0}""")]
    [TestCase("""{"serves": ["four"]}""")]
    [TestCase("""{"serves": []}""")]
    public void ABadNumber_IsRefused(string expected)
    {
        var refusal = Assert.ThrowsAsync<InvalidDataException>(async () =>
            await GradeAsync(Check, Trial(Mode, expected, Turn(1, [Text("4")]))));

        Assert.That(refusal!.Message, Does.Contain("bobotie").And.Contain("serves"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"serves": 4}""", Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, through the reader the file names, and the check lists its
    /// key.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheck_ThroughTheNamedReader()
    {
        var built = Build("""{"key": "serves", "reader": "digits", "required": false}""");

        Assert.That(built, Is.TypeOf<NumberNamedCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("number-named", 1, false)));
        Assert.That(built.ExpectedKeys, Is.EqualTo(new[] { "serves" }));
    }

    /// <summary>
    /// A reader of the wrong kind is refused, naming it: a money reader can't name a number of people.
    /// </summary>
    [Test]
    public void AReaderOfTheWrongKind_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            Build("""{"key": "serves", "reader": "currency-amount"}"""));

        Assert.That(refusal!.Message, Does.Contain("currency-amount"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>
    /// Builds the check the way an eval file does: by its type, with the general readers to name from.
    /// </summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("number-named")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(),
                new CheckContext(string.Empty, [], new ReaderCatalog(GeneralReaders.All)));
}
