using System.Text.Json;
using LouisAgent.Puls.Admin.Rates;

namespace LouisAgent.Puls.Tests.Admin.Rates;

// F18 step 5a.2: the run's JSON carries each mode's pass@k and pass^k as the runner wrote them (cumulative over the repeats, so the last repeat's are the mode's);
// the portal reads and words them, and works nothing out. Null where there is none: a dash, never 0. Written first: the reader and the words threw
// NotImplementedException on the red run.
public class ModeRatesTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    [Test]
    public void AModesRates_AreTheLastRepeatsThatHasThem()
    {
        var read = ModeRatesReader.Read(Json("""
            {"modes":[
              {"name":"puls-cook","repeat":1,"pass_at_k":1,"pass_hat_k":1,"k":1,"infrastructure":0,"retried":0},
              {"name":"puls-cook","repeat":2,"pass_at_k":1,"pass_hat_k":0.5,"k":2,"infrastructure":1,"retried":1},
              {"name":"puls-only","repeat":1,"pass_at_k":0,"pass_hat_k":0,"k":1,"infrastructure":0,"retried":0}]}
            """));

        Assert.That(read, Is.EqualTo(new[]
        {
            new ModeRate("puls-cook", 2, 1.0, 0.5, 1, 1),
            new ModeRate("puls-only", 1, 0.0, 0.0, 0, 0),
        }));
    }

    [Test]
    public void ARateNoTrialMade_StaysNull_NeverZero()
    {
        var read = ModeRatesReader.Read(Json("""{"modes":[{"name":"puls-cook","repeat":1,"pass_at_k":null,"pass_hat_k":null,"k":1,"infrastructure":3,"retried":0}]}"""));

        Assert.That(read.Single(), Is.EqualTo(new ModeRate("puls-cook", 1, null, null, 3, 0)));
    }

    [Test]
    public void AModeFromBeforeTheRates_HasNoK_AndNoRates()
    {
        var read = ModeRatesReader.Read(Json("""{"modes":[{"name":"puls-cook","repeat":1,"toolsets":["web"],"role":null,"llm":null}]}"""));

        Assert.That(read.Single(), Is.EqualTo(new ModeRate("puls-cook", null, null, null, 0, 0)));
    }

    [Test]
    public void ARunWithNoModes_HasNone() =>
        Assert.That(ModeRatesReader.Read(Json("""{"modes":[]}""")), Is.Empty);

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
