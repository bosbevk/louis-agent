using LouisAgent.Packs.Shops;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Packs.Shops.Catalogues;
using LouisAgent.Packs.Shops.Tools;
using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Packs.Shops.Tests.Tools;

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

        public string? Area { get; init; }
        public string? Store { get; init; }
        public bool AreaIgnored { get; init; }

        public string? Place { get; private set; }

        public Task<ShopSearch> SearchAsync(string query, int maxResults, string? place = null, CancellationToken cancellationToken = default)
        {
            (Query, MaxResults, Token, Place) = (query, maxResults, cancellationToken, place);
            if (Fails is not null) throw Fails;
            return Task.FromResult(new ShopSearch(products, Area, Store, AreaIgnored));
        }
    }

    private static readonly ShopProduct Chicken = new("Farmer's Choice Fresh Chicken 16 Piece Drumsticks & Wings Per kg", 87.99m, 103.99m, "ZAR",
        "1.1–1.3 kg", 79.99m, "kg", new Uri("https://www.checkers.co.za/product/farmers-choice-fresh-chicken-16-piece-drumsticks-and-wings-per-kg-10796287KG"));

    private static readonly ShopProduct Chakalaka = new("KOO Mild & Spicy Chakalaka Can 410g", 25.99m, null, "ZAR", null, null, "each",
        new Uri("https://www.checkers.co.za/product/koo-mild-and-spicy-chakalaka-can-410g-10133306EA"),
        new Uri("https://catalog.sixty60.co.za/v2/files/6a5f7a88745efcce53fcd220?width=256&height=256"));

    private static ShopTools Tools(FakeCatalogue catalogue) => new(new Dictionary<ShopKind, IShopCatalogue> { [ShopKind.Checkers] = catalogue });

    [Test]
    public async Task AShopsProducts_AreATable_FramedAsData()
    {
        string result = await Tools(new FakeCatalogue(Chicken, Chakalaka)).FindProducts("Checkers", "chicken pieces");

        // F33's run: "Checkers's products" read wrong for a shop whose name ends in s; the header names the shop plainly.
        Assert.That(result, Does.StartWith("Products at Checkers (its default store: no area shared) for 'chicken pieces' (2)").And.Not.Contain("'s products"));
        Assert.That(result, Does.Contain("untrusted"), "a shop's names come from its site");
        // PR #58's review: the Price column is what a pack costs; a pack sold by weight, from its lightest to its heaviest.
        // Louis: "can we have a product image": the photo the shop shows, for the cook's list; "-" without one.
        Assert.That(result, Does.Contain("| Product | Size | Pack price | Per kg | Image | Link |"));
        Assert.That(result, Does.Contain("| Farmer's Choice Fresh Chicken 16 Piece Drumsticks & Wings Per kg | 1.1–1.3 kg | R87.99–R103.99 | R79.99/kg | - | https://www.checkers.co.za/product/farmers-choice-fresh-chicken-16-piece-drumsticks-and-wings-per-kg-10796287KG |"));
        Assert.That(result, Does.Contain("| KOO Mild & Spicy Chakalaka Can 410g | - | R25.99 | - | https://catalog.sixty60.co.za/v2/files/6a5f7a88745efcce53fcd220?width=256&height=256 | https://www.checkers.co.za/product/koo-mild-and-spicy-chakalaka-can-410g-10133306EA |"));
    }

    [Test]
    public async Task AProductSoldByWeight_WithNoPackWeight_HasNoPackPrice() =>
        Assert.That(await Tools(new FakeCatalogue(new ShopProduct("Beef Mince Per kg", null, null, "ZAR", null, 129.99m, "kg",
                new Uri("https://www.checkers.co.za/search?Search=mince")))).FindProducts("checkers", "mince"),
            Does.Contain("| Beef Mince Per kg | - | - | R129.99/kg | - |"));

    [Test]
    public async Task TheStoreThatAnswered_IsNamed_WithItsId() =>
        // F34 step 1b, then Louis: "how can we verify … that the address was used": the store that answered, by its id.
        Assert.That(await Tools(new FakeCatalogue(Chakalaka) { Area = "Malmesbury", Store = "655b0c8fb44cda100fd99383" }).FindProducts("checkers", "chakalaka"),
            Does.StartWith("Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'chakalaka' (1)"));

    [Test]
    public async Task AnAnswerFromAnotherStore_IsSaidSo()
    {
        string result;
        using (LouisAgent.Core.Sessions.UserPlace.Use(new("Malmesbury", null)))
            result = await Tools(new FakeCatalogue(Chakalaka) { Store = "5ece6935faafe599532665b2", AreaIgnored = true }).FindProducts("checkers", "chakalaka");

        Assert.That(result, Does.StartWith("Products at Checkers (its default store, store 5ece6935: Malmesbury's wasn't used) for 'chakalaka'"));
    }

    // F34 run dd992620f6cc: cook-en named Pretoria only in its message, no place was shared, and every search was the
    // default store's. The tools take the town from the task (area) when Puls shared no place; a shared place wins.
    [Test]
    public async Task TheTownFromTheTask_IsWhereItSearches_WhenNoPlaceWasShared()
    {
        var catalogue = new FakeCatalogue(Chakalaka);

        string result = await Tools(catalogue).FindProducts("checkers", "chakalaka", area: " Pretoria ");

        Assert.That(catalogue.Place, Is.EqualTo("Pretoria"));
        Assert.That(result, Does.Contain("its default store: none found for Pretoria"), "the fake found none: the header says for where");
    }

    // Review of PR #61: area is whatever the model passes, so a street address the user typed would go to Nominatim, the
    // log and the store cache file. Only the town goes on: a part with a digit (a house number, a postcode) is dropped.
    [TestCase("12 Piet Retief Street, Malmesbury, 7300", "Malmesbury")]
    [TestCase("Malmesbury, Wes-Kaap", "Malmesbury, Wes-Kaap")]
    [TestCase("7300", null)]
    public async Task OnlyTheTown_GoesOn(string area, string? searched)
    {
        var catalogue = new FakeCatalogue(Chakalaka);

        await Tools(catalogue).FindProducts("checkers", "chakalaka", area: area);

        Assert.That(catalogue.Place, Is.EqualTo(searched));
    }

    [Test]
    public async Task ASharedPlace_WinsOverTheTasksTown()
    {
        var catalogue = new FakeCatalogue(Chakalaka);
        using (LouisAgent.Core.Sessions.UserPlace.Use(new("Malmesbury, Wes-Kaap", null)))
            await Tools(catalogue).SearchSpecials("checkers", "chakalaka", area: "Pretoria");

        Assert.That(catalogue.Place, Is.EqualTo("Malmesbury, Wes-Kaap"));
    }

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

    // --- F34 step 3: SearchSpecials and SearchPromotions (Louis: "the 2 new Tools"): the same search, one kind of deal ---

    private static readonly ShopProduct Coffee = new("NESCAFÉ Classic Instant Coffee 200g", 149.99m, null, "ZAR", null, null, "each",
        new Uri("https://www.checkers.co.za/product/nescafe-classic-instant-coffee-200g-10395676EA"), null,
        new ShopDeal(ShopDealKind.Special, "BUY 2 FOR R199"));

    private static readonly ShopProduct Thighs = new("Farmer's Choice Fresh Drum & Thigh Chicken 16-Piece", 135.98m, 151.98m, "ZAR",
        "1.7–1.9 kg", 79.99m, "kg", new Uri("https://www.checkers.co.za/product/farmers-choice-drum-and-thigh-10732965KG"), null,
        new ShopDeal(ShopDealKind.Promotion, "SAVE"));

    private static readonly ShopProduct Rooibos = new("Freshpak Rooibos 80s", 59.99m, null, "ZAR", null, null, "each",
        new Uri("https://www.checkers.co.za/product/freshpak-rooibos-80s-10001EA"), null, new ShopDeal(ShopDealKind.Special, null));

    [Test]
    public async Task SearchSpecials_KeepsTheMultiBuys_WithTheirDeal()
    {
        var catalogue = new FakeCatalogue(Chakalaka, Coffee, Thighs, Rooibos);

        string result = await Tools(catalogue).SearchSpecials("checkers", "coffee");

        Assert.That(result, Does.StartWith("Specials (multi-buys) at Checkers (its default store: no area shared) for 'coffee' (2)"));
        Assert.That(result, Does.Contain("untrusted"));
        Assert.That(result, Does.Contain("| Product | Size | Pack price | Per kg | Deal | Image | Link |"));
        Assert.That(result, Does.Contain("| NESCAFÉ Classic Instant Coffee 200g | - | R149.99 | - | BUY 2 FOR R199 | - | https://www.checkers.co.za/product/nescafe-classic-instant-coffee-200g-10395676EA |"));
        Assert.That(result, Does.Contain("| Freshpak Rooibos 80s | - | R59.99 | - | a multi-buy (its link shows it) |"), "a deal the card didn't word");
        Assert.That(result, Does.Not.Contain("Chakalaka").And.Not.Contain("Farmer's Choice"));
        Assert.That(catalogue.MaxResults, Is.EqualTo(20), "the most the catalogue gives, then one kind kept");
    }

    [Test]
    public async Task SearchPromotions_KeepsThePromotions_WithTheirBadge()
    {
        string result = await Tools(new FakeCatalogue(Chakalaka, Coffee, Thighs)).SearchPromotions("checkers", "chicken");

        Assert.That(result, Does.StartWith("Promotions at Checkers (its default store: no area shared) for 'chicken' (1)"));
        Assert.That(result, Does.Contain("| Farmer's Choice Fresh Drum & Thigh Chicken 16-Piece | 1.7–1.9 kg | R135.98–R151.98 | R79.99/kg | SAVE |"));
        Assert.That(result, Does.Not.Contain("NESCAFÉ").And.Not.Contain("Chakalaka"));
    }

    [Test]
    public async Task ADealTool_KeepsAtMostWhatWasAskedFor()
    {
        string result = await Tools(new FakeCatalogue(Coffee, Rooibos)).SearchSpecials("checkers", "coffee", maxResults: 1);

        Assert.That(result, Does.Contain("(1)").And.Contain("NESCAFÉ").And.Not.Contain("Rooibos"));
    }

    [Test]
    public async Task NoDealOfItsKind_SaysSo_AndWhatToTry()
    {
        Assert.That(await Tools(new FakeCatalogue(Chakalaka, Thighs)).SearchSpecials("checkers", "chakalaka"),
            Does.Contain("no multi-buy").And.Contain("chakalaka").And.Contain("FindProducts"));
        Assert.That(await Tools(new FakeCatalogue(Chakalaka, Coffee)).SearchPromotions("checkers", "chakalaka"),
            Does.Contain("no promotion").And.Contain("chakalaka").And.Contain("FindProducts"));
    }

    [Test]
    public async Task ADealTool_ForAShopNotEnabled_SaysWhichAre() =>
        Assert.That(await Tools(new FakeCatalogue()).SearchPromotions("pnp", "chicken"), Does.Contain("checkers"));

    [Test]
    public async Task FindProducts_KeepsItsTable_NoDealColumn() =>
        Assert.That(await Tools(new FakeCatalogue(Coffee)).FindProducts("checkers", "coffee"),
            Does.Contain("| Product | Size | Pack price | Per kg | Image | Link |").And.Not.Contain("BUY 2 FOR"));

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

    // CL15 step 4: the toolset is the pack's, not core's catalogue's; the shops setting is the pack's own.
    private static ToolsetDefinition ShopsToolset(string? shops) =>
        new ShopsPack(new ShopsOptions { ShopCatalogues = shops }).Toolsets.Single(t => t.Name == Packs.Shops.ShopsToolset.Shops.Wire());

    private static IReadOnlyList<string> Missing(string? shops, string? browser) =>
        ShopsToolset(shops).MissingSettings(new ToolsetContext(new AgentOptions { BrowserUrl = browser }, new NoSearch()));

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
        Assert.That(ShopsToolset("checkers").ToolTypes, Is.EqualTo(new[] { typeof(ShopTools) }));
}
