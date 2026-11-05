using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Packs.Recipes;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// What the cook's prompt says about reading a recipe now that the recipes pack does it (F35.2 steps 2 and 2b): a
/// minion searches with <c>FindRecipes</c> and the cook reads the link with <c>ReadRecipe</c>, what the print gives
/// that the cook only passes on, and what is gone. A prompt that trusts code for facts and keeps judgment for itself
/// (ADR 0067, GOAL skill 5).
/// </summary>
public class PulsCookRecipeToolsTests
{
    /// <summary>The cook's file as the model reads it, through the catalogue, on one line.</summary>
    private static string Cook() =>
        string.Join(' ', PromptCatalog.Get("specialists/puls-cook.md")!.Body.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The cook starts the find-recipe minion for the search, with one example task, and reads the link it gives with
    /// <c>ReadRecipe</c>, in that order.
    /// </summary>
    [Test]
    public void TheCook_StartsTheMinionForTheSearch_ThenReadsItsLinkWithReadRecipe()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("`StartMinion(\"puls-minion-find-recipe\", \"FindRecipes\", "));
        Assert.That(cook, Does.Contain("Dish: bobotie. The user's language: Afrikaans."),
            "one example task: the dish and the language");
        Assert.That(cook, Does.Contain("`{name} · {site} · {link}`").And.Contain("`none · tried: {sites}`"));
        Assert.That(cook, Does.Contain("`ReadRecipe` its links in order"));
        Assert.That(cook, Does.Contain("*publishes no recipe data*"), "a text print is read as the page's text, once");
        Assert.That(cook.IndexOf("`StartMinion(", StringComparison.Ordinal),
            Is.LessThan(cook.IndexOf("`ReadRecipe` its links in order", StringComparison.Ordinal)));
    }

    /// <summary>
    /// The cook no longer calls <c>FindRecipes</c> itself (the minion does) and says so plainly, since the tool stays
    /// in its toolset for the minion to hold; a <c>none</c> still falls back to <c>WebSearch</c> then
    /// <c>ReadRecipe</c>.
    /// </summary>
    [Test]
    public void TheCook_NeverCallsFindRecipesItself_AndFallsBackToWebSearchOnNone()
    {
        string cook = Cook();

        Assert.That(cook, Does.Not.Contain("`FindRecipes(source, query)`"), "the minion's call, not the cook's");
        Assert.That(cook, Does.Contain("you never call it yourself"));
        Assert.That(cook, Does.Contain("`none`").And.Contain("`WebSearch` with the short query, then `ReadRecipe`"));
        Assert.That(cook, Does.Contain("Your first tool calls are `GetToday` and `StartMinion`, nothing else"));
        Assert.That(cook, Does.Contain("`StartMinion` for the search"));
        Assert.That(cook, Does.Not.Contain("`FindRecipes` and `ReadRecipe` for the recipe"));
    }

    /// <summary>
    /// The minion gives one link per site, so the cook reads the links in order under the stop and tie rules, and goes
    /// to <c>WebSearch</c> on <c>none</c>, on a minion that errors or times out, or when every link is used up with
    /// none fitting.
    /// </summary>
    [Test]
    public void TheCook_ReadsTheMinionsLinksInOrder_AndTreatsAnErrorAsNone()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("one line per site that has the dish"));
        Assert.That(cook, Does.Contain("`ReadRecipe` its links in order"));
        Assert.That(cook, Does.Contain("when every link is used up with none fitting, go to (5)"));
        Assert.That(cook, Does.Contain("A minion that errors or times out counts as `none`: go to (5)"));
    }

    /// <summary>
    /// The stop rule counts the prints read, whether their links came from the minion or from <c>WebSearch</c>, at
    /// most three, so how many links the minion gives is left to its own file.
    /// </summary>
    [Test]
    public void TheStopRule_CountsPrintsRead_WhereverTheirLinksCameFrom()
    {
        string cook = Cook();

        Assert.That(cook,
            Does.Contain("Read at most three prints, whether their links come from the minion or from `WebSearch`"));
    }

    /// <summary>
    /// The cook names no first site: the prompt said to start with one, the model obeyed, and 19 of 21 recipes came
    /// from it. A site's name here would bring that back.
    /// </summary>
    [Test]
    public void TheCook_NamesNoSiteToStartWith()
    {
        string cook = Cook();

        Assert.That(cook, Does.Not.Contain("foodandhome"));
        Assert.That(cook, Does.Not.Contain("`FindRecipes(source, query)`"));
        Assert.That(cook, Does.Contain("You name no site"));
    }

    /// <summary>
    /// In a real chat the cook dropped the minion's chicken bobotie because "the user asked for the classic", which they
    /// hadn't, and went to the web. A variant is the dish, and the minion's links come before the web.
    /// </summary>
    [Test]
    public void TheCook_TakesAVariant_AndUsesTheMinionsLinksBeforeTheWeb()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("A variant of the dish is the dish, unless the task asks for a particular kind"));
        Assert.That(cook, Does.Contain("Never `WebSearch` while a link is left"));
    }

    /// <summary>A web recipe says so under Unsure, in words the check reads, and a shop's site is never a guess.</summary>
    [Test]
    public void TheCook_SaysAWebRecipeUnderUnsure_AndNeverGuessesAShopsSite()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("from a web search").And.Contain("uit 'n websoektog"));
        Assert.That(cook, Does.Contain("never guess an address"));
    }

    /// <summary>The user is "you" to the cook, never someone it talks about: an answer said "die gebruiker".</summary>
    [Test]
    public void TheCook_TalksToTheUser_NeverAboutThem() =>
        Assert.That(Cook(), Does.Contain("Write to the user as *you* / *jy*, never *the user* / *die gebruiker*"));

    /// <summary>The image line and the link come from the tool's print, and the amounts from its lines.</summary>
    [Test]
    public void TheCook_TakesTheImageLine_AndTheLink_FromThePrint()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("the print's `![name](url)` line"));
        Assert.That(cook, Does.Contain("the print's `Link:` line"));
        Assert.That(cook, Does.Contain("ingredient lines"));
    }

    /// <summary>
    /// The block list of sites is gone: the browser refuses them in code (ADR 0068), and the prompt names none.
    /// </summary>
    [Test]
    public void TheCook_NamesNoSite_ThatRefusesReaders()
    {
        string cook = Cook();

        foreach (string domain in new[]
                 {
                     "huisgenoot.com", "sarie.com", "netwerk24.com", "news24.com", "food24.com",
                     "maroelamedia.co.za", "eatout.co.za", "halaal.recipes", "boerekos.co.za"
                 })
            Assert.That(cook, Does.Not.Contain(domain), domain);
        Assert.That(cook, Does.Not.Contain("Skip the results on these sites"));
    }

    /// <summary>
    /// A recipe page is read with <c>ReadRecipe</c>, never <c>FetchUrl</c> or <c>ReadRenderedPage</c>; the map is
    /// <c>FindBranches</c> now (F50.2), so the cook fetches nothing.
    /// </summary>
    [Test]
    public void TheCook_ReadsARecipePage_WithReadRecipe_AndFetchesNothing()
    {
        string cook = Cook();

        Assert.That(cook, Does.Not.Contain("ReadRenderedPage"));
        Assert.That(cook, Does.Not.Contain("`FetchUrl` a page").And.Not.Contain("`FetchUrl` for the recipe page"));
        Assert.That(cook, Does.Not.Contain("`FetchUrl`").And.Not.Contain("nominatim"), "the map is a tool now");
        Assert.That(cook, Does.Contain("`FindBranches` for the shop's branch"));
        Assert.That(cook, Does.Not.Contain("never a link you didn't fetch"));
    }

    /// <summary>
    /// A user who named no shop gets the shops near them; with no town to look up, the cook asks for it in the answer,
    /// and the line that sends them off to look for themselves is gone in both languages.
    /// </summary>
    [Test]
    public void TheCook_ListsTheShopsNearTheUser_OrInvitesTheirTown()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("`FindShopsNear` (`town`, the user's town)"));
        Assert.That(cook, Does.Contain("Gebruik my ligging"));
        Assert.That(cook, Does.Contain("*Usave*, never *Usave Malmesbury*"), "a branch's name is the map's own");
        Assert.That(cook, Does.Contain("| Pick n Pay | Pick n Pay | Voortrekker Street, Malmesbury |"),
            "the format's example doesn't teach a made-up name");
        Assert.That(cook, Does.Not.Contain("Any supermarket near you").And.Not.Contain("Enige supermark naby jou"));
    }

    /// <summary>
    /// A text print is page text with every image the page has, so the photo rules stay for it (Louis, 2026-11-04):
    /// no share button, no icon, no logo, nothing from another site, nothing built from a pattern.
    /// </summary>
    [Test]
    public void TheCook_KeepsThePhotoRules_ForATextPrint()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("take the image link itself, whole"));
        Assert.That(cook,
            Does.Contain("never one you build from a pattern").And.Contain("never one from another site"));
        Assert.That(cook, Does.Contain("never a share button, a print or PDF icon, a logo or an avatar"));
        Assert.That(cook, Does.Contain("no image, and nothing said about it"));
    }

    /// <summary>
    /// The one stop rule reads prints now: a photo only breaks a tie between prints that fit the budget.
    /// </summary>
    [Test]
    public void TheCook_KeepsTheTieRule_ForPrints()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("Stop at the first print that has the recipe, a photo of the dish"));
        Assert.That(cook, Does.Contain("Read at most three prints"));
        Assert.That(cook, Does.Contain("If none of the three fits the budget, use the cheapest of them"));
        Assert.That(cook, Does.Contain("A photo only breaks a tie between prints that fit"));
        Assert.That(cook, Does.Contain("a print over the budget is never used for its photo"));
        Assert.That(cook, Does.Contain("use the best print of them that fits and show no image"));
    }

    /// <summary>
    /// A page in the other language is translated step for step with every number kept (Louis, 2026-11-04), and
    /// nothing tells the cook to copy a page's words into the other language's answer.
    /// </summary>
    [Test]
    public void TheCook_TranslatesTheMethod_AndKeepsEveryNumber()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("the page's language differs from the task's"));
        Assert.That(cook, Does.Contain("write the steps in the task's language, step for step"));
        Assert.That(cook, Does.Contain("every number, time and temperature unchanged"));
        Assert.That(cook, Does.Not.Contain("quote the method"));
        Assert.That(cook, Does.Contain("Answer in the task's language"), "the existing rule stays");
    }

    /// <summary>
    /// The opening line names every toolset the cook gets: the header's (recipes, and F50.2's where to buy and shop
    /// prices) and the one Puls hands it.
    /// </summary>
    [Test]
    public void TheCooksOpeningLine_NamesEveryToolsetItGets()
    {
        var header = PromptCatalog.Get("specialists/puls-cook.md")!.Header!;

        Assert.That(Cook(), Does.Contain("(toolsets: `web`, `recipes`, `where-to-buy`, `shop-prices`, `calculator` and `minions`)"));
        Assert.That(header.Toolsets,
            Is.EqualTo(new[]
            {
                RecipesToolset.Recipes.Wire(), PulsToolset.WhereToBuy.Wire(), PulsToolset.ShopPrices.Wire(),
                PulsToolset.Calculator.Wire(), CoreToolset.Minions.Wire()
            }), "web comes from Puls, the rest from the header, all required");
        Assert.That(SkillLoader.ToolsetsOf(PulsRole.Cook.Wire(), RoleLevel.Specialist), Is.EqualTo(header.Toolsets));
    }
}
