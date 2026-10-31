using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;
using LouisAgent.Packs.Shops.Checks;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// Every identity's and pack's own checks reach the runner through the composition, so the runner gathers them
/// without naming an identity or a pack.
/// </summary>
public class EvalChecksTests
{
    /// <summary>The list holds Puls's fixed rules, then the shops pack's, each in its owner's order.</summary>
    [Test]
    public void TheList_HoldsPulssFixedRules_ThenTheShopsPacks() =>
        Assert.That(EvalChecks.All.Select(entry => entry.Type),
            Is.EqualTo(PulsChecks.All.Concat(ShopsChecks.All).Select(entry => entry.Type)));

    /// <summary>
    /// <c>untooled-shops</c> is the shops pack's: the composition's entry for it is the one the pack lists, and Puls
    /// lists it no more.
    /// </summary>
    [Test]
    public void UntooledShops_IsTheShopsPacks()
    {
        var entry = EvalChecks.All.Single(each => each.Type == "untooled-shops");

        Assert.That(ShopsChecks.All, Has.Member(entry));
        Assert.That(PulsChecks.All.Select(each => each.Type), Has.No.Member("untooled-shops"));
    }
}
