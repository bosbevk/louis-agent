using LouisAgent.Composition;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Identities.Puls.Judges;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// The runner gathers one check catalogue from every owner's list, the harness's general checks, every identity's and
/// pack's own through the composition, and Puls's judges, so an eval file can name any of them.
/// </summary>
public class RunnerChecksTests
{
    /// <summary>
    /// The runner's catalogue lists the general checks first, then the composition's (Puls's fixed rules, then the
    /// shops pack's), then Puls's judges, each type once.
    /// </summary>
    [Test]
    public void TheCatalogue_IsTheGeneralChecks_ThenTheComposedChecks_ThenPulssJudges()
    {
        var types = RunnerChecks.Catalog.Listing().Select(line => line.Type);

        Assert.That(types,
            Is.EqualTo(GeneralChecks.All.Concat(EvalChecks.All).Concat(PulsJudges.All).Select(entry => entry.Type)));
    }
}
