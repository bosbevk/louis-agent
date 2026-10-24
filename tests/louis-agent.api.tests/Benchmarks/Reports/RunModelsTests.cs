using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F18 step 2a: a ledger row on a model the run didn't declare is flagged. A run that declared no model (or only "unknown",
// a harness that doesn't know what a mode ran on) flags nothing, so a run from before 2a is never flagged on every row.
public class RunModelsTests
{
    [Test]
    public void AModelTheRunDidNotDeclare_IsFlagged_ThoseItDid_AreNot()
    {
        var declared = new[] { new BenchmarkProtocol.ModelInfo("puls-cook", BenchmarkModelLevel.Agent, null, "claude-haiku-5-5") };

        Assert.That(RunModels.Unlisted(declared, ["claude-haiku-5-5", "claude-sonnet-5-5", null, "claude-sonnet-5-5"]),
            Is.EqualTo(new[] { "claude-sonnet-5-5" }));
    }

    [Test]
    public void ARunThatDeclaredNoModel_FlagsNothing() =>
        Assert.That(RunModels.Unlisted(null, ["claude-haiku-5-5"]), Is.Empty);

    [Test]
    public void ARunThatDeclaredOnlyUnknown_FlagsNothing() =>
        Assert.That(RunModels.Unlisted([new BenchmarkProtocol.ModelInfo("puls-cook", BenchmarkModelLevel.Agent, null, null)], ["claude-haiku-5-5"]), Is.Empty);
}
