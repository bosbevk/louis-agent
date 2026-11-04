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
    /// Its one tool is <c>FindRecipes</c>, tried on one site at a time, at most three sites, reading titles before it
    /// picks one.
    /// </summary>
    [Test]
    public void TheMinion_HasOneTool_FindRecipes_AndReadsTheTitles()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("`FindRecipes(source, query)`"));
        Assert.That(minion, Does.Contain("one site at a time").And.Contain("at most three sites"));
        Assert.That(minion, Does.Contain("a hit can be an article or a category"));
        foreach (string other in new[] { "ReadRecipe", "WebSearch", "FetchUrl", "ReadRenderedPage" })
            Assert.That(minion, Does.Not.Contain(other), $"no other tool: {other}");
    }

    /// <summary>It answers in two shapes: a line for the dish it found, or <c>none</c> with the sites tried.</summary>
    [Test]
    public void TheMinion_AnswersInTwoShapes_AFoundLine_OrNoneWithTheSitesTried()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("`{name} · {site} · {link}`"));
        Assert.That(minion, Does.Contain("`none · tried: {sites}`"));
        Assert.That(minion, Does.Contain("exactly as the hit gives them"), "the name and the link are the hit's");
    }

    /// <summary>
    /// It always tries every site it was given, up to three, and answers one line per site that has the dish; a site
    /// with none adds no line, and <c>none</c> is only for when no site has it (Louis's pick, F35.2 step 2b).
    /// </summary>
    [Test]
    public void TheMinion_TriesEveryGivenSite_AndAnswersOneLinePerSiteThatHasTheDish()
    {
        string minion = Minion();

        Assert.That(minion, Does.Contain("Try every site you were given, at most three"));
        Assert.That(minion, Does.Contain("one line per site that has the dish"));
        Assert.That(minion, Does.Contain("A site with no such hit adds no line"));
        Assert.That(minion, Does.Contain("only when no site you tried has the dish"));
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
