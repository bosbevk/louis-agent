using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Identities.Puls.Judges;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// The runner gathers one check catalogue from every owner's list, the harness's general checks, Puls's fixed rules
/// and Puls's judges, so an eval file can name any of them.
/// </summary>
public class RunnerChecksTests
{
    /// <summary>
    /// The runner's catalogue lists the general checks first, then Puls's fixed rules, then its judges, each type
    /// once.
    /// </summary>
    [Test]
    public void TheCatalogue_IsTheGeneralChecks_ThenPulssFixedRules_ThenItsJudges()
    {
        var types = RunnerChecks.Catalog.Listing().Select(line => line.Type);

        Assert.That(types,
            Is.EqualTo(GeneralChecks.All.Concat(PulsChecks.All).Concat(PulsJudges.All).Select(entry => entry.Type)));
    }
}
