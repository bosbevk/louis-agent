using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Runs.Protocol;

namespace LouisAgent.Api.Tests.Evals.Reports;

/// <summary>
/// A ledger row on a model the run didn't declare gets flagged. A run that declared no model, or only "unknown"
/// (the harness didn't know what a mode ran on), flags nothing, so an older run isn't flagged on every row.
/// </summary>
public class RunModelsTests
{
    /// <summary>Only the models the run didn't declare are flagged, each once.</summary>
    [Test]
    public void AModelTheRunDidNotDeclare_IsFlagged_ThoseItDid_AreNot()
    {
        var declared = new[]
        {
            new EvalProtocol.ModelInfo(SampleMode.Beta, EvalModelLevel.Agent, null, "claude-haiku-5-5")
        };

        Assert.That(RunModels.Unlisted(declared, ["claude-haiku-5-5", "claude-sonnet-5-5", null, "claude-sonnet-5-5"]),
            Is.EqualTo(new[] { "claude-sonnet-5-5" }));
    }

    /// <summary>A run that declared no models flags nothing.</summary>
    [Test]
    public void ARunThatDeclaredNoModel_FlagsNothing() =>
        Assert.That(RunModels.Unlisted(null, ["claude-haiku-5-5"]), Is.Empty);

    /// <summary>A run that only knew its model as unknown flags nothing.</summary>
    [Test]
    public void ARunThatDeclaredOnlyUnknown_FlagsNothing() =>
        Assert.That(
            RunModels.Unlisted(
                [new EvalProtocol.ModelInfo(SampleMode.Beta, EvalModelLevel.Agent, null, null)],
                ["claude-haiku-5-5"]), Is.Empty);
}
