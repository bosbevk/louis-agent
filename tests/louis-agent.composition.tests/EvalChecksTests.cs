using LouisAgent.Packs.Recipes.Checks;
using LouisAgent.Packs.Shops.Checks;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// Every identity's and pack's own checks reach the runner through the composition, so the runner gathers them
/// without naming an identity or a pack.
/// </summary>
public class EvalChecksTests
{
    /// <summary>
    /// The list is the shops pack's, then the recipes pack's, each in its own order: Puls ships readers, not checks,
    /// since F49.3.
    /// </summary>
    [Test]
    public void TheList_IsTheShopsPacksThenTheRecipesPacks() =>
        Assert.That(EvalChecks.All.Select(entry => entry.Type),
            Is.EqualTo(ShopsChecks.All.Concat(RecipesChecks.All).Select(entry => entry.Type)));

    /// <summary>
    /// <c>untooled-shops</c> and <c>estimates-at-shop</c> are the shops pack's: the composition's entries are the ones
    /// the pack lists.
    /// </summary>
    [TestCase("untooled-shops")]
    [TestCase("estimates-at-shop")]
    public void AShopRule_IsTheShopsPacks(string type) =>
        Assert.That(ShopsChecks.All, Has.Member(EvalChecks.All.Single(each => each.Type == type)));

    /// <summary>
    /// <c>recipe-from-source</c> is the recipes pack's: the composition's entry is the one the pack lists.
    /// </summary>
    [Test]
    public void TheRecipeRule_IsTheRecipesPacks() =>
        Assert.That(RecipesChecks.All,
            Has.Member(EvalChecks.All.Single(each => each.Type == "recipe-from-source")));
}
