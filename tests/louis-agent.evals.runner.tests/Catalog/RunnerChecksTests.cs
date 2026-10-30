using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using LouisAgent.Evals.Runner.Catalog;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// The runner gathers one check catalogue from every owner's list, the harness's general checks and Puls's own, so an
/// eval file can name any of them.
/// </summary>
public class RunnerChecksTests
{
    /// <summary>The runner's catalogue lists the general checks first, then Puls's, each type once.</summary>
    [Test]
    public void TheCatalogue_IsTheGeneralChecksThenPulss()
    {
        var types = RunnerChecks.Catalog.Listing().Select(line => line.Type);

        Assert.That(types,
            Is.EqualTo(GeneralChecks.All.Concat(PulsChecks.All).Select(entry => entry.Type)));
    }
}
