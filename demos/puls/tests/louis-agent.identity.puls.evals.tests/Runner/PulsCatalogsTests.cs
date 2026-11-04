using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Identities.Puls.Judges;
using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.WhereToBuy.Checks;

using LouisAgent.PulsEvalRunner;

namespace LouisAgent.Identities.Puls.Evals.Tests.Runner;

/// <summary>
/// The runner gathers one check catalogue from every owner's list, the harness's general checks, each pack's checks
/// project, and Puls's judges, so an eval file can name any of them.
/// </summary>
public class PulsCatalogsTests
{
    /// <summary>
    /// The runner's catalogue lists the general checks first, then the where-to-buy rules, the recipes rule and Puls's
    /// judges, each type once.
    /// </summary>
    [Test]
    public void TheCatalogue_IsTheGeneralChecks_ThenThePacksChecks_ThenPulssJudges()
    {
        var types = PulsCatalogs.All.CheckCatalog.Listing().Select(line => line.Type);

        Assert.That(types,
            Is.EqualTo(GeneralChecks.All.Concat(WhereToBuyChecks.All).Concat(RecipesChecks.All).Concat(PulsJudges.All)
                .Select(entry => entry.Type)));
    }
}
