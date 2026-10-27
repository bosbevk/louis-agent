using LouisAgent.Packs.Shops.Catalogues;
using LouisAgent.Packs.Shops.Places;
using LouisAgent.Packs.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Packs.Shops.Tests.Providers;

// F33 step 2: Checkers' products read from its own catalogue. Its search page (/search?Search=…, the address its own
// search box sends; /search?q= finds nothing) fetches /api/catalogue/get-products-filter, and that answer has each
// product's name, price, unit and article number; the page's cards link each product as /product/{name}-{article}{unit}.
// The fixtures are a real answer and page, recorded 2026-10-17 and trimmed, so a renamed field fails here first.
public class CheckersCatalogueTests
{
    private static readonly string Fixtures = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.pack.shops.tests", "Providers", "Fixtures");
    private static string Json => File.ReadAllText(Path.Combine(Fixtures, "checkers-search.json"));
    private static string Html => File.ReadAllText(Path.Combine(Fixtures, "checkers-search.html"));
    private static readonly Uri Search = CheckersCatalogue.SearchUri("chicken pieces");

    [Test]
    public void TheSearch_IsTheAddressCheckersOwnSearchBoxSends() =>
        Assert.That(Search.AbsoluteUri, Is.EqualTo("https://www.checkers.co.za/search?Search=chicken%20pieces"));

    // PR #58's review: a pack sold by weight showed its price per kg as its price, so the cook priced a 1–1.2 kg pack of
    // thighs at R99.99 (run fc7f8ecfdc87). A pack's price is what its weight costs: the lightest to the heaviest pack.
    [Test]
    public void AProductSoldByWeight_IsWhatItsPacksCost_WithItsPricePerKgAndItsLink()
    {
        var chicken = CheckersCatalogue.Parse(Json, Html, Search, 10)[0];

        Assert.That(chicken.Name, Is.EqualTo("Farmer's Choice Fresh Chicken 16 Piece Drumsticks & Wings Per kg"));
        Assert.That((chicken.Price, chicken.PriceTo), Is.EqualTo(((decimal?)87.99m, (decimal?)103.99m)), "R79.99/kg × 1.1 kg to × 1.3 kg");
        Assert.That((chicken.Currency, chicken.UnitPrice, chicken.UnitOfMeasure), Is.EqualTo(("ZAR", (decimal?)79.99m, "kg")));
        Assert.That(chicken.Size, Is.EqualTo("1.1–1.3 kg"));
        Assert.That(chicken.Url.ToString(), Is.EqualTo("https://www.checkers.co.za/product/farmers-choice-fresh-chicken-16-piece-drumsticks-and-wings-per-kg-10796287KG"));
    }

    [Test]
    public void AProductWhoseCardIsntOnThePage_LinksTheSearchThatShowsItsPrice() =>
        Assert.That(CheckersCatalogue.Parse(Json, Html, Search, 10)[1].Url, Is.EqualTo(Search));

    [Test]
    public void AProductSoldByThePiece_IsItsPriceEach()
    {
        var chakalaka = CheckersCatalogue.Parse(Json, Html, Search, 10)[2];

        Assert.That((chakalaka.Name, chakalaka.Price, chakalaka.PriceTo, chakalaka.UnitOfMeasure),
            Is.EqualTo(("KOO Mild & Spicy Chakalaka Can 410g", (decimal?)25.99m, (decimal?)null, "each")));
        Assert.That((chakalaka.UnitPrice, chakalaka.Size), Is.EqualTo(((decimal?)null, (string?)null)), "the size is in its name");
        Assert.That(chakalaka.Url.AbsolutePath, Is.EqualTo("/product/koo-mild-and-spicy-chakalaka-can-410g-10133306EA"));
    }

    // Louis: "can we have a product image". The answer names each product's image (imageId); the page's cards load it
    // from Checkers' catalogue host, at 256 pixels.
    [Test]
    public void AProductsImage_IsTheOneItsCardShows() =>
        Assert.That(CheckersCatalogue.Parse(Json, Html, Search, 10)[2].Image?.AbsoluteUri,
            Is.EqualTo("https://catalog.sixty60.co.za/v2/files/6a5f7a88745efcce53fcd220?width=256&height=256"));

    [Test]
    public void AProductWithoutAnImage_HasNone() =>
        Assert.That(CheckersCatalogue.Parse(Json, Html, Search, 10)[0].Image, Is.Null);

    [Test]
    public void TheList_IsCutAtMaxResults() =>
        Assert.That(CheckersCatalogue.Parse(Json, Html, Search, 2), Has.Count.EqualTo(2));

