using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// What the find-recipe minion's file says (F35.2 step 2b, Louis's pick M1: a minion for the search only): it is a
/// minion role of Puls, holds <c>FindRecipes</c> and nothing else, answers in two fixed shapes, and never asks for a
/// recipe's amounts. Reading the page stays the cook's, so the recipe reaches it as the tool printed it.
/// </summary>
public class PulsMinionFindRecipeTests
{
    /// <summary>The minion's file path, the way the catalogue lists it.</summary>
    private const string Path = "minions/puls-minion-find-recipe.md";

    /// <summary>
    /// The minion's file as the model reads it, through the catalogue, on one line; a test that reads it fails, with
    /// the reason, when the file isn't there.
    /// </summary>
    private static string Minion()
    {
        var file = PromptCatalog.Get(Path);
        Assert.That(file, Is.Not.Null, "the minion's file is embedded with the identity's prompts");
        return string.Join(' ', file!.Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>The file is embedded, loads through the catalogue as a minion, and is a role Puls offers.</summary>
    [Test]
    public void TheMinionsFile_IsEmbedded_AndLoadsAsAMinionRole()
    {
        var file = PromptCatalog.Get(Path);

        Assert.That(file, Is.Not.Null, "embedded with the identity's other prompts");
        Assert.That(file!.Header!.Name, Is.EqualTo(PulsRole.MinionFindRecipe.Wire()));
        Assert.That(file.Header.Level, Is.EqualTo(RoleLevel.Minion));
        Assert.That(file.Header.Toolsets, Is.Empty, "a minion gets its one tool from its specialist, not a header");
        Assert.That(RoleCatalog.Available(RoleLevel.Minion), Does.Contain(PulsRole.MinionFindRecipe.Wire()));
        Assert.That(RoleCatalog.MinionBaseOf(PulsRole.MinionFindRecipe.Wire()), Is.EqualTo("puls-minion"));
    }

    /// <summary>
    /// Its one tool is <c>FindRecipes</c>, with no site named, at most three searches, reading titles before it picks.
    /// A site's name here would be the one it always tried first, which is how one site came to hold 19 of 21 recipes.
    /// </summary>
    [Test]
    public void TheMinion_HasOneTool_FindRecipes_NamesNoSite_AndReadsTheTitles()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("`FindRecipes(query)`").And.Contain("leave `source` out"));
        Assert.That(minion, Does.Contain("at most three searches"));
        Assert.That(minion, Does.Not.Contain("foodandhome"));
        Assert.That(minion, Does.Contain("a hit can be an article or a category"));
        foreach (string other in new[] { "ReadRecipe", "WebSearch", "FetchUrl", "ReadRenderedPage" })
            Assert.That(minion, Does.Not.Contain(other), $"no other tool: {other}");
    }

    /// <summary>
    /// A real chat's minion gave three links from one site, one of them a roti for a bobotie, and another said
    /// <c>none</c> with <c>Orange Chicken Marinade</c> in front of it. So: one line per site, the plain dish first, a
    /// variant labelled, a different dish never.
    /// </summary>
    [Test]
    public void TheMinion_RanksThePlainDishFirst_AndNeverGivesADifferentDish()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("one line per site"));
        Assert.That(minion, Does.Contain("the plain dish first"));
        Assert.That(minion, Does.Contain("(variant)"));
        Assert.That(minion, Does.Contain("A roti is not a bobotie"));
    }

    /// <summary>Its next try skips the sites it was just shown, and that list is for this dish only.</summary>
    [Test]
    public void TheMinion_SkipsTheSitesItAsked_WhenItSearchesAgain() =>
        Assert.That(Minion(), Does.Contain("`skip`").And.Contain("the sites the result says it asked"));

    /// <summary>It answers in two shapes: a line for the dish it found, or <c>none</c> with the sites tried.</summary>
    [Test]
    public void TheMinion_AnswersInTwoShapes_AFoundLine_OrNoneWithTheSitesTried()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("`{name} · {site} · {link}`"));
        Assert.That(minion, Does.Contain("`none · tried: {sites}`"));
        Assert.That(minion, Does.Not.Contain("tried: A Recipe Site"),
            "a local model whose search failed copied the example's site names as the ones it tried");
        Assert.That(minion, Does.Contain("never a name from these instructions"));
        Assert.That(minion, Does.Contain("exactly as the hit gives them"), "the name and the link are the hit's");
    }

    /// <summary>
    /// It answers one line per site that has the dish; a site with none adds no line, a search with none means another
    /// search, and <c>none</c> is only for when no site it was shown has it (finding G1 of F35.4).
    /// </summary>
    [Test]
    public void TheMinion_SearchesAgainOnNothing_AndAnswersOneLinePerSiteThatHasTheDish()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("search again"));
        Assert.That(minion, Does.Contain("one line per site that has the dish"));
        Assert.That(minion, Does.Contain("A site with no such hit adds no line"));
        Assert.That(minion, Does.Contain("only when no site you were shown has the dish"));
        Assert.That(minion, Does.Not.Contain("Never read past a hit"), "the first-hit-wins rule is gone");
    }

    /// <summary>It never gives a recipe, an amount or a link it didn't see: reading the page is the cook's.</summary>
    [Test]
    public void TheMinion_NeverAsksForARecipe_AnAmount_OrALinkItDidntSee()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("Never a recipe, an amount, a method or a link you didn't see"));
        Assert.That(minion, Does.Not.Contain("scale"), "no amounts: the cook scales them from the print");
    }
}
