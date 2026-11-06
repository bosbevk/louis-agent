using System.Text.RegularExpressions;
using LouisAgent.Core.Skills;

namespace LouisAgent.Identities.Puls.Tests;

/// <summary>
/// The router and the cook say the same about prices (F18 step 5.7.2, Louis, 2026-10-25: "update the router voice.md
/// too all prompts to work with the new functionality"): the cook looks up no prices and names the shop's branch.
/// </summary>
public class PulsEstimatesPromptsTests
{
    /// <summary>One built-in prompt's text, header and all.</summary>
    private static string Read(string path) =>
        SkillFileSource.ReadEmbedded(path) ?? throw new FileNotFoundException(path);

    /// <summary>A prompt with its line breaks and indents as single spaces, for a phrase the file wraps.</summary>
    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

    /// <summary>Puls promises estimates and where to buy, never today's prices.</summary>
    [Test]
    public void TheRouter_PromisesEstimatesAndWhereToBuy_NotTodaysPrices()
    {
        string router = Flat(Read("agent/puls/voice.md"));

        Assert.That(router,
            Does.Contain("without its estimated costs and where to buy near the user is half an answer"));
        Assert.That(router, Does.Not.Contain("today's prices"));
        Assert.That(router, Does.Not.Contain("It has the pages and prices you never saw"));
    }

    /// <summary>Puls's brief to the cook asks for no product photos; the recipe image is the cook's own rule.</summary>
    [Test]
    public void TheBrief_AsksForNoProductPhotos()
    {
        // The cook shows no product photos ("no brand, no photo"); its one recipe image is its own rule, the brief
        // doesn't ask for it.
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Not.Contain("sy foto").And.Not.Contain("its photo").And.Not.Contain("with its photo"));
        Assert.That(router, Does.Not.Contain("elke produk"));
    }

    /// <summary>The shop line in the brief makes the cook name the branch and estimate the prices.</summary>
    [Test]
    public void TheShopLine_NamesTheBranch_AndEstimatesThePrices()
    {
        string router = Flat(Read("agent/puls/voice.md"));

        Assert.That(router, Does.Not.Contain("The cook prices at that shop alone"));
        Assert.That(router, Does.Contain("names that shop's branch under *Where to buy* and estimates the prices"));
        Assert.That(router, Does.Not.Contain("Waar gaan jy inkopies doen"),
            "never asked: the prices are the same estimates wherever they shop");
        Assert.That(router, Does.Contain("brief the cook with *any* and never ask"));
        Assert.That(router, Does.Contain("A follow-up keeps the shop until the user changes it"));
    }

    /// <summary>The routing rows for cooking and shopping estimate the costs and compare no shop's prices.</summary>
    [Test]
    public void TheRolesRows_EstimateTheCosts_AndNameTheBranch_ComparingNoShopsPrices()
    {
        string[] rows = Read("agent/puls/voice.md").Split('\n')
            .Where(row => row.StartsWith("| `web`, for every") && row.Contains("`puls-cook`")).ToArray();

        Assert.That(rows, Has.Length.EqualTo(2), "cooking and shopping");
        Assert.That(rows, Is.All.Contain("estimates the costs and names the branch of the shop the user will buy at"));
        Assert.That(rows, Is.All.Contain("put their town and their shop in the task"));
        Assert.That(rows,
            Is.All.Not.Contain("prices the ingredients").And.Not.Contain("prices/compares").And.Not
                .Contain("prices/comparison"));
        Assert.That(rows.Single(row => row.Contains("shopping/grocery")),
            Does.Contain("no comparison of shops' prices"));
    }

    /// <summary>A user who asks for a real price is told, in either language, that none is looked up for now.</summary>
    [Test]
    public void AUserAskingForARealPrice_IsToldPulsDoesNotLookShopsPricesUpForNow()
    {
        string router = Flat(Read("agent/puls/voice.md"));

        Assert.That(Flat(Read("agent/puls/voice-af.md")),
            Does.Contain(
                "Ek soek vir eers nie winkels se pryse op nie (ek het hulle om toestemming gevra); " +
                "die koste is skattings."));
        Assert.That(Flat(Read("agent/puls/voice-en.md")),
            Does.Contain(
                "I don't look up shops' prices for now (I asked them for permission); the costs are estimates."));
        Assert.That(router, Does.Contain("Never offer another shop's prices instead"));
    }
}