    private const string Two = """
        {"products":[
          {"articleNumber":"1","name":"On special","unitOfMeasure":"EA","price":30.00,"discountedPrice":24.99,"currency":"ZAR","isOnPromotion":true,"isStockAvailable":true},
          {"articleNumber":"2","name":"Sold out","unitOfMeasure":"EA","price":10.00,"discountedPrice":10.00,"currency":"ZAR","isStockAvailable":false}
        ],"totalCount":2}
        """;

    [Test]
    public void AProductOnSpecial_IsWhatItCostsNow() =>
        Assert.That(CheckersCatalogue.Parse(Two, "", Search, 10)[0].Price, Is.EqualTo(24.99m));

    private const string Weighed = """
        {"products":[
          {"articleNumber":"3","name":"Beef Mince (300g - 500g)","unitOfMeasure":"KG","price":129.99,"discountedPrice":129.99,"currency":"ZAR","isStockAvailable":true,"weightOptions":["300-500"]},
          {"articleNumber":"4","name":"Beef Mince Per kg","unitOfMeasure":"KG","price":129.99,"discountedPrice":129.99,"currency":"ZAR","isStockAvailable":true}
        ],"totalCount":2}
        """;

    [Test]
    public void APackOfMince_IsWhatItsWeightCosts_NotItsPricePerKg()
    {
        var mince = CheckersCatalogue.Parse(Weighed, "", Search, 10)[0];

        Assert.That((mince.Price, mince.PriceTo, mince.UnitPrice), Is.EqualTo(((decimal?)39.00m, (decimal?)65.00m, (decimal?)129.99m)));
    }

    [Test]
    public void AProductSoldByWeight_WithNoPackWeight_HasNoPackPrice_OnlyItsPricePerKg()
    {
        var mince = CheckersCatalogue.Parse(Weighed, "", Search, 10)[1];

        Assert.That((mince.Price, mince.PriceTo, mince.UnitPrice, mince.Size), Is.EqualTo(((decimal?)null, (decimal?)null, (decimal?)129.99m, (string?)null)));
    }

    [Test]
    public void AProductOutOfStock_IsLeftOut_ItCantBeBought() =>
        Assert.That(CheckersCatalogue.Parse(Two, "", Search, 10).Select(p => p.Name), Is.EqualTo(new[] { "On special" }));

    // F34 step 2: a deal is in the same visit. The answer says which products have one: a multi-buy ("bonus buy") by
    // bonusBuyIds, a promotion by isOnPromotion; their prices don't change. The card says what it is: its promotion
    // badge ("BUY 2 FOR R199", "SAVE"). The fixtures are a real answer and page, recorded 2026-10-17 and trimmed; the
    // last product's card is left out, as a deal whose wording the page didn't show.
    private static string DealsJson => File.ReadAllText(Path.Combine(Fixtures, "checkers-deals.json"));
    private static string DealsHtml => File.ReadAllText(Path.Combine(Fixtures, "checkers-deals.html"));
    private static IReadOnlyList<ShopProduct> Deals => CheckersCatalogue.Parse(DealsJson, DealsHtml, Search, 10);

    [Test]
    public void AMultiBuy_IsASpecial_WithItsCardsWording() =>
        Assert.That(Deals[0].Deal, Is.EqualTo(new ShopDeal(ShopDealKind.Special, "BUY 2 FOR R199")));

    [Test]
    public void AProductOnPromotion_IsAPromotion_WithItsBadge() =>
        Assert.That(Deals[2].Deal, Is.EqualTo(new ShopDeal(ShopDealKind.Promotion, "SAVE")));

    [Test]
    public void AProductWithNoDeal_HasNone() =>
        Assert.That(Deals[1].Deal, Is.Null);

    [Test]
    public void ADealWhoseCardDoesntShowIt_KeepsItsKind_WithoutWording() =>
        Assert.That(Deals[3].Deal, Is.EqualTo(new ShopDeal(ShopDealKind.Special, null)));

    [Test]
    public void ADealDoesntChangeThePrice() =>
        Assert.That((Deals[0].Price, Deals[2].UnitPrice), Is.EqualTo(((decimal?)149.99m, (decimal?)79.99m)));

    // --- Through the browser ---

