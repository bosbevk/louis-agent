using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Summary;

namespace LouisAgent.Puls.Tests.Admin.Summary;

/// <summary>
/// Each mode's roll-up as the runner wrote it and the API priced it, from the run reply's summary rows (F18 step
/// 5c.3): the portal reads it and works nothing out. A run from before 5c has none (null, not an empty list).
/// </summary>
public class SummaryReaderTests
{
    /// <summary>Three summary lines: two of one mode, one of another.</summary>
    private static readonly IReadOnlyList<SummaryRow> Rows = JsonSerializer.Deserialize<List<SummaryRow>>(
        SampleMode.Fill(
            """
            [{"mode":"<ModeBeta>","slice":"overall","n":8,"passed":6,"failed":1,"unknown":1,"infrastructure":0,
              "tasks":["a","b"],"too_few":false,"pass_rate":0.75,"pass_at_k":1,"pass_hat_k":0.5,
              "cost_per_trial":0.1,"cost_per_pass":0.13},
             {"mode":"<ModeBeta>","slice":"language:af","n":3,"passed":2,"failed":1,"unknown":0,"infrastructure":0,
              "tasks":["a"],"too_few":true,"pass_rate":null,"pass_at_k":null,"pass_hat_k":null,
              "cost_per_trial":null,"cost_per_pass":0.2},
             {"mode":"<ModeGamma>","slice":"overall","n":8,"passed":0,"failed":8,"unknown":0,"infrastructure":0,
              "tasks":["a"],"too_few":false,"pass_rate":0,"pass_at_k":0,"pass_hat_k":0,"cost_per_trial":0}]
            """), AdminJson.SnakeCase)!;

    /// <summary>Each mode keeps its slices in the order they came, the overall line first.</summary>
    [Test]
    public void EachModeKeepsItsSlicesInOrder_TheOverallLineFirst()
    {
        var modes = SummaryReader.Read(Rows)!;

        Assert.That(modes.Select(m => m.Mode), Is.EqualTo(new[] { SampleMode.Beta, SampleMode.Gamma }));
        Assert.That(modes[0].Slices.Select(s => s.Slice), Is.EqualTo(new[] { "overall", "language:af" }));
    }

    /// <summary>A slice reads as the runner and the ledger gave it.</summary>
    [Test]
    public void ASlice_IsReadAsTheRunnerAndTheLedgerGaveIt()
    {
        var overall = SummaryReader.Read(Rows)![0].Slices[0];

        Assert.That((overall.N, overall.Passed, overall.Failed, overall.Unknown, overall.Infrastructure),
            Is.EqualTo((8, 6, 1, 1, 0)));
        Assert.That(overall.Tasks, Is.EqualTo(new[] { "a", "b" }));
        Assert.That((overall.TooFew, overall.PassRate, overall.PassAtK, overall.PassHatK),
            Is.EqualTo((false, (double?)0.75, (double?)1.0, (double?)0.5)));
        Assert.That((overall.CostPerTrial, overall.HasCostPerPass, overall.CostPerPass),
            Is.EqualTo(((decimal?)0.1m, true, (decimal?)0.13m)));
    }

    /// <summary>A slice of too few trials has no rates, and an unknown cost is null, never 0.</summary>
    [Test]
    public void ATooFewSlice_HasNoRates_AndAnUnknownCostIsNull_NeverZero()
    {
        var few = SummaryReader.Read(Rows)![0].Slices[1];

        Assert.That((few.TooFew, few.PassRate, few.PassAtK, few.PassHatK),
            Is.EqualTo((true, (double?)null, (double?)null, (double?)null)));
        Assert.That(few.CostPerTrial, Is.Null);
    }

    /// <summary>A slice where nothing passed has no cost per pass.</summary>
    [Test]
    public void ASliceWithNoCostPerPass_BecauseNothingPassed_HasNone() =>
        Assert.That(SummaryReader.Read(Rows)![1].Slices[0].HasCostPerPass, Is.False);

    /// <summary>A run from before 5c has no roll-up: null, not an empty list.</summary>
    [Test]
    public void ARunFromBefore5c_HasNoRollUp_NullNotAnEmptyList() =>
        Assert.That(SummaryReader.Read(null), Is.Null);
}
