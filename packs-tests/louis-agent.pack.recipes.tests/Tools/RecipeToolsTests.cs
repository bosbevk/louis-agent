using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Tests.Tools;

/// <summary>
/// Checks the two tools the model calls: the search of an enabled site and the reading of a recipe page. They are
/// tested with a fake site and a fake browser that answers with saved pages, so what a test expects is what the page
/// says.
/// </summary>
public class RecipeToolsTests
{
    private static readonly Uri BobotiePage = new("https://www.foodandhome.co.za/recipes/pork-bobotie");

    private static readonly RecipeHit[] BobotieHits =
    [
        new("Pork bobotie", BobotiePage),
        new("Braaied bobotie-spiced chicken", new Uri("https://www.foodandhome.co.za/recipes/braaied-bobotie"))
    ];

    /// <summary>The tools over the sites given and a browser that answers every address with one page.</summary>
    private static RecipeTools Tools(FakeSiteBrowser browser, params FakeRecipeSource[] sources) =>
        new(sources.ToDictionary(source => source.Site.Slug, source => (IRecipeSource)source), browser);

    /// <summary>The tools over the Food &amp; Home site, which answers with two hits.</summary>
    private static RecipeTools Search(FakeRecipeSource source) => Tools(new FakeSiteBrowser(), source);

    /// <summary>The tools over a browser that answers with this saved recipe page.</summary>
    private static RecipeTools Reader(string fixture) => Tools(new FakeSiteBrowser(Page(fixture)));

