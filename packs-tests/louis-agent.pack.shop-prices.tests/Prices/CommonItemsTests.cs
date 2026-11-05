using LouisAgent.Packs.ShopPrices.Estimates;
using LouisAgent.Packs.ShopPrices.Prices.Regions;
using LouisAgent.Packs.ShopPrices.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices;

/// <summary>
/// What a recipe really asks for gets a price: the lines are the ingredients of 18 saved recipe pages, written as the
/// cook writes its list, with the rotis and flaked almonds a real answer left unpriced. A line that misses here would
/// come back <c>?</c> and leave the total short.
/// </summary>
public class CommonItemsTests
{
    /// <summary>The lines, with what a cupboard often holds too: the cook's list leaves nothing out but tap water.</summary>
    private static readonly string[] Lines =
    [
        "cooking oil 750 ml", "salt 500 g", "black pepper 50 g", "white sugar 1 kg", "cake flour 1 kg",
        "white vinegar 750 ml", "stock cubes 120 g",
        "rotis 5", "flaked almonds 100 g", "almond slivers 100 g", "dried peaches 250 g", "sultanas 200 g",
        "raisins 200 g", "apricot jam 450 g", "fruit chutney 470 g", "curry powder 100 g", "turmeric 50 g",
        "bay leaves 10 g", "cinnamon 50 g", "garam masala 50 g", "vanilla essence 40 ml", "filo pastry 500 g",
        "puff pastry 400 g", "sour cream 250 ml", "fresh cream 250 ml", "evaporated milk 380 g", "custard 1 l",
        "milk 1 l", "butter 500 g", "eggs 6", "white bread 700 g", "brown bread 700 g", "beef mince 500 g",
        "pork mince 500 g", "white fish fillets 500 g", "onions 1 kg", "carrots 1 kg", "potatoes 2 kg", "tomatoes 1 kg",
        "garlic 100 g", "ginger 100 g", "celery 1", "tomato paste 115 g", "worcestershire sauce 250 ml",
        "dijon mustard 200 g", "sesame seeds 100 g", "mixed herbs 20 g", "olive oil 500 ml", "spaghetti 500 g",
        "cheddar cheese 400 g", "mozzarella 250 g", "parmesan 100 g", "nutmeg 50 g", "baking powder 100 g",
        "cornflour 500 g"
    ];

    /// <summary>Every line gets a price from the tables the pack ships, and none comes back <c>?</c>.</summary>
    [Test]
    public async Task EveryCommonLine_GetsAPrice()
    {
        var book = await new PriceBookCache(new ShippedPrices(), TimeProvider.System).GetAsync(CancellationToken.None);

        var missed = BasketEstimate.Of(Lines, book, Region.National, null).Unpriced.Select(line => line.Item.Text)
            .ToList();

        Assert.That(missed, Is.Empty, string.Join(", ", missed));
    }

    /// <summary>
    /// A line that shares a word with another product is priced from its own: a price from the wrong product looks as
    /// sure as a right one, which is worse than a <c>?</c>.
    /// </summary>
    [TestCase("white fish fillets 500 g", "Fish fillets - frozen")]
    [TestCase("pork mince 500 g", "Pork mince")]
    [TestCase("sour cream 250 ml", "Sour cream")]
    [TestCase("evaporated milk 380 g", "Evaporated milk")]
    [TestCase("black pepper 50 g", "Black pepper")]
    [TestCase("green pepper 1", "Green/red/yellow pepper - fresh")]
    public async Task ALineThatSharesAWord_IsPricedFromItsOwnProduct(string line, string product)
    {
        var book = await new PriceBookCache(new ShippedPrices(), TimeProvider.System).GetAsync(CancellationToken.None);

        var priced = BasketEstimate.Of([line], book, Region.National, null).Lines.Single();

        Assert.That(priced.Match?.Product.Product, Is.EqualTo(product));
    }
}
