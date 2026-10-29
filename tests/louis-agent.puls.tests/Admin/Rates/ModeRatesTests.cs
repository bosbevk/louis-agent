using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Rates;

namespace LouisAgent.Puls.Tests.Admin.Rates;

/// <summary>
/// Each mode's pass@k and pass^k as the runner wrote them on the run's mode rows (F18 step 5a.2): cumulative over the
/// repeats, so the last repeat's are the mode's. The portal reads and words them, and works nothing out; a rate no
/// trial made is a dash, never 0.
/// </summary>
public class ModeRatesTests
{
    /// <summary>A mode's row for one repeat, with its rates or none.</summary>
    private static ModeRow Row(string mode, int repeat, double? passAtK = null, double? passHatK = null, int? k = null,
        int? infrastructure = null, int? retried = null) =>
        new(mode, repeat, [], null, null, passAtK, passHatK, k, infrastructure, retried);

    /// <summary>A mode's rates are its last repeat's that has them.</summary>
    [Test]
    public void AModesRates_AreTheLastRepeatsThatHasThem()
    {
        var read = ModeRatesReader.Read(
        [
            Row(SampleMode.Beta, 1, 1, 1, 1, 0, 0),
            Row(SampleMode.Beta, 2, 1, 0.5, 2, 1, 1),
            Row(SampleMode.Alpha, 1, 0, 0, 1, 0, 0),
        ]);

        Assert.That(read,
            Is.EqualTo(new[]
            {
                new ModeRate(SampleMode.Beta, 2, 1.0, 0.5, 1, 1), new ModeRate(SampleMode.Alpha, 1, 0.0, 0.0, 0, 0),
            }));
    }

    /// <summary>A rate no trial made stays null, never 0.</summary>
    [Test]
    public void ARateNoTrialMade_StaysNull_NeverZero() =>
        Assert.That(ModeRatesReader.Read([Row(SampleMode.Beta, 1, null, null, 1, 3, 0)]).Single(),
            Is.EqualTo(new ModeRate(SampleMode.Beta, 1, null, null, 3, 0)));

    /// <summary>A mode from before the rates has no k and no rates.</summary>
    [Test]
    public void AModeFromBeforeTheRates_HasNoK_AndNoRates() =>
        Assert.That(ModeRatesReader.Read([Row(SampleMode.Beta, 1)]).Single(),
            Is.EqualTo(new ModeRate(SampleMode.Beta, null, null, null, 0, 0)));

    /// <summary>A run with no modes has no rates.</summary>
    [Test]
    public void ARunWithNoModes_HasNone() =>
        Assert.That(ModeRatesReader.Read([]), Is.Empty);

    /// <summary>The words name the k they are over, and a dash stands for none.</summary>
    [Test]
    public void TheWords_NameTheKTheyAreOver_AndShowADashForNone()
    {
        Assert.That(RatesText.PassAt(3), Is.EqualTo("pass@3"));
        Assert.That(RatesText.PassHat(3), Is.EqualTo("pass^3"));
        Assert.That(RatesText.PassAt(null), Is.EqualTo("pass@k"));
        Assert.That(RatesText.Share(0.5), Is.EqualTo("50%"));
        Assert.That(RatesText.Share(1), Is.EqualTo("100%"));
        Assert.That(RatesText.Share(0), Is.EqualTo("0%"), "a rate of 0 is a rate");
        Assert.That(RatesText.Share(null), Is.EqualTo("–"));
        Assert.That(RatesText.LeftOut(1), Is.EqualTo("1 left out (infrastructure)"));
        Assert.That(RatesText.Retried(2), Is.EqualTo("2 retried"));
    }
}
