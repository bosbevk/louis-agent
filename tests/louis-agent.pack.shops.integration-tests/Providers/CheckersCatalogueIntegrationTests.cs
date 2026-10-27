using LouisAgent.Testing;
using LouisAgent.Packs.Shops.Catalogues;
using LouisAgent.Packs.Shops.Places;
using LouisAgent.Packs.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Packs.Shops.IntegrationTests.Providers;

// F33 step 2: Checkers' products read from its own catalogue. Its search page (/search?Search=…, the address its own
// search box sends; /search?q= finds nothing) fetches /api/catalogue/get-products-filter, and that answer has each
// product's name, price, unit and article number; the page's cards link each product as /product/{name}-{article}{unit}.
// The fixtures are a real answer and page, recorded 2026-10-17 and trimmed, so a renamed field fails here first.
[Category("Integration")]
public class CheckersCatalogueIntegrationTests
{
    // F34 step 1b: the search runs in the store that serves the user's town, when they shared it; the answer says which.
    private static readonly BrowserSession MalmesburysStore = new([new BrowserCookie("storeids", "655b0c8fb44cda100fd99383", "www.checkers.co.za")]);

    private sealed class OneTownsStore : IWebBrowser
    {
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new BrowserVisit(url.AbsoluteUri, "", [], """{"storeContexts":[{"storeId":"655b0c8fb44cda100fd99383"}]}""", MalmesburysStore.Cookies));
    }

    private static CheckersStore Store() =>
        new(new OneTownsStore(), (place, _) => Task.FromResult<(double, double)?>(place == "Malmesbury" ? (-33.46, 18.72) : null));

    [Test, Category("Live")]
    public async Task FromMalmesbury_TheSearchIsMalmesburysStore()
    {
        string url = Outside.Stack("BROWSER_URL", "browser to search Checkers with");
        var browser = new ChromeWebBrowser(new Uri(url!));

        var found = await new CheckersCatalogue(browser, new CheckersStore(browser, LouisAgent.Packs.Shops.Places.PlaceCoordinates.OfAsync)).SearchAsync("chicken", 5, "Malmesbury");

        Assert.That(found.Area, Is.EqualTo("Malmesbury"));
        Assert.That(found.Store, Does.Not.StartWith("5ece6935"), "the default store's id: the town's store answered");
        Assert.That(found.Products, Is.Not.Empty);
    }

    [Test, Category("Live")]
    public async Task Checkers_ShowsItsDeals_WithTheirWording()
    {
        string url = Outside.Stack("BROWSER_URL", "browser to search Checkers with");

        var products = (await new CheckersCatalogue(new ChromeWebBrowser(new Uri(url!))).SearchAsync("chicken", 16)).Products;

        Assert.That(products, Has.Some.Matches<ShopProduct>(p => p.Deal is { Text.Length: > 0 }), "deals come and go; chicken has had one all week");
    }

    [Test, Category("Live")]
    public async Task Checkers_FindsItsProducts_WithPricesAndLinks()
    {
        string url = Outside.Stack("BROWSER_URL", "browser to search Checkers with");

        var products = (await new CheckersCatalogue(new ChromeWebBrowser(new Uri(url!))).SearchAsync("chakalaka", 5)).Products;

        Assert.That(products, Is.Not.Empty);
        Assert.That(products, Has.All.Matches<ShopProduct>(p => (p.Price > 0 || p.UnitPrice > 0) && p.Url.Host == "www.checkers.co.za"));
    }
}
