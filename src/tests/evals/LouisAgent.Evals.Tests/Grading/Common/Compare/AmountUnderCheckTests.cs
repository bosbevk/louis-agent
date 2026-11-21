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
/// The <c>amount-under</c> check: the first answer's total, as the named reader reads it, is at most the amount the
/// scenario expects under a key. A scenario that sets no amount passes; one that sets a bad amount is refused.
/// </summary>
public class AmountUnderCheckTests
{
    /// <summary>The check over the plain currency reader, on the <c>budget</c> key.</summary>
    private static readonly AmountUnderCheck Check = new(new CurrencyAmount(), "budget");

    /// <summary>A total at the expected amount passes, and the reason gives both.</summary>
    [Test]
    public async Task ATotalAtTheAmount_Passes()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"budget": 150}""", Turn(1, [Text("Totaal: R150,00")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Pass, 1.0)));
        Assert.That(grade.Reason, Does.Contain("150"));
    }

    /// <summary>A total over the amount fails, and the reason gives the total and the amount.</summary>
    [Test]
    public async Task ATotalOverTheAmount_Fails()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"budget": 150}""", Turn(1, [Text("Totaal: R189,99")])));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("189.99").And.Contain("150"));
    }

    /// <summary>
    /// A scenario that sets no amount passes, as the cook's budget check does: there is nothing to keep under.
    /// </summary>
    [Test]
    public async Task NoAmountSet_Passes()
    {
        var grade = await GradeAsync(Check, Trial(Mode, "{}", Turn(1, [Text("Totaal: R189,99")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>An answer the reader reads no total from is Unknown: the check can't tell.</summary>
    [Test]
    public async Task NoTotalRead_IsUnknown()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"budget": 150}""", Turn(1, [Text("Kook dit self.")])));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>An amount that isn't a number above zero is refused, naming the scenario and the key.</summary>
    [TestCase("""{"budget": 0}""")]
    [TestCase("""{"budget": "cheap"}""")]
    public void ABadAmount_IsRefused(string expected)
    {
        var refusal = Assert.ThrowsAsync<InvalidDataException>(async () =>
            await GradeAsync(Check, Trial(Mode, expected, Turn(1, [Text("R10")]))));

        Assert.That(refusal!.Message, Does.Contain("bobotie").And.Contain("budget"));
    }

    /// <summary>A turn the infrastructure broke can't be judged, so it is Unknown.</summary>
    [Test]
    public async Task AnInfrastructureFailure_IsUnknown()
    {
        var grade = await GradeAsync(Check, Trial(Mode, """{"budget": 150}""", Turn(1, failure: TurnFailure.Timeout)));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, through the reader the file names, and the check lists its
    /// key so the loader can see the key is read and fed.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheck_ThroughTheNamedReader()
    {
        var built = Build("""{"key": "budget", "reader": "currency-amount"}""");

        Assert.That(built, Is.TypeOf<AmountUnderCheck>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("amount-under", 1, true)));
        Assert.That(built.ExpectedKeys, Is.EqualTo(new[] { "budget" }));
    }

    /// <summary>A reader the catalogue doesn't have, or one of the wrong kind, is refused, naming it.</summary>
    [TestCase("rand-total")]
    [TestCase("digits")]
    public void AnUnknownOrWrongReader_IsRefused(string reader)
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            Build($$"""{"key": "budget", "reader": "{{reader}}"}"""));

        Assert.That(refusal!.Message, Does.Contain(reader));
    }

    /// <summary>A blank key is refused, naming the check type and the field.</summary>
    [Test]
    public void ABlankKey_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("""{"key": " ", "reader": "currency-amount"}"""));

        Assert.That(refusal!.Message, Does.Contain("amount-under").And.Contain("key"));
    }

    /// <summary>Grades the trial with the check.</summary>
    private static async Task<Grade> GradeAsync(ICheck check, Trial trial) =>
        await check.GradeAsync(trial, CancellationToken.None);

    /// <summary>
    /// Builds the check the way an eval file does: by its type, with the general readers to name from.
    /// </summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("amount-under")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(),
                new CheckContext(string.Empty, [], new ReaderCatalog(GeneralReaders.All)));
}