    /// <summary>One saved recipe page, as the browser would hand over its HTML.</summary>
    private static string Page(string name) => File.ReadAllText(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Recipes", "Fixtures", name + ".html"));

    /// <summary>The text the model reads for a message.</summary>
    private static string Text(RecipeToolMessage message, object? values = null) =>
        MessageText.Format(message, values);

    /// <summary>
    /// A search answers with the frame, a heading that names the site and the count, then the hits numbered with
    /// their address.
    /// </summary>
    [Test]
    public async Task FindRecipes_GivesTheFrame_TheHeading_AndTheNumberedHits()
    {
        var source = new FakeRecipeSource("foodandhome", BobotieHits);

        string[] lines = (await Search(source).FindRecipes("foodandhome", "bobotie")).Split('\n');

        Assert.That(lines[0], Is.EqualTo(Text(RecipeToolMessage.UntrustedFrame)));
        Assert.That(lines[1], Is.EqualTo(Text(RecipeToolMessage.HitsFound, new
        {
            Source = "Food & Home",
            Count = 2
        })));
        Assert.That(lines[2],
            Is.EqualTo("1. Pork bobotie · https://www.foodandhome.co.za/recipes/pork-bobotie"));
        Assert.That(lines[3],
            Is.EqualTo("2. Braaied bobotie-spiced chicken · https://www.foodandhome.co.za/recipes/braaied-bobotie"));
        Assert.That(lines[4], Is.EqualTo(Text(RecipeToolMessage.FrameEnd)));
        Assert.That(source.Query, Is.EqualTo("bobotie"));
    }

    /// <summary>The site's name may be typed in any case, with spaces around it, and the query is trimmed.</summary>
    [Test]
    public async Task FindRecipes_TakesTheSiteInAnyCase_AndTrimsTheQuery()
    {
        var source = new FakeRecipeSource("foodandhome", BobotieHits);

        string result = await Search(source).FindRecipes("  FoodAndHome ", "  bobotie ");

        Assert.That(result, Does.Contain("1. Pork bobotie"));
        Assert.That(source.Query, Is.EqualTo("bobotie"));
    }

    /// <summary>A site that is not enabled says which are, so the model can pick one.</summary>
    [Test]
    public async Task AnUnknownSite_SaysWhichAreEnabled()
    {
        var tools = Tools(new FakeSiteBrowser(), new FakeRecipeSource("thelazymakoti"),
            new FakeRecipeSource("foodandhome"));

        string result = await tools.FindRecipes("nosuchsite", "bobotie");

        Assert.That(result, Is.EqualTo(Text(RecipeToolMessage.SourceNotEnabled,
            new
            {
                Source = "nosuchsite",
                Enabled = "foodandhome, thelazymakoti"
            })));
    }

    /// <summary>With no site on at all, it says none.</summary>
    [Test]
    public async Task WithNoSiteEnabled_ItSaysNone() =>
        Assert.That(await Tools(new FakeSiteBrowser()).FindRecipes("foodandhome", "bobotie"),
            Does.Contain("Enabled: none."));

    /// <summary>An empty query is asked for again, and no site is searched.</summary>
    [TestCase("")]
    [TestCase("   ")]
    public async Task AnEmptyQuery_IsAskedForAgain(string query)
    {
        var source = new FakeRecipeSource("foodandhome", BobotieHits);

        string result = await Search(source).FindRecipes("foodandhome", query);

        Assert.That(result, Is.EqualTo(Text(RecipeToolMessage.QueryEmpty)));
        Assert.That(source.Query, Is.Null);
    }

    /// <summary>The default is five results, and more than ten is cut to ten; less than one gives one.</summary>
    [TestCase(null, 5)]
    [TestCase(3, 3)]
    [TestCase(10, 10)]
    [TestCase(50, 10)]
    [TestCase(0, 1)]
    public async Task TheNumberOfResults_IsFiveByDefault_AndAtMostTen(int? asked, int expected)
    {
        var source = new FakeRecipeSource("foodandhome", BobotieHits);

        _ = asked is { } count
            ? await Search(source).FindRecipes("foodandhome", "bobotie", count)
            : await Search(source).FindRecipes("foodandhome", "bobotie");

        Assert.That(source.MaxResults, Is.EqualTo(expected));
    }

    /// <summary>A site that finds nothing says so, and what to try.</summary>
    [Test]
    public async Task ASearchWithNoHits_SaysSo() =>
        Assert.That(await Search(new FakeRecipeSource("foodandhome", [])).FindRecipes("foodandhome", "zzz"),
            Is.EqualTo(Text(RecipeToolMessage.SourceNoResults, new
            {
                Source = "Food & Home",
                Query = "zzz"
            })));

    /// <summary>A site's own failure comes back as its text, never thrown (the pack rule).</summary>
    [Test]
    public async Task ASourceThatFails_ComesBackAsItsText()
    {
        var source = new FakeRecipeSource("foodandhome", fails: new RecipeSourceException("Food & Home said no."));

        Assert.That(await Search(source).FindRecipes("foodandhome", "bobotie"), Is.EqualTo("Food & Home said no."));
    }

    /// <summary>
    /// A recipe page is printed from its own data: the frame, then the name, the line of how much and how long, the
    /// numbered lines, the photo, the author and where the page is.
    /// </summary>
    [Test]
    public async Task ReadRecipe_PrintsTheRecipeFromThePagesData()
    {
        string result = await Reader("foodandhome-pork-bobotie").ReadRecipe(BobotiePage.AbsoluteUri);

        string[] lines = result.Split('\n');
        Assert.That(lines[0], Is.EqualTo(Text(RecipeToolMessage.UntrustedFrame)));
        Assert.That(lines[1], Is.EqualTo("# Pork bobotie"));
        Assert.That(result, Does.Contain("Makes 4 to 6 · prep 20 min · cook 30 min\n"));
        Assert.That(result, Does.Not.Contain("· total"), "the page's total is empty, so the line leaves it out");
        Assert.That(result, Does.Contain("## Ingredients\n### Filling\n1. 500 g Pork mince"));
        Assert.That(result, Does.Contain("### Topping\n15. 2 Eggs").And.Not.Contain("***"));
        Assert.That(result, Does.Contain("## Method\n1. Preheat oven to 170"));
        Assert.That(result, Does.Contain("![Pork bobotie](https://www.foodandhome.co.za/wp-content/uploads/2026/09/"));
        Assert.That(result, Does.Contain("Author: SA Pork"));
        Assert.That(lines[^2], Is.EqualTo($"Link: {BobotiePage}"));
        Assert.That(lines[^1], Is.EqualTo(Text(RecipeToolMessage.FrameEnd)));
    }

    /// <summary>The link is where the browser ended after any redirect, not the address it was asked for.</summary>
    [Test]
    public async Task ReadRecipe_LinksToWhereTheBrowserEnded()
    {
        var browser = new RedirectingBrowser(Page("foodandhome-pork-bobotie"),
            "https://www.foodandhome.co.za/recipes/pork-bobotie-final");

        string result = await new RecipeTools(new Dictionary<string, IRecipeSource>(), browser)
            .ReadRecipe("https://foodandhome.co.za/old");

        Assert.That(result, Does.Contain("Link: https://www.foodandhome.co.za/recipes/pork-bobotie-final"));
    }

    /// <summary>
    /// A page with no recipe data comes back as its text, labelled with the site, framed as data, and with the link.
    /// </summary>
    [Test]
    public async Task APageWithNoRecipeData_ComesBackAsItsText_Labelled()
    {
        const string address = "https://thelazymakoti.com/chakalaka/";

        string[] lines = (await Reader("thelazymakoti-chakalaka").ReadRecipe(address)).Split('\n');

        Assert.That(lines[0], Is.EqualTo(Text(RecipeToolMessage.UntrustedFrame)));
        Assert.That(lines[1], Is.EqualTo("thelazymakoti.com publishes no recipe data; this is the page's text."));
        Assert.That(string.Join('\n', lines), Does.Contain("Ingredients:").And.Contain("- 1 onion, finely chopped"));
        Assert.That(lines[^2], Is.EqualTo($"Link: {address}"));
        Assert.That(lines[^1], Is.EqualTo(Text(RecipeToolMessage.FrameEnd)));
    }

    /// <summary>A long page's text is cut at 8,000 characters, and the answer says it was cut.</summary>
    [Test]
    public async Task APagesTextOverTheBound_IsCut_AndSaysSo()
    {
        string html = $"<html><body><main><p>{new string('a', 20_000)}</p></main></body></html>";
        var tools = Tools(new FakeSiteBrowser(html));

        string result = await tools.ReadRecipe("https://example.co.za/long");

        Assert.That(result, Does.Contain(new string('a', 8_000) + "\n"));
        Assert.That(result, Does.Not.Contain(new string('a', 8_001)));
        Assert.That(result, Does.Contain(Text(RecipeToolMessage.PageCut, new { Shown = 8_000 })));
    }

    /// <summary>A page of no readable text says so rather than showing an empty frame.</summary>
    [Test]
    public async Task APageWithNoReadableText_SaysSo() =>
        Assert.That(await Tools(new FakeSiteBrowser("<html><body></body></html>")).ReadRecipe("https://example.co.za/"),
            Does.Contain(Text(RecipeToolMessage.PageEmpty)));

    /// <summary>
    /// Something that is not an address, or not http or https, is refused before the browser is asked for anything.
    /// </summary>
    [TestCase("not an address")]
    [TestCase("")]
    [TestCase("ftp://example.co.za/recipe")]
    [TestCase("file:///C:/secrets.txt")]
    [TestCase("javascript:alert(1)")]
    public async Task AnInvalidAddress_IsRefused_BeforeAnyVisit(string url)
    {
        var browser = new FakeSiteBrowser("<html></html>");

        string result = await Tools(browser).ReadRecipe(url);

        Assert.That(result, Is.EqualTo(Text(RecipeToolMessage.AddressRefused)));
        Assert.That(browser.Requested, Is.Empty);
    }

    /// <summary>
    /// A page the browser refuses (a private address, or one a site's <c>robots.txt</c> disallows) comes back as the
    /// browser's own text, never thrown.
    /// </summary>
    [Test]
    public async Task APageTheBrowserRefuses_ComesBackAsItsText()
    {
        var browser = new FakeSiteBrowser(fails: new WebBrowserException("robots.txt disallows that page."));

        string result = await Tools(browser).ReadRecipe("https://www.foodandhome.co.za/private");

        Assert.That(result, Is.EqualTo(Text(RecipeToolMessage.PageRefused,
            new
            {
                Url = "https://www.foodandhome.co.za/private",
                Reason = "robots.txt disallows that page."
            })));
    }

    /// <summary>A browser that answers a different address from the one asked, as after a redirect.</summary>
    private sealed class RedirectingBrowser(string html, string endsAt) : IWebBrowser
    {
        /// <summary>Opens the page and says it ended at the other address.</summary>
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
            string? script = null, string? readySelector = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserVisit(endsAt, html, []));
    }
}
