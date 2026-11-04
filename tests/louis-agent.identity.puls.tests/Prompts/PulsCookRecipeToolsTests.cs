using LouisAgent.Choices;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Packs.Recipes;

namespace LouisAgent.Identities.Puls.Tests.Prompts;

/// <summary>
/// What the cook's prompt says about reading a recipe now that the recipes pack does it (F35.2 step 2): the order of
/// the work through <c>FindRecipes</c> and <c>ReadRecipe</c>, what the print gives that the cook only passes on, and
/// what is gone. A prompt that trusts code for facts and keeps judgment for itself (ADR 0067, GOAL skill 5).
/// </summary>
public class PulsCookRecipeToolsTests
{
    /// <summary>The cook's file as the model reads it, through the catalogue, on one line.</summary>
    private static string Cook() =>
        string.Join(' ', PromptCatalog.Get("specialists/puls-cook.md")!.Body.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The cook's role text names both tools, in the order of the work, starting on the data site.</summary>
    [Test]
    public void TheCook_NamesTheTwoRecipeTools_InTheOrderOfTheWork()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("`FindRecipes(source, query)`").And.Contain("`ReadRecipe`"));
        Assert.That(cook, Does.Contain("start with `foodandhome`"));
        Assert.That(cook, Does.Contain("at most three sites"));
        Assert.That(cook, Does.Contain("*publishes no recipe data*"), "a text print is read as the page's text, once");
        Assert.That(cook.IndexOf("`FindRecipes(source, query)`", StringComparison.Ordinal),
            Is.LessThan(cook.IndexOf("`ReadRecipe` the hit", StringComparison.Ordinal)));
    }

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
    /// A recipe page is read with <c>ReadRecipe</c>, never <c>FetchUrl</c> or <c>ReadRenderedPage</c>; the map is a
    /// tool now, so the cook fetches nothing.
    /// </summary>
    [Test]
    public void TheCook_ReadsARecipePage_WithReadRecipe_AndFetchesNothing()
    {
        string cook = Cook();

        Assert.That(cook, Does.Not.Contain("ReadRenderedPage"));
        Assert.That(cook, Does.Not.Contain("`FetchUrl` a page").And.Not.Contain("`FetchUrl` for the recipe page"));
        Assert.That(cook, Does.Not.Contain("`FetchUrl`").And.Not.Contain("nominatim"), "the map is a tool now");
        Assert.That(cook, Does.Contain("`FindShops` for the supermarkets near the user"));
        Assert.That(cook, Does.Not.Contain("never a link you didn't fetch"));
    }

    /// <summary>
    /// The list keeps only the nearest few, so in a city a chain the user named can fall outside it. Saying "no branch"
    /// then would be wrong, so the cook looks that one shop up, only then, and only once.
    /// </summary>
    [Test]
    public void TheCook_LooksUpANamedShop_OnlyWhenTheListLacksIt()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("**Not in the list:** look that shop up once with `FindBranches`"));
        Assert.That(cook, Does.Contain("`FindBranches` only for a shop the user named that `FindShops` didn't list"));
        Assert.That(cook, Does.Contain("A shop neither of them lists is never named"));
        Assert.That(cook, Does.Not.Contain("A shop it didn't list is never named"));
    }

    /// <summary>
    /// What the model reads says the rule, never who decided it or when: Puls's voice, the cook and the where-to-buy
    /// guide carry no "Louis" and no ISO date ("2026-11-04"). It does not check ticket or step notes (F50.2, step 3),
    /// a month named in an example ("August 2026"), or the tools' descriptions and message texts: none of those holds
    /// such a note today (searched by hand), but nothing here would catch one.
    /// </summary>
    [Test]
    public void WhatTheModelReads_NamesNoOneAndNoDate()
    {
        string[] texts =
        [
            string.Join(' ', PromptCatalog.Get("agent/puls/voice.md")!.Body.Split((char[]?)null)),
            Cook(),
            SkillFileSource.ReadEmbedded("toolsets/where-to-buy.md")!,
        ];

        foreach (string text in texts)
        {
            Assert.That(text, Does.Not.Contain("Louis"));
            Assert.That(text, Does.Not.Contain("2026-").And.Not.Contain("2025-"));
        }
    }

    /// <summary>
    /// The cook looks the town up once and lists every supermarket it gives, two of one chain as two rows (Louis,
    /// 2026-11-04: "it must show both then"); a shop the user named is picked out of the list.
    /// </summary>
    [Test]
    public void TheCook_ListsEveryShopTheTownHas_MergingNone_AndPicksANamedOneOut()
    {
        string cook = Cook();

        Assert.That(cook, Does.Contain("once, with `FindShops`").And.Contain("`town`, the user's town; no shop"));
        Assert.That(cook, Does.Contain("one row for each shop it lists"));
        Assert.That(cook, Does.Contain("two Pick n Pays are two rows, each with its own street"));
        Assert.That(cook, Does.Contain("pick that shop's rows out of the list"));
        Assert.That(cook, Does.Not.Contain("Any supermarket near you"), "the one line is gone: the shops are listed");
        Assert.That(cook, Does.Contain("found no supermarket"), "and what to say when the map has none");
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

        Assert.That(Cook(), Does.Contain("(toolsets: `web`, `recipes`, `where-to-buy` and `shop-prices`)"));
        Assert.That(header.Toolsets,
            Is.EqualTo(new[]
            {
                RecipesToolset.Recipes.Wire(), PulsToolset.WhereToBuy.Wire(), PulsToolset.ShopPrices.Wire()
            }), "web comes from Puls, the rest from the header");
        Assert.That(SkillLoader.ToolsetsOf(PulsRole.Cook.Wire(), RoleLevel.Specialist), Is.EqualTo(header.Toolsets));
    }
}
