using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Web.Browsing;
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

        string[] lines = (await Search(source).FindRecipes("bobotie", "foodandhome")).Split('\n');

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

        string result = await Search(source).FindRecipes("  bobotie ", "  FoodAndHome ");

        Assert.That(result, Does.Contain("1. Pork bobotie"));
        Assert.That(source.Query, Is.EqualTo("bobotie"));
    }

    /// <summary>A site that is not enabled says which are, so the model can pick one.</summary>
    [Test]
    public async Task AnUnknownSite_SaysWhichAreEnabled()
    {
        var tools = Tools(new FakeSiteBrowser(), new FakeRecipeSource("thelazymakoti"),
            new FakeRecipeSource("foodandhome"));

        string result = await tools.FindRecipes("bobotie", "nosuchsite");

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
        Assert.That(await Tools(new FakeSiteBrowser()).FindRecipes("bobotie", "foodandhome"),
            Does.Contain("Enabled: none."));

    /// <summary>An empty query is asked for again, and no site is searched.</summary>
    [TestCase("")]
    [TestCase("   ")]
    public async Task AnEmptyQuery_IsAskedForAgain(string query)
    {
        var source = new FakeRecipeSource("foodandhome", BobotieHits);

        string result = await Search(source).FindRecipes(query, "foodandhome");

        Assert.That(result, Is.EqualTo(Text(RecipeToolMessage.QueryEmpty)));
        Assert.That(source.Query, Is.Null);
    }

    /// <summary>The default is eight results, and more than ten is cut to ten; less than one gives one.</summary>
    [TestCase(null, 8)]
    [TestCase(3, 3)]
    [TestCase(10, 10)]
    [TestCase(50, 10)]
    [TestCase(0, 1)]
    public async Task TheNumberOfResults_IsEightByDefault_AndAtMostTen(int? asked, int expected)
    {
        var source = new FakeRecipeSource("foodandhome", BobotieHits);

        _ = asked is { } count
            ? await Search(source).FindRecipes("bobotie", "foodandhome", count)
            : await Search(source).FindRecipes("bobotie", "foodandhome");

        Assert.That(source.MaxResults, Is.EqualTo(expected));
    }

    /// <summary>A site that finds nothing says so, and what to try.</summary>
    [Test]
    public async Task ASearchWithNoHits_SaysSo() =>
        Assert.That(await Search(new FakeRecipeSource("foodandhome", [])).FindRecipes("zzz", "foodandhome"),
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

        Assert.That(await Search(source).FindRecipes("bobotie", "foodandhome"), Is.EqualTo("Food & Home said no."));
    }

    /// <summary>The tools over these sites, with a seeded draw so a test can repeat.</summary>
    private static RecipeTools Seeded(params FakeRecipeSource[] sources) =>
        new(sources.ToDictionary(source => source.Site.Slug, source => (IRecipeSource)source), new FakeSiteBrowser(),
            new Random(1));

    /// <summary>A site with one hit named after it.</summary>
    private static FakeRecipeSource WithHit(string slug) =>
        new(slug, [new RecipeHit($"{slug} bobotie", new Uri($"https://example.com/{slug}/bobotie"))]);

    /// <summary>
    /// With no site named it searches several, and each line says whose page it is, because the model is choosing
    /// between sites now: this is what stops every recipe coming from one.
    /// </summary>
    [Test]
    public async Task NoSiteNamed_ListsTheHitsOfSeveralSites_EachWithItsSite()
    {
        var tools = Seeded(WithHit("taste"), WithHit("foodandhome"));

        string[] lines = (await tools.FindRecipes("bobotie")).Split('\n');

        Assert.That(lines[0], Is.EqualTo(Text(RecipeToolMessage.UntrustedFrame)));
        Assert.That(lines[1], Does.Contain("Woolworths TASTE").And.Contain("Food & Home").And.Contain("(2)"));
        Assert.That(lines[2..4], Has.Some
            .EqualTo("1. taste bobotie · Woolworths TASTE · https://example.com/taste/bobotie")
            .Or.Some.EqualTo("2. taste bobotie · Woolworths TASTE · https://example.com/taste/bobotie"));
        Assert.That(lines[^1], Is.EqualTo(Text(RecipeToolMessage.FrameEnd)));
    }

    /// <summary>Spaces are no name: the model leaving it blank means the same as leaving it out.</summary>
    [TestCase("")]
    [TestCase("   ")]
    public async Task ABlankSite_IsNoSiteNamed(string source)
    {
        var site = WithHit("foodandhome");

        await Seeded(site).FindRecipes("  bobotie ", source);

        Assert.That(site.Query, Is.EqualTo("bobotie"));
    }

    /// <summary>A site searched through the web provider stays out of the draw, and can still be named.</summary>
    [Test]
    public async Task ASiteWithNoSearchOfItsOwn_IsOutOfTheDraw_ButCanBeNamed()
    {
        var webSearched = WithHit("saltyginger");
        var ownSearch = WithHit("foodandhome");
        var tools = Seeded(webSearched, ownSearch);

        await tools.FindRecipes("bobotie");
        Assert.That(webSearched.Query, Is.Null);
        Assert.That(ownSearch.Query, Is.EqualTo("bobotie"));

        string named = await tools.FindRecipes("bobotie", "saltyginger");
        Assert.That(named, Does.Contain("saltyginger bobotie"));
    }

    /// <summary>Nothing on the sites asked says which they were, and that another search asks others.</summary>
    [Test]
    public async Task NoHitsOnAnySite_NamesThem_AndSaysToSearchAgain()
    {
        string result = await Seeded(new FakeRecipeSource("foodandhome", [])).FindRecipes("zzz");

        Assert.That(result, Does.Contain("Food & Home").And.Contain("'zzz'").And.Contain("Search again"));
    }

    /// <summary>Every site refusing gives their reasons, never an exception.</summary>
    [Test]
    public async Task EverySiteRefusing_GivesTheirReasons()
    {
        var refusing = new FakeRecipeSource("foodandhome", fails: new RecipeSourceException("Food & Home said no."));

        string result = await Seeded(refusing).FindRecipes("bobotie");

        Assert.That(result, Does.Contain("Food & Home said no.").And.Contain("Search again"));
    }

    /// <summary>With some sites answering, a refusal doesn't reach the model.</summary>
    [Test]
    public async Task OneSiteRefusing_IsLeftOut_WhenAnotherAnswers()
    {
        var refusing = new FakeRecipeSource("taste", fails: new RecipeSourceException("TASTE said no."));

        string result = await Seeded(refusing, WithHit("foodandhome")).FindRecipes("bobotie");

        Assert.That(result, Does.Contain("foodandhome bobotie").And.Not.Contain("TASTE said no."));
    }

    /// <summary>With only web-searched sites on, there is no draw: it says so and which can be named.</summary>
    [Test]
    public async Task NoSiteInTheDraw_SaysWhichCanBeNamed()
    {
        string result = await Seeded(WithHit("saltyginger")).FindRecipes("bobotie");

        Assert.That(result, Does.Contain("saltyginger").And.Contain("source"));
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
