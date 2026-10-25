using System.Text.RegularExpressions;
using LouisAgent.Core.Skills;

namespace LouisAgent.Identities.Puls.Tests;

// F18 step 5.7.2 (Louis, 2026-10-25: "update the router voice.md too all prompts to work with the new functionality"): step 5.7 made the cook look up no prices
// and name the shop's branch; the router and the cook's reading rule say the same. Written first: the router still promised prices and photos.
public class PulsEstimatesPromptsTests
{
    private static string Read(string path) => SkillFileSource.ReadEmbedded(path) ?? throw new FileNotFoundException(path);

    /// <summary>A prompt with its line breaks and indents as single spaces, for a phrase the file wraps.</summary>
    private static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

    [Test]
    public void TheRouter_PromisesEstimatesAndWhereToBuy_NotTodaysPrices()
    {
        string router = Flat(Read("agent/puls/voice.md"));

        Assert.That(router, Does.Contain("without its estimated costs and where to buy near the user is half an answer"));
        Assert.That(router, Does.Not.Contain("today's prices"));
        Assert.That(router, Does.Not.Contain("It has the pages and prices you never saw"));
    }

    [Test]
    public void TheBrief_AsksForNoProductPhotos()
    {
        // The cook shows no product photos ("no brand, no photo"); its one recipe image is its own rule, the brief doesn't ask for it.
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Not.Contain("sy foto").And.Not.Contain("its photo").And.Not.Contain("with its photo"));
        Assert.That(router, Does.Not.Contain("elke produk"));
    }

    [Test]
    public void TheShopLine_NamesTheBranch_AndEstimatesThePrices()
    {
        string router = Flat(Read("agent/puls/voice.md"));

        Assert.That(router, Does.Not.Contain("The cook prices at that shop alone"));
        Assert.That(router, Does.Contain("names that shop's branch under *Where to buy* and estimates the prices"));
        Assert.That(router, Does.Contain("Waar gaan jy inkopies doen"), "still asked when missing: the branch needs the shop");
        Assert.That(router, Does.Contain("A follow-up keeps the shop until the user changes it"));
    }

    [Test]
    public void TheRolesRows_EstimateTheCosts_AndNameTheBranch_ComparingNoShopsPrices()
    {
        string[] rows = Read("agent/puls/voice.md").Split('\n').Where(row => row.StartsWith("| `web`, for every") && row.Contains("`puls-cook`")).ToArray();

        Assert.That(rows, Has.Length.EqualTo(2), "cooking and shopping");
        Assert.That(rows, Is.All.Contain("estimates the costs and names the branch of the shop the user will buy at"));
        Assert.That(rows, Is.All.Contain("put their town and their shop in the task"));
        Assert.That(rows, Is.All.Not.Contain("prices the ingredients").And.Not.Contain("prices/compares").And.Not.Contain("prices/comparison"));
        Assert.That(rows.Single(row => row.Contains("shopping/grocery")), Does.Contain("no comparison of shops' prices"));
    }

    [Test]
    public void AUserAskingForARealPrice_IsToldPulsDoesNotLookShopsPricesUpForNow()
    {
        string router = Flat(Read("agent/puls/voice.md"));

        Assert.That(router, Does.Contain("Ek soek vir eers nie winkels se pryse op nie (ek het hulle om toestemming gevra); die koste is skattings."));
        Assert.That(router, Does.Contain("I don't look up shops' prices for now (I asked them for permission); the costs are estimates."));
        Assert.That(router, Does.Contain("Never offer another shop's prices instead"));
    }

    [Test]
    public void TheCook_SkipsTheSitesThatRefuseAiReaders_ADecisionOfADR0038()
    {
        // 5.7.0 found these disallow Claude by name in robots.txt (Huisgenoot and SARIE are netwerk24.com, Food24 is News24), or answer with a captcha
        // (Boerekos) or a 429 (News24): a site that refuses automated readers refuses this one. The check in code is Sources as data's RobotsPolicy.
        string cook = Flat(Read("specialists/puls-cook.md"));

        foreach (string domain in new[] { "huisgenoot.com", "sarie.com", "netwerk24.com", "news24.com", "food24.com", "maroelamedia.co.za", "eatout.co.za", "halaal.recipes", "boerekos.co.za" })
            Assert.That(cook, Does.Contain($"`{domain}`"), domain);
        Assert.That(cook, Does.Contain("skip them for the next search result"));
        Assert.That(cook, Does.Contain("a page that answers with a captcha or a block is dropped the same way"));
    }
}
