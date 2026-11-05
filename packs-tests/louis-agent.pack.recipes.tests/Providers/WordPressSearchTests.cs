using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Web.Browsing;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Providers;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Tests.Providers;

/// <summary>
/// Checks that a WordPress site's own search page is turned into hits, on pages saved from real sites, and that a
/// refusal from the browser reaches the cook as a message it can read.
/// </summary>
/// <remarks>
/// The browser is a fake that answers with a saved page, so what a test expects is what the page says.
/// </remarks>
public class WordPressSearchTests
{
    /// <summary>The site with this slug, from the table.</summary>
    private static RecipeSite Site(string slug) => RecipeSites.Find(slug)!;

    /// <summary>Searches the site with a browser that answers with the named saved search page.</summary>
    private static async Task<IReadOnlyList<RecipeHit>> Find(string slug, string fixture, string query = "bobotie",
        int max = 50) =>
        await new WordPressSearch(Site(slug), new FakeSiteBrowser(FakeSiteBrowser.Fixture(fixture)))
            .FindAsync(query, max);

    /// <summary>
    /// Food &amp; Home's page gives every heading that links to the site, in page order, and skips the rest.
    /// </summary>
    [Test]
    public async Task FoodAndHome_GivesTheHeadingsThatLinkToItself_InPageOrder()
    {
        var hits = await Find("foodandhome", "foodandhome-search-bobotie");

        Assert.That(hits, Has.Count.EqualTo(15), "the page's first heading, 'bobotie', links nowhere");
        Assert.That(hits[0], Is.EqualTo(new RecipeHit("Pork bobotie",
            new Uri("https://www.foodandhome.co.za/recipes/pork-bobotie"))));
        Assert.That(hits[1].Name, Is.EqualTo("Braaied bobotie-spiced chicken with rice noodles"));
    }

    /// <summary>
    /// A title the page wrote with entities, like a curly apostrophe or an ampersand, reads as plain text.
    /// </summary>
    [Test]
    public async Task ATitle_HasItsEntitiesDecoded()
    {
        var names = (await Find("foodandhome", "foodandhome-search-bobotie")).Select(h => h.Name).ToList();

        Assert.That(names, Does.Contain("[CLOSED] WIN Warren Mendes’ Food Trail South Africa cookbook"));
        Assert.That(names, Does.Contain("Quick & Easy Ramadan recipes"));
    }

    /// <summary>
    /// Tantalise's page gives its nine results, though each link sits on its own line inside the heading.
    /// </summary>
    [Test]
    public async Task Tantalise_GivesItsNineResults_WithTheLinkAcrossTheWhitespaceInTheHeading()
    {
        var hits = await Find("tantalisemytastebuds", "tantalisemytastebuds-search-bobotie");

        Assert.That(hits, Has.Count.EqualTo(9));
        Assert.That(hits[1], Is.EqualTo(new RecipeHit("Cape Malay Pickled Fish (Ingelegde vis)",
            new Uri("https://www.tantalisemytastebuds.com/cape-malay-pickled-fish/"))));
        Assert.That(hits[4].Name, Is.EqualTo("The way we were – Traditions"));
    }

    /// <summary>You never get more hits than you asked for.</summary>
    [Test]
    public async Task TheList_IsCutAtMaxResults() =>
        Assert.That((await Find("foodandhome", "foodandhome-search-bobotie", max: 3)).Select(h => h.Name),
            Is.EqualTo(new[]
            {
                "Pork bobotie", "Braaied bobotie-spiced chicken with rice noodles", "Bobotie Pancake Cups"
            }));

    /// <summary>The Lazy Makoti does not jump to the post: a search with one match is a list with one entry.</summary>
    [Test]
    public async Task TheLazyMakotisChakalakaSearch_IsAResultsListWithOneEntry()
    {
        var hits = await Find("thelazymakoti", "thelazymakoti-search-chakalaka", "chakalaka");

        Assert.That(hits.Single(),
            Is.EqualTo(new RecipeHit("Chakalaka", new Uri("https://thelazymakoti.com/chakalaka/"))));
    }

    /// <summary>The soup search is not an empty page: it holds one result, a chicken and rice soup.</summary>
    [Test]
    public async Task TheLazyMakotisSoupSearch_IsNotEmpty_ItHoldsOneResult() =>
        Assert.That((await Find("thelazymakoti", "thelazymakoti-search-soup", "soup")).Single().Name,
            Is.EqualTo("Chicken and Rice Soup"));

    /// <summary>A page that says nothing was found gives an empty list, not an error.</summary>
    [Test]
    public async Task APageWithNoResultHeading_GivesAnEmptyList_NoException()
    {
        const string nothing = "<html><body><h1>Nothing found</h1><p>Sorry, nothing matched.</p></body></html>";

        var hits = await new WordPressSearch(Site("thelazymakoti"), new FakeSiteBrowser(nothing)).FindAsync("xyz", 5);

        Assert.That(hits, Is.Empty);
    }

    /// <summary>When the browser refuses a page, the cook gets a message that names the site and the reason.</summary>
    [Test]
    public void AnAnswerTheBrowserRefused_BecomesAMessageNamingTheSiteAndTheReason()
    {
        var site = Site("thelazymakoti");
        var refused = new FakeSiteBrowser(fails: new WebBrowserException("robots.txt says no"));

        var thrown = Assert.ThrowsAsync<RecipeSourceException>(() =>
            new WordPressSearch(site, refused).FindAsync("chakalaka", 5));

        Assert.That(thrown!.Message, Is.EqualTo(MessageText.Format(RecipeToolMessage.SourceRefused,
            new
            {
                Source = site.Name,
                Query = "chakalaka",
                Reason = "robots.txt says no"
            })));
    }

    /// <summary>Cancelling a search is not a refusal, so the cancel goes on up.</summary>
    [Test]
    public void ACancel_PassesThrough()
    {
        var cancelled = new FakeSiteBrowser(fails: new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            new WordPressSearch(Site("thelazymakoti"), cancelled).FindAsync("chakalaka", 5));
    }

    /// <summary>The words are made safe for a web address, so a space or an ampersand can't break it.</summary>
    [Test]
    public async Task TheQuery_IsUrlEncodedIntoTheSitesSearchAddress()
    {
        var browser = new FakeSiteBrowser();

        await new WordPressSearch(Site("thelazymakoti"), browser).FindAsync("hoender kerrie & rys", 5);

        Assert.That(browser.Requested.Single().AbsoluteUri,
            Is.EqualTo("https://thelazymakoti.com/?s=hoender+kerrie+%26+rys"));
    }

    /// <summary>A site that lives under a path, like Rooi Rose, is searched under that path.</summary>
    [Test]
    public async Task ASiteWithAPathInItsHome_IsSearchedBeneathIt()
    {
        var browser = new FakeSiteBrowser();

        await new WordPressSearch(new RecipeSite("x", "X", new Uri("https://novanews.co.za/rooirose"),
            RecipeReadBy.Text, RecipeSearch.WordPress, new DateOnly(2026, 11, 4), null), browser).FindAsync("a", 5);

        Assert.That(browser.Requested.Single().AbsoluteUri, Is.EqualTo("https://novanews.co.za/rooirose/?s=a"));
    }
}
