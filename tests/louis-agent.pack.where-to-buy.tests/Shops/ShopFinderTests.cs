using System.Globalization;
using System.Web;
using LouisAgent.Packs.WhereToBuy.Shops;

namespace LouisAgent.Packs.WhereToBuy.Tests.Shops;

/// <summary>
/// The supermarkets near a town, on the three answers the real map gave: only named supermarkets in the shop
/// category, nearest first, two shops of one chain kept as two, a small box first and a wider one only when the small
/// one holds fewer than the finder keeps.
/// </summary>
public class ShopFinderTests
{
    /// <summary>A finder over a fake map around this town, answering with this file's places.</summary>
    private static (ShopFinder Finder, List<Uri> Asked) Finder(string town, string fixture) =>
        Over(town, ShopMaps.Fixture(fixture));

    /// <summary>A finder over a fake map around this town, answering with these places.</summary>
    private static (ShopFinder Finder, List<Uri> Asked) Over(string town, string places)
    {
        var (map, asked) = ShopMaps.Around(town, places);
        return (new ShopFinder(map), asked);
    }

    /// <summary>A request's box as half its width in degrees, read from its viewbox.</summary>
    private static double HalfWidth(Uri request)
    {
        string[] box = HttpUtility.ParseQueryString(request.Query)["viewbox"]!.Split(',');
        return (double.Parse(box[2], CultureInfo.InvariantCulture) -
                double.Parse(box[0], CultureInfo.InvariantCulture)) / 2;
    }

    /// <summary>
    /// Louis, 2026-11-04: "it must show both then". Malmesbury's two Pick n Pays, one on Voortrekker Road and one on
    /// Voortrekker Street in Riebeek-Wes, are two rows; the nearest is not the map's first answer.
    /// </summary>
    [Test]
    public async Task MalmesburysTwoPickNPays_AreTwoRows_NearestFirst()
    {
        var (finder, _) = Finder(ShopMaps.Malmesbury, "map-malmesbury.json");

        var found = await finder.FindAsync("Malmesbury", CancellationToken.None);

        var shops = found!.Shops;
        Assert.That(shops, Has.Count.EqualTo(6), "all six shops of the map, none cut off by the kept count");
        Assert.That(shops[0].Street, Is.EqualTo("55 Voortrekker Road"), "the nearest, though the map listed it fourth");
        Assert.That(shops.Where(s => s.Name == "Pick n Pay").Select(s => s.Street),
            Is.EqualTo(new[] { "55 Voortrekker Road", "Voortrekker Street" }), "two shops of one chain, never merged");
        Assert.That(shops.Select(s => s.Km), Is.Ordered.Ascending);
        Assert.That(shops[^1].Km, Is.EqualTo(18.2).Within(0.5), "Riebeek-Wes is last");
    }

    /// <summary>A small box answers a town's own shops, and a wider one adds the neighbours, once.</summary>
    [Test]
    public async Task ASmallBoxFirst_ThenAWiderOne_WhileFewerThanKeptAreFound()
    {
        var (finder, asked) = Finder(ShopMaps.Malmesbury, "map-malmesbury.json");

        var found = await finder.FindAsync("Malmesbury", CancellationToken.None);

        Assert.That(asked, Has.Count.EqualTo(3), "the town, the small box, the wider box");
        Assert.That(asked[1].Query, Does.Contain("bounded=1").And.Contain("q=supermarket"));
        Assert.That(HalfWidth(asked[1]), Is.EqualTo(0.05).Within(1e-9), "about 5.5 km from the town's centre");
        Assert.That(HalfWidth(asked[2]), Is.EqualTo(0.15).Within(1e-9), "about 17 km");
        Assert.That(found!.SearchedKm, Is.EqualTo(16));
    }

    /// <summary>A hit with no name is no shop to name; the one that is named stays.</summary>
    [Test]
    public async Task APrinceAlbertHitWithNoName_IsLeftOut()
    {
        var (finder, _) = Finder(ShopMaps.PrinceAlbert, "map-prince-albert.json");

        var found = await finder.FindAsync("Prince Albert", CancellationToken.None);

        Assert.That(found!.Shops.Select(s => (s.Name, s.Street)), Is.EqualTo(new[] { ("Spar", "Odendaal Street") }));
        Assert.That(found.Town.Address!.State, Is.EqualTo("Western Cape"));
    }

