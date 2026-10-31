using LouisAgent.Packs.Shops.Checks;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// Every identity's and pack's own checks reach the runner through the composition, so the runner gathers them
/// without naming an identity or a pack.
/// </summary>
public class EvalChecksTests
{
    /// <summary>
    /// The list is the shops pack's, in the pack's order: Puls ships readers, not checks, since F49.3.
    /// </summary>
    [Test]
    public void TheList_IsTheShopsPacks() =>
        Assert.That(EvalChecks.All.Select(entry => entry.Type),
            Is.EqualTo(ShopsChecks.All.Select(entry => entry.Type)));

    /// <summary>
    /// <c>untooled-shops</c> and <c>estimates-at-shop</c> are the shops pack's: the composition's entries are the ones
    /// the pack lists.
    /// </summary>
    [TestCase("untooled-shops")]
    [TestCase("estimates-at-shop")]
    public void AShopRule_IsTheShopsPacks(string type) =>
        Assert.That(ShopsChecks.All, Has.Member(EvalChecks.All.Single(each => each.Type == type)));
}
