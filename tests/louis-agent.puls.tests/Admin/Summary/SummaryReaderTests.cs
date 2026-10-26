using System.Text.Json;
using LouisAgent.Puls.Admin.Summary;

namespace LouisAgent.Puls.Tests.Admin.Summary;

// F18 step 5c.3: the run's JSON carries each mode's roll-up as the runner wrote it and the API priced it (`summaries`); the portal reads it and works nothing out. A run from before 5c
// has none (null, not an empty list), and the portal says so. Written first: the reader threw NotImplementedException on the red run.
public class SummaryReaderTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private const string Run = """
        {"summaries":[
          {"mode":"puls-cook","slice":"overall","n":8,"passed":6,"failed":1,"unknown":1,"infrastructure":0,"tasks":["a","b"],"too_few":false,"pass_rate":0.75,"pass_at_k":1,"pass_hat_k":0.5,"cost_per_trial":0.1,"cost_per_pass":0.13},
          {"mode":"puls-cook","slice":"language:af","n":3,"passed":2,"failed":1,"unknown":0,"infrastructure":0,"tasks":["a"],"too_few":true,"pass_rate":null,"pass_at_k":null,"pass_hat_k":null,"cost_per_trial":null,"cost_per_pass":0.2},
          {"mode":"puls-tools","slice":"overall","n":8,"passed":0,"failed":8,"unknown":0,"infrastructure":0,"tasks":["a"],"too_few":false,"pass_rate":0,"pass_at_k":0,"pass_hat_k":0,"cost_per_trial":0}]}
        """;

    [Test]
    public void EachModeKeepsItsSlicesInOrder_TheOverallLineFirst()
    {
        var modes = SummaryReader.Read(Json(Run))!;

        Assert.That(modes.Select(m => m.Mode), Is.EqualTo(new[] { "puls-cook", "puls-tools" }));
        Assert.That(modes[0].Slices.Select(s => s.Slice), Is.EqualTo(new[] { "overall", "language:af" }));
    }

    [Test]
    public void ASlice_IsReadAsTheRunnerAndTheLedgerGaveIt()
    {
        var overall = SummaryReader.Read(Json(Run))![0].Slices[0];

        Assert.That((overall.N, overall.Passed, overall.Failed, overall.Unknown, overall.Infrastructure), Is.EqualTo((8, 6, 1, 1, 0)));
        Assert.That(overall.Tasks, Is.EqualTo(new[] { "a", "b" }));
        Assert.That((overall.TooFew, overall.PassRate, overall.PassAtK, overall.PassHatK), Is.EqualTo((false, (double?)0.75, (double?)1.0, (double?)0.5)));
        Assert.That((overall.CostPerTrial, overall.HasCostPerPass, overall.CostPerPass), Is.EqualTo(((decimal?)0.1m, true, (decimal?)0.13m)));
    }

    [Test]
    public void ATooFewSlice_HasNoRates_AndAnUnknownCostIsNull_NeverZero()
    {
        var few = SummaryReader.Read(Json(Run))![0].Slices[1];

        Assert.That((few.TooFew, few.PassRate, few.PassAtK, few.PassHatK), Is.EqualTo((true, (double?)null, (double?)null, (double?)null)));
        Assert.That(few.CostPerTrial, Is.Null);
    }

    [Test]
    public void ASliceWithNoCostPerPass_BecauseNothingPassed_HasNone() =>
        Assert.That(SummaryReader.Read(Json(Run))![1].Slices[0].HasCostPerPass, Is.False);

    [TestCase("""{"summaries":null}""")]
    [TestCase("""{"run":{"id":"x"}}""")]
    public void ARunFromBefore5c_HasNoRollUp_NullNotAnEmptyList(string run) =>
        Assert.That(SummaryReader.Read(Json(run)), Is.Null);
}
