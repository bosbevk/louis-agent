using LouisAgent.Core.Tools.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Core.Tests.Tools.Shops.Providers;

// F34 step 1b (Louis: "make sure we set the address in checkersprovider, to make sure we use the correct catalogue").
// Checkers answers for a default store unless the visitor has chosen an area: its own address picker asks
// /api/store/fetch-store-contexts for the area's stores, and the reply sets the cookies every later search is answered
// from. CheckersStore does the same once per town, in a visit of its own, and keeps the cookies Checkers set. The
// answer is a real one for Malmesbury's centre, recorded 2026-10-17.
public class CheckersStoreTests
{
    private static readonly string Contexts = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.core.tests", "Tools", "Shops", "Providers", "Fixtures", "checkers-store-contexts.json"));

    private static readonly BrowserCookie[] SetByCheckers =
    [
        new("storeContexts", "%5B%7B%22storeId%22%3A%22655b0c8fb44cda100fd99383%22%7D%5D", "www.checkers.co.za"),
        new("storeids", "6a60644dcd48d5c3791143cf-655b0c8fb44cda100fd99383", "www.checkers.co.za"),
        new("istio-storeIds", "6a60644dcd48d5c3791143cf-655b0c8fb44cda100fd99383", "www.checkers.co.za"),
        new("aws-waf-token", "a-token", "www.checkers.co.za"),
        new("_ga", "GA1.1", ".checkers.co.za"),
    ];

    private sealed class FakeBrowser(string? answer = null, IReadOnlyList<BrowserCookie>? cookies = null, Exception? fails = null) : IWebBrowser
    {
        public List<(Uri Url, BrowserSession? Session, string? Script)> Visits { get; } = [];

        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default)
        {
            Visits.Add((url, session, script));
            if (fails is not null) throw fails;
            return Task.FromResult(new BrowserVisit(url.AbsoluteUri, "<html></html>", [], answer, cookies));
        }
    }

    private static Task<(double Latitude, double Longitude)?> Malmesbury(string place, CancellationToken _) =>
        Task.FromResult<(double, double)?>(place.Contains("malmesbury", StringComparison.OrdinalIgnoreCase) ? (-33.460278, 18.723611) : null);

    [Test]
    public async Task ATownsStore_IsTheCookiesCheckersSet_AskedOncePerTown()
    {
        var browser = new FakeBrowser(Contexts, SetByCheckers);
        var store = new CheckersStore(browser, Malmesbury);

        var first = await store.ForAsync("Malmesbury");
        var again = await store.ForAsync(" malmesbury ");

        Assert.That(first!.Cookies.Select(c => c.Name), Is.EquivalentTo(new[] { "storeContexts", "storeids", "istio-storeIds" }),
            "the store's cookies, Checkers' own, never its bot token or analytics");
        Assert.That(again, Is.SameAs(first));
        Assert.That(browser.Visits, Has.Count.EqualTo(1), "one lookup per town");
    }

    [Test]
    public async Task TheLookup_IsTheSitesOwnCall_WithTheTownsCentre_AndNoUserText()
    {
        var browser = new FakeBrowser(Contexts, SetByCheckers);

        await new CheckersStore(browser, Malmesbury).ForAsync("Malmesbury, Wes-Kaap");

        var (url, session, script) = browser.Visits.Single();
        Assert.That(url.AbsoluteUri, Is.EqualTo("https://www.checkers.co.za/"));
        Assert.That(session, Is.Not.Null.And.Property("Cookies").Empty, "a context of its own, starting empty");
        Assert.That(script, Does.Contain("/api/store/fetch-store-contexts?update=true").And.Contain("-33.460278").And.Contain("18.723611"));
        Assert.That(script, Does.Not.Contain("Malmesbury").And.Not.Contain("Wes-Kaap"), "only numbers of ours go into the script");
    }

    [Test]
    public async Task NoPlace_IsTheDefaultStore_WithoutAVisit()
    {
        var browser = new FakeBrowser(Contexts, SetByCheckers);

        Assert.That(await new CheckersStore(browser, Malmesbury).ForAsync(null), Is.Null);
        Assert.That(await new CheckersStore(browser, Malmesbury).ForAsync("  "), Is.Null);
        Assert.That(browser.Visits, Is.Empty);
    }

    [Test]
    public async Task APlaceOpenStreetMapDoesntKnow_IsTheDefaultStore()
    {
        var browser = new FakeBrowser(Contexts, SetByCheckers);

        Assert.That(await new CheckersStore(browser, Malmesbury).ForAsync("Atlantis"), Is.Null);
        Assert.That(browser.Visits, Is.Empty);
    }

    [TestCase("{\"storeContexts\":[],\"servicedByOtherBrand\":null}")]
    [TestCase("not json")]
    [TestCase(null)]
    public async Task NoStoreServingThePlace_IsTheDefaultStore(string? answer) =>
        Assert.That(await new CheckersStore(new FakeBrowser(answer, SetByCheckers), Malmesbury).ForAsync("Malmesbury"), Is.Null);

    [Test]
    public async Task ABrowserError_IsTheDefaultStore_NotAFailedSearch() =>
        Assert.That(await new CheckersStore(new FakeBrowser(fails: new WebBrowserException("didn't answer")), Malmesbury).ForAsync("Malmesbury"), Is.Null);

    [Test, Category("Live")]
    public async Task Malmesbury_IsServedByAStoreOtherThanTheDefault()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to ask Checkers with (the Puls stack's puls-browser).");

        var session = await new CheckersStore(new ChromeWebBrowser(new Uri(url!)), LouisAgent.Core.Tools.Shops.PlaceCoordinates.OfAsync).ForAsync("Malmesbury");

        Assert.That(session, Is.Not.Null);
        Assert.That(session!.Cookies.Single(c => c.Name == "storeids").Value, Does.Not.StartWith("5ece6935faafe599532665b2"), "the default store's id");
    }
}
