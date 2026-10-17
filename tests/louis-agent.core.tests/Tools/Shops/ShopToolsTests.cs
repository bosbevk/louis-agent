using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Shops;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tests.Tools.Shops;

// F33 step 3: FindProducts, the shops toolset's tool. A shop's products from its own catalogue as a short table, framed
// as untrusted data (a shop's names come from its site); a shop that isn't enabled says which are; the toolset is off
// unless SHOP_CATALOGUES names a shop and BROWSER_URL a browser (development and benchmarks first, ADR 0023).
public class ShopToolsTests
{
    private sealed class FakeCatalogue(params ShopProduct[] products) : IShopCatalogue
    {
        public string? Query { get; private set; }
        public int MaxResults { get; private set; }
        public CancellationToken Token { get; private set; }
        public Exception? Fails { get; init; }
        public ShopKind Shop => ShopKind.Checkers;

        public Task<IReadOnlyList<ShopProduct>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
        {
            (Query, MaxResults, Token) = (query, maxResults, cancellationToken);
            if (Fails is not null) throw Fails;
            return Task.FromResult<IReadOnlyList<ShopProduct>>(products);
        }
    }

    private static readonly ShopProduct Chicken = new("Farmer's Choice Fresh Chicken 16 Piece Drumsticks & Wings Per kg", 87.99m, 103.99m, "ZAR",
        "1.1–1.3 kg", 79.99m, "kg", new Uri("https://www.checkers.co.za/product/farmers-choice-fresh-chicken-16-piece-drumsticks-and-wings-per-kg-10796287KG"));

    private static readonly ShopProduct Chakalaka = new("KOO Mild & Spicy Chakalaka Can 410g", 25.99m, null, "ZAR", null, null, "each",
        new Uri("https://www.checkers.co.za/product/koo-mild-and-spicy-chakalaka-can-410g-10133306EA"));

    private static ShopTools Tools(FakeCatalogue catalogue) => new(new Dictionary<ShopKind, IShopCatalogue> { [ShopKind.Checkers] = catalogue });

    [Test]
    public async Task AShopsProducts_AreATable_FramedAsData()
    {
        string result = await Tools(new FakeCatalogue(Chicken, Chakalaka)).FindProducts("Checkers", "chicken pieces");

        // F33's run: "Checkers's products" read wrong for a shop whose name ends in s; the header names the shop plainly.
        Assert.That(result, Does.StartWith("Products at Checkers for 'chicken pieces' (2)").And.Not.Contain("'s products"));
        Assert.That(result, Does.Contain("untrusted"), "a shop's names come from its site");
        // PR #58's review: the Price column is what a pack costs; a pack sold by weight, from its lightest to its heaviest.
        Assert.That(result, Does.Contain("| Product | Size | Pack price | Per kg | Link |"));
        Assert.That(result, Does.Contain("| Farmer's Choice Fresh Chicken 16 Piece Drumsticks & Wings Per kg | 1.1–1.3 kg | R87.99–R103.99 | R79.99/kg | https://www.checkers.co.za/product/farmers-choice-fresh-chicken-16-piece-drumsticks-and-wings-per-kg-10796287KG |"));
        Assert.That(result, Does.Contain("| KOO Mild & Spicy Chakalaka Can 410g | - | R25.99 | - | https://www.checkers.co.za/product/koo-mild-and-spicy-chakalaka-can-410g-10133306EA |"));
    }

    [Test]
    public async Task AProductSoldByWeight_WithNoPackWeight_HasNoPackPrice() =>
        Assert.That(await Tools(new FakeCatalogue(new ShopProduct("Beef Mince Per kg", null, null, "ZAR", null, 129.99m, "kg",
                new Uri("https://www.checkers.co.za/search?Search=mince")))).FindProducts("checkers", "mince"),
            Does.Contain("| Beef Mince Per kg | - | - | R129.99/kg |"));

    [Test]
    public async Task TheQueryTheCountAndTheTurnsCancellation_ReachTheCatalogue()
    {
        var catalogue = new FakeCatalogue(Chicken);
        using var turn = new CancellationTokenSource();

        await Tools(catalogue).FindProducts("checkers", "  chicken pieces ", maxResults: 50, cancellationToken: turn.Token);

        Assert.That((catalogue.Query, catalogue.MaxResults, catalogue.Token), Is.EqualTo(("chicken pieces", 20, turn.Token)), "at most 20");
    }

    [TestCase("pnp")]
    [TestCase("")]
    public async Task AShopThatIsntEnabled_SaysWhichAre(string shop) =>
        Assert.That(await Tools(new FakeCatalogue()).FindProducts(shop, "chakalaka"), Does.Contain("checkers"));

    [Test]
    public async Task WithNoShopEnabled_ItSaysNone() =>
        Assert.That(await new ShopTools(new Dictionary<ShopKind, IShopCatalogue>()).FindProducts("checkers", "chakalaka"), Does.Contain("none"));

    [Test]
    public async Task ACataloguesError_IsTheAnswer_WithItsReason() =>
        Assert.That(await Tools(new FakeCatalogue { Fails = new ShopCatalogueException("Checkers' catalogue gave no answer") }).FindProducts("checkers", "x"),
            Does.StartWith("Error").And.Contain("gave no answer"));

    [Test]
    public async Task NoProduct_SaysSo_AndWhatToTry() =>
        Assert.That(await Tools(new FakeCatalogue()).FindProducts("checkers", "unicorn steak"), Does.Contain("unicorn steak").And.Contain("Checkers"));

    [Test]
    public async Task AnEmptyQuery_SaysWhatToSearchFor() =>
        Assert.That(await Tools(new FakeCatalogue(Chicken)).FindProducts("checkers", " "), Does.Contain("query"));

    // --- SHOP_CATALOGUES, and the toolset ---

    [TestCase("checkers", new[] { ShopKind.Checkers })]
    [TestCase(" Checkers, checkers ", new[] { ShopKind.Checkers })]
    [TestCase("checkers,pnp", new[] { ShopKind.Checkers })]
    [TestCase("pnp", new ShopKind[0])]
    [TestCase(null, new ShopKind[0])]
    public void TheEnabledShops_AreTheKnownNamesInTheSetting(string? setting, ShopKind[] expected) =>
        Assert.That(ShopCatalogueFactory.Enabled(setting), Is.EqualTo(expected));

    private sealed class NoSearch : IWebSearchProvider
    {
        public string Name => "none";
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebSearchResult>>([]);
    }

    private static IReadOnlyList<string> Missing(string? shops, string? browser) =>
        ToolsetCatalog.All.Single(t => t.Name == "shops")
            .MissingSettings(new ToolsetContext(new AgentOptions { ShopCatalogues = shops, BrowserUrl = browser }, new NoSearch()));

    [Test]
    public void TheToolset_NeedsAShopAndABrowser()
    {
        Assert.That(Missing(null, null), Is.EquivalentTo(new[] { "SHOP_CATALOGUES", "BROWSER_URL" }));
        Assert.That(Missing("pnp", "http://puls-browser:9222"), Is.EqualTo(new[] { "SHOP_CATALOGUES" }), "no shop it knows");
        Assert.That(Missing("checkers", null), Is.EqualTo(new[] { "BROWSER_URL" }));
        Assert.That(Missing("checkers", "http://puls-browser:9222"), Is.Empty);
    }

    [Test]
    public void TheToolset_GivesFindProducts() =>
        Assert.That(ToolsetCatalog.All.Single(t => t.Name == "shops").ToolType, Is.EqualTo(typeof(ShopTools)));
}