    /// <summary>
    /// A suburb of a big city gets the shops near it: the answer holds shops 10 to 30 km away in no order, and the
    /// nearest eight, nearest first, come out of it, though the map's first named hit is far away.
    /// </summary>
    [Test]
    public async Task SeaPoint_GetsTheNearestEight_FromAScatter_NoNamelessOne()
    {
        var (finder, asked) = Finder(ShopMaps.SeaPoint, "map-sea-point.json");

        var found = await finder.FindAsync("Sea Point", CancellationToken.None);

        var shops = found!.Shops;
        Assert.That(shops, Has.Count.EqualTo(ShopFinder.Kept));
        Assert.That(shops.Select(s => s.Km), Is.Ordered.Ascending);
        Assert.That(shops[0].Street, Is.EqualTo("76 Regent Road"), "the nearest is a late hit, not the map's first");
        Assert.That(shops.Select(s => s.Name), Does.Not.Contain("").And.Not.Contain("Jumbo Wholesale"));
        Assert.That(shops.Take(3).Select(s => s.Street), Does.Contain("Saint George's Mall"));
        Assert.That(asked, Has.Count.EqualTo(3), "fewer than eight in the small box, so the wider one too");
    }

    /// <summary>A shop the small box found stays in the answer, though the wider box's twenty leave it out.</summary>
    [Test]
    public async Task AShopOfTheSmallBox_IsKept_WhenTheWiderBoxCutsItOff()
    {
        const string near = """
                            {"name":"Nearby Spar","category":"shop","type":"supermarket",
                             "lat":"-33.9190","lon":"18.3850","address":{"road":"Main Road"}}
                            """;
        const string far = """
                           {"name":"Far Spar","category":"shop","type":"supermarket",
                            "lat":"-33.8700","lon":"18.4500","address":{"road":"Far Road"}}
                           """;
        string places = $"[{string.Join(",", Enumerable.Repeat(far, 20))},{near}]";
        var (finder, _) = Over(ShopMaps.SeaPoint, places);

        var found = await finder.FindAsync("Sea Point", CancellationToken.None);

        Assert.That(found!.Shops[0].Name, Is.EqualTo("Nearby Spar"));
    }

    /// <summary>Only a shop-category supermarket counts, and the same place twice is one row.</summary>
    [Test]
    public async Task OnlySupermarketShops_AreKept_AndAnExactDuplicateIsOne()
    {
        const string spar = """
                            {"name":"Spar","category":"shop","type":"supermarket",
                             "lat":"-33.46","lon":"18.72","address":{"road":"A Street"}}
                            """;
        const string street = """
                              {"name":"Spar Street","category":"highway","type":"residential",
                               "lat":"-33.46","lon":"18.72","address":{}}
                              """;
        const string bakery = """
                              {"name":"Bakery","category":"shop","type":"bakery",
                               "lat":"-33.46","lon":"18.72","address":{}}
                              """;
        var (finder, _) = Over(ShopMaps.Malmesbury, $"[{spar},{spar},{street},{bakery}]");

        var found = await finder.FindAsync("Malmesbury", CancellationToken.None);

        Assert.That(found!.Shops.Select(s => s.Name), Is.EqualTo(new[] { "Spar" }));
    }

    /// <summary>No shop in any box says how far was searched; a town the map doesn't know is no search.</summary>
    [Test]
    public async Task NoShop_SaysHowFarWasSearched_AndAnUnknownTown_IsNone()
    {
        var (finder, asked) = Over(ShopMaps.Malmesbury, "[]");
        var (nowhere, _) = Over("[]", "[]");

        var found = await finder.FindAsync("Malmesbury", CancellationToken.None);

        Assert.That(found!.Shops, Is.Empty);
        Assert.That(found.SearchedKm, Is.EqualTo(55));
        Assert.That(asked, Has.Count.EqualTo(4), "the town, then three boxes, since none held a shop");
        Assert.That(await nowhere.FindAsync("Nowhere", CancellationToken.None), Is.Null);
    }
}