    private sealed class FakeBrowser(params string[] captured) : IWebBrowser
    {
        public Uri? Visited { get; private set; }
        public Func<Uri, bool>? Capture { get; private set; }
        public BrowserSession? Session { get; private set; }
        public string? ReadySelector { get; private set; }

        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default)
        {
            (Visited, Capture, Session, ReadySelector) = (url, capture, session, readySelector);
            return Task.FromResult(new BrowserVisit(url.ToString(), Html, captured));
        }
    }

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

    // Louis: "how can we verify that the product comes from the malmesbury store? that the address was used". Each
    // product in Checkers' answer names the store that priced it (storeId): the town's only when it's one of the stores
    // the town's lookup named; else Checkers answered from another (its default), and the search says so.
    private static string AnswerFrom(string storeId) =>
        "{\"products\":[{\"articleNumber\":\"1\",\"name\":\"Rice 1kg\",\"unitOfMeasure\":\"EA\",\"price\":24.99,\"discountedPrice\":24.99," +
        "\"currency\":\"ZAR\",\"isStockAvailable\":true,\"storeId\":\"" + storeId + "\"}],\"totalCount\":1}";

    [Test]
    public async Task WhereTheUserIs_TheSearchRunsInTheStoreThatServesIt_AndItsAnswerProvesIt()
    {
        var browser = new FakeBrowser(AnswerFrom("655b0c8fb44cda100fd99383"));
        var found = await new CheckersCatalogue(browser, Store()).SearchAsync("rice", 5, "Malmesbury");

        Assert.That(browser.Session!.Cookies.Single().Value, Is.EqualTo("655b0c8fb44cda100fd99383"));
        Assert.That((found.Area, found.Store, found.AreaIgnored), Is.EqualTo(("Malmesbury", "655b0c8fb44cda100fd99383", false)));
    }

    [Test]
    public async Task AnAnswerFromAnotherStore_IsNeverCalledTheTowns()
    {
        var browser = new FakeBrowser(AnswerFrom("5ece6935faafe599532665b2"));
        var found = await new CheckersCatalogue(browser, Store()).SearchAsync("rice", 5, "Malmesbury");

        Assert.That((found.Area, found.Store, found.AreaIgnored), Is.EqualTo(((string?)null, "5ece6935faafe599532665b2", true)));
    }

    [Test]
    [NonParallelizable]
    public async Task AnAnswerFromAnotherStore_LogsTheTownAndTheStoreAsHashes()
    {
        // F18 step 4f.2: the warning says the search was answered from a store that doesn't serve the town; neither is in clear.
        using var stderr = new CapturedStderr();

        await new CheckersCatalogue(new FakeBrowser(AnswerFrom("5ece6935faafe599532665b2")), Store()).SearchAsync("rice", 5, "Malmesbury");

        Assert.That(stderr.Text, Does.Contain("Checkers answered a search from #").And.Contain("from store #"));
        Assert.That(stderr.Text, Does.Not.Contain("Malmesbury").And.Not.Contain("5ece6935faafe599532665b2"));
    }

    [Test]
    public async Task WithNoPlace_OrNoStoreForIt_TheSearchIsTheDefaultStores()
    {
        var browser = new FakeBrowser(Json);
        var noPlace = await new CheckersCatalogue(browser, Store()).SearchAsync("chicken pieces", 5);
        Assert.That((browser.Session, noPlace.Area), Is.EqualTo(((BrowserSession?)null, (string?)null)));

        var unknown = await new CheckersCatalogue(browser, Store()).SearchAsync("chicken pieces", 5, "Atlantis");
        Assert.That((browser.Session, unknown.Area), Is.EqualTo(((BrowserSession?)null, (string?)null)));
    }

    [Test]
    public async Task ASearch_VisitsTheSearchPage_AndKeepsOnlyTheCataloguesAnswer()
    {
        var browser = new FakeBrowser(Json);
        var products = (await new CheckersCatalogue(browser).SearchAsync("chicken pieces", 5)).Products;

        Assert.That(products, Has.Count.EqualTo(3));
        Assert.That(browser.Visited, Is.EqualTo(Search));
        Assert.That(browser.Capture!(new Uri("https://www.checkers.co.za/api/catalogue/get-products-filter")), Is.True);
        Assert.That(browser.Capture!(new Uri("https://www.checkers.co.za/api/catalogue/get-products-filter-options")), Is.False);
        Assert.That(browser.Capture!(new Uri("https://www.checkers.co.za/api/catalogue/get-popular-searches")), Is.False);
        Assert.That(browser.ReadySelector, Does.Contain("product-card_card__"), "done when a card is drawn, not when the page goes quiet");
    }

    [Test]
    public void NoAnswerFromTheCatalogue_SaysSo_NamingTheSearch() =>
        Assert.That(() => new CheckersCatalogue(new FakeBrowser()).SearchAsync("chicken pieces", 5),
            Throws.TypeOf<ShopCatalogueException>().With.Message.Contains("Checkers").And.Message.Contains("chicken pieces"));

    [Test]
    public void AnAnswerThatCantBeRead_SaysSo() =>
        Assert.That(() => new CheckersCatalogue(new FakeBrowser("<html>not json</html>")).SearchAsync("chicken pieces", 5),
            Throws.TypeOf<ShopCatalogueException>().With.Message.Contains("Checkers"));

    // F34, cook check 42d837f13659: Checkers' refusals were a page that wasn't its catalogue, then no answer. Either tells
    // the shop's throttle, so the next search waits; a browser that's down is ours, not a refusal.
    [TestCase("<html>Request blocked</html>")]
    [TestCase(null)]
    public async Task ARefusedSearch_MakesTheNextOneWait(string? answer)
    {
        var waits = new List<TimeSpan>();
        var now = new DateTimeOffset(2026, 10, 18, 0, 24, 0, TimeSpan.Zero);
        var throttle = new LouisAgent.Packs.Shops.Providers.ShopThrottle(2, TimeSpan.FromSeconds(20), () => now,
            (wait, _) => { waits.Add(wait); return Task.CompletedTask; });
        var refused = new CheckersCatalogue(answer is null ? new FakeBrowser() : new FakeBrowser(answer), throttle: throttle);

        Assert.That(() => refused.SearchAsync("onions", 5), Throws.TypeOf<ShopCatalogueException>());
        await new CheckersCatalogue(new FakeBrowser(Json), throttle: throttle).SearchAsync("rice", 5);

        Assert.That(waits, Is.EqualTo(new[] { TimeSpan.FromSeconds(20) }));
    }

    [Test]
    public async Task ABrowserThatsDown_IsNoRefusal()
    {
        var waits = new List<TimeSpan>();
        var throttle = new LouisAgent.Packs.Shops.Providers.ShopThrottle(2, TimeSpan.FromSeconds(20), null, (wait, _) => { waits.Add(wait); return Task.CompletedTask; });

        Assert.That(() => new CheckersCatalogue(new DownBrowser(), throttle: throttle).SearchAsync("onions", 5), Throws.TypeOf<ShopCatalogueException>());
        await new CheckersCatalogue(new FakeBrowser(Json), throttle: throttle).SearchAsync("rice", 5);

        Assert.That(waits, Is.Empty);
    }

    private sealed class DownBrowser : IWebBrowser
    {
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null, string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default) =>
            throw new WebBrowserException("The browser at http://puls-browser:9222 didn't answer");
    }

    [Test]
    public void TheBrowsersError_IsTheCataloguesOwn() =>
        Assert.That(() => new CheckersCatalogue(new DownBrowser()).SearchAsync("chicken pieces", 5),
            Throws.TypeOf<ShopCatalogueException>().With.Message.Contains("didn't answer"));

    [Category("Integration")]
    [Test, Category("Live")]
    public async Task FromMalmesbury_TheSearchIsMalmesburysStore()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to search Checkers with (the Puls stack's puls-browser).");
        var browser = new ChromeWebBrowser(new Uri(url!));

        var found = await new CheckersCatalogue(browser, new CheckersStore(browser, LouisAgent.Packs.Shops.Places.PlaceCoordinates.OfAsync)).SearchAsync("chicken", 5, "Malmesbury");

        Assert.That(found.Area, Is.EqualTo("Malmesbury"));
        Assert.That(found.Store, Does.Not.StartWith("5ece6935"), "the default store's id: the town's store answered");
        Assert.That(found.Products, Is.Not.Empty);
    }

    [Category("Integration")]
    [Test, Category("Live")]
    public async Task Checkers_ShowsItsDeals_WithTheirWording()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to search Checkers with (the Puls stack's puls-browser).");

        var products = (await new CheckersCatalogue(new ChromeWebBrowser(new Uri(url!))).SearchAsync("chicken", 16)).Products;

        Assert.That(products, Has.Some.Matches<ShopProduct>(p => p.Deal is { Text.Length: > 0 }), "deals come and go; chicken has had one all week");
    }

    [Category("Integration")]
    [Test, Category("Live")]
    public async Task Checkers_FindsItsProducts_WithPricesAndLinks()
    {
        string? url = Environment.GetEnvironmentVariable("BROWSER_URL");
        Assume.That(url, Is.Not.Null.And.Not.Empty, "BROWSER_URL isn't set: no browser to search Checkers with (the Puls stack's puls-browser).");

        var products = (await new CheckersCatalogue(new ChromeWebBrowser(new Uri(url!))).SearchAsync("chakalaka", 5)).Products;

        Assert.That(products, Is.Not.Empty);
        Assert.That(products, Has.All.Matches<ShopProduct>(p => (p.Price > 0 || p.UnitPrice > 0) && p.Url.Host == "www.checkers.co.za"));
    }
}
